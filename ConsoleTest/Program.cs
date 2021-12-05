// See https://aka.ms/new-console-template for more information

using System;
using System.Linq;
using System.Threading;
using HidSharp;
using HidSharp.Reports;
using HidSharpPolling;

class Program
{
    
    static void Main(string[] argv)
    {
        HidSharpPollingWrapper wrapper =
            new HidSharpPollingWrapper(DeviceList.Local, device =>
            {
                uint devUsage = HidSharpPollingWrapper.GetTopLevelUsage(device) ;
                if ((devUsage >> 16)==1) // desktop device
                {
                    switch (devUsage & 0xFFFF)
                    {
                        case 2:
                        case 4:
                        case 6:
                            return true;

                        default:
                            return false;
                    }

                }
                else
                {
                    return false;
                }
            });
        while (true)
        {
            foreach (var deviceRec in wrapper.Devices)
            {
                uint devUsage = deviceRec.TopLevelUsage;
                Console.Write(((Usage)devUsage).ToString());
                Console.WriteLine("("+deviceRec.Name+")");
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