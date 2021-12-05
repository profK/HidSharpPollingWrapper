// See https://aka.ms/new-console-template for more information

using System;
using System.Linq;
using System.Threading;
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
        while (true)
        {
            foreach (var deviceRec in wrapper.Devices)
            {
                Console.WriteLine(deviceRec.Name);
                foreach (var value in deviceRec.Values)
                {
                    Console.Write("  ");
                    Usage usage = (Usage) (value.Usages.FirstOrDefault());
                    Console.Write(usage.ToString());
                    if (value.DataItem.IsBoolean)
                    {
                        Console.Write("  Digital: ");
                        Console.WriteLine(value.GetLogicalValue());
                    }
                    else
                    {
                        Console.Write("  Analog: ");
                        Console.WriteLine(value.GetFractionalValue());
                    }
                }
            }
            Thread.Sleep(500); // poll 2xsec
        }
    }
}