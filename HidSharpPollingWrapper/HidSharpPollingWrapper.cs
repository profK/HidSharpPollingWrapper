using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using HidSharp;
using HidSharp.Reports;
using HidSharp.Reports.Input;
using HidSharp.Utility;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HidSharpPolling
{
    public class HidSharpPollingWrapper
    {
        private DeviceList devList;
        private Func<HidDevice, bool> selectionPredicate;
        private ILogger _logger;

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
            // start logging
            IHost host = Host.CreateDefaultBuilder().Build();
            host.RunAsync();
            ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
                builder.AddEventLog());
            _logger = loggerFactory.CreateLogger("HidSharpPollingWrapper");
            //initiate polling
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
                        string devKey = device.DevicePath;
                        if (!devices.ContainsKey(devKey))
                        {
                            InputRecord inputRecord = new InputRecord(device);
                            devices.TryAdd(devKey,inputRecord
                               );
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
                                _logger.LogInformation("Listening to device: "+inputRecord.Name);
                            }
                            else 
                                _logger.LogWarning("Device open failed: "+
                                                  exception.Message);
                        }

                        
                    }
                }
                catch (Exception ex)
                {
                   _logger.LogWarning("AddFailure: "+ex.Message);
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