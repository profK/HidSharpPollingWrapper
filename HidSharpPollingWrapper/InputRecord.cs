using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using HidSharp;
using HidSharp.Reports;

namespace HidSharpPolling
{
    public class InputRecord
    {
        private HidDevice _hidDevice;

        private ConcurrentDictionary<int, DataValue> values =
            new ConcurrentDictionary<int, DataValue>();
        public InputRecord(HidDevice device)
        {
            _hidDevice = device;
        }

        public string Name
        {
            get
            {
                return _hidDevice.GetFriendlyName();
            }
        }
        
        public DataValue[] Values
        {
            get
            {
                return values.Values.ToArray();
            }
        }

        public void SetValue(int idx, DataValue value)
        {
            values.AddOrUpdate(idx, value,
                (i, dataValue) => dataValue);
        }

       
    }
}