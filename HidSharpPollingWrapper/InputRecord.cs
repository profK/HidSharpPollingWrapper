using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using HidSharp;
using HidSharp.Reports;

namespace HidSharpPolling
{
    public class InputValue
    {
        public InputValue(DataValue dv)
        {
            IsBoolean = dv.DataItem.IsBoolean;
            AnalogValue = dv.GetFractionalValue();
            DigitalValue = dv.GetLogicalValue();
            Usages = dv.Usages;
        }

        public int DigitalValue { get;  }

        public double AnalogValue { get; }

        public bool IsBoolean { get; }
        public IEnumerable<uint> Usages { get; }
    }
    public class InputRecord
    {
        private HidDevice _hidDevice;

        private ConcurrentDictionary<int, InputValue> values =
            new ConcurrentDictionary<int, InputValue>();

        private Usage _usage;
        private string _friendlyName;

        public InputRecord(HidDevice device)
        {
            _hidDevice = device;
            _usage = (Usage)HidSharpPollingWrapper.GetTopLevelUsage(_hidDevice);
            try
            {
                // this can exception if not supported by device
                _friendlyName = "(" + _hidDevice.GetFriendlyName() + ")";
            }
            catch (Exception e)
            {
                _friendlyName = "";
            }
        }

        public string Name
        {
            get
            {
                return _usage+_friendlyName;
            }
        }
        
        public InputValue[] Values
        {
            get
            {
                return values.Values.ToArray();
            }
        }
        

        public uint TopLevelUsage {
            get
            {
                return HidSharpPollingWrapper.GetTopLevelUsage(_hidDevice);
            }
        }

        public void SetValue(int idx, DataValue value)
        {
            values.AddOrUpdate(idx, new InputValue(value),
                (i, inputValue) => new InputValue(value)) ;
        }

       
    }
}