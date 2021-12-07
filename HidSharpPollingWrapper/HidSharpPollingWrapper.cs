using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using HidSharp;
using HidSharp.Reports;
using HidSharp.Reports.Input;
using HidSharp.Utility;

namespace HidSharpPolling
{
    public class HidSharpPollingWrapper
    {
        private DeviceList devList;
        private Func<HidDevice, bool> selectionPredicate;

        private ConcurrentDictionary<string, InputRecord> devices =
            new ConcurrentDictionary<string, InputRecord>();

        public InputRecord[] Devices
        {
            get
            {
                return devices.Values.ToArray();
            }
        }
        
        public HidSharpPollingWrapper(DeviceList list,Func<HidDevice,bool> predicate = null)
        {
            selectionPredicate = predicate;
            devList = list;
            HidSharpDiagnostics.EnableTracing = true;
            HidSharpDiagnostics.PerformStrictChecks = true;

            devList.Changed += DevListOnChanged;
            DevListOnChanged(devList,new DeviceListChangedEventArgs()); // initial config
        }

        private void DevListOnChanged(object? sender, DeviceListChangedEventArgs e)
        {
            // get the new devices list
            foreach(var device in devList.GetHidDevices())
            {
                try
                {
                    if ((selectionPredicate==null)||(selectionPredicate(device))) //Desktop Device
                    {
                        var usage = GetTopLevelUsage(device);
                        Console.WriteLine("Trying add of "+
                                          ((Usage) usage).ToString());
                       string devKey = device.DevicePath;
                        if (!devices.ContainsKey(devKey))
                        {
                            devices.TryAdd(devKey,
                                new InputRecord(device));
                            // start listenign to it
                            HidDeviceInputReceiver rcvr = 
                                device.GetReportDescriptor().CreateHidDeviceInputReceiver();
                            rcvr.Started += RcvrOnStarted;
                            rcvr.Stopped += RcvrOnStopped;
                            rcvr.Received += RcvrOnReceived;
                            OpenConfiguration config = new OpenConfiguration();
                            config.SetOption(OpenOption.Exclusive, false);
                            config.SetOption(OpenOption.Interruptible, true);
                            DeviceStream istream=null;
                            Exception exception;
                            if (device.TryOpen(config, out istream, out exception))
                            {
                                //can cast cause came from HID device
                                rcvr.Start((HidStream)istream);
                            }
                            else 
                                Console.WriteLine("Device open failed: "+
                                                  exception.Message);
                        }

                        Console.WriteLine("Added");
                    }
                }
                catch (Exception ex)
                {
                   Console.WriteLine("AddFailure: "+ex.Message);
                }
            }
        }

        private void RcvrOnReceived(object? sender, EventArgs e)
        {
            HidDeviceInputReceiver reciever = sender as HidDeviceInputReceiver;
            ReportDescriptor descr = reciever.ReportDescriptor;
            HidDevice device = reciever.Stream.Device;
            byte[] inbuff = new byte[descr.MaxInputReportLength];
            Report report;
            if (reciever.TryRead(inbuff, 0, out report))//should succeed
            {
                var parser = report.DeviceItem.CreateDeviceItemInputParser();
                parser.TryParseReport(inbuff, 0,report);
                for (int idx = 0; idx < parser.ValueCount; idx++)
                {
                    devices[device.DevicePath].SetValue(idx,
                            parser.GetValue(idx));
                }
            }
          

        }

        private void RcvrOnStopped(object? sender, EventArgs e)
        {
            HidDeviceInputReceiver reciever = sender as HidDeviceInputReceiver;
            HidDevice device = reciever.Stream.Device;
            InputRecord removedRecord;
            devices.Remove(device.GetSerialNumber(), out removedRecord);
        }

        private void RcvrOnStarted(object? sender, EventArgs e)
        {
            
        }

        //TODO: Change to extension method on HidDevice
        public static uint GetTopLevelUsage(HidDevice dev)
        {
            var reportDescriptor = dev.GetReportDescriptor();
            var ditem = reportDescriptor.DeviceItems.FirstOrDefault();
            return ditem.Usages.GetAllValues().FirstOrDefault();
        }
    }

   
}