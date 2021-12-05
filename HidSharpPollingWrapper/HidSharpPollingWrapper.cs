using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using HidSharp;
using HidSharp.Reports;
using HidSharp.Reports.Input;

namespace HidSharpPolling
{
    public class HidSharpPollingWrapper
    {
        private DeviceList devList;
        private Func<HidDevice, bool> selectionPredicate;

        private ConcurrentDictionary<string, InputRecord> devices =
            new ConcurrentDictionary<string, InputRecord>();
        
        public HidSharpPollingWrapper(DeviceList list,Func<HidDevice,bool> predicate = null)
        {
            selectionPredicate = predicate;
            devList = list;
            DevListOnChanged(devList,new DeviceListChangedEventArgs()); // initial config
            devList.Changed += DevListOnChanged;
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
                        if (!devices.ContainsKey(device.GetSerialNumber()))
                        {
                            devices.TryAdd(device.GetSerialNumber(),
                                new InputRecord(device));
                            // start listenign to it
                            HidDeviceInputReceiver rcvr = 
                                device.GetReportDescriptor().CreateHidDeviceInputReceiver();
                            rcvr.Started += RcvrOnStarted;
                            rcvr.Stopped += RcvrOnStopped;
                            rcvr.Received += RcvrOnReceived;
                            HidStream istream = device.Open();
                            rcvr.Start(istream);
                        }
                    }
                }
                catch (Exception ex)
                {
                   //nop just ignore if we cannat register it properly
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
                for (int idx = 0; idx < parser.ValueCount; idx++)
                {
                    devices[device.GetSerialNumber()].SetValue(idx,
                            parser.GetValue(idx));
                }
            }
          

        }

        private void RcvrOnStopped(object? sender, EventArgs e)
        {
            //nop
        }

        private void RcvrOnStarted(object? sender, EventArgs e)
        {
            
        }

        
    }

   
}