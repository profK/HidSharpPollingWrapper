using System.Collections.Concurrent;
using System.Collections.Generic;
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

        public void SetValue(int idx, DataValue value)
        {
            values.AddOrUpdate(idx, value,
                (i, dataValue) => dataValue);
        }
    }
}