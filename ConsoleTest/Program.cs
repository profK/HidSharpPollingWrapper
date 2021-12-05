// See https://aka.ms/new-console-template for more information

using System.Linq;
using HidSharp;
using HidSharp.Reports;
using HidSharpPolling;

class Program
{
    private static uint GetTopLevelUsage(HidDevice dev)
    {
        var reportDescriptor = dev.GetReportDescriptor();
        var ditem = reportDescriptor.DeviceItems.FirstOrDefault();
        return ditem.Usages.GetAllValues().FirstOrDefault();
    }
    static void Main(string[] argv)
    {
        HidSharpPollingWrapper wrapper =
            new HidSharpPollingWrapper(DeviceList.Local, device =>
            {
                switch (GetTopLevelUsage(device) >> 16)
                {
                    case 2:
                    case 4:
                    case 6: 
                        return true;
                      
                    default:
                        return false;
                      
                }
            });
    }
}