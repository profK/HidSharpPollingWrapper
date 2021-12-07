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
                    uint usage = (devUsage & 0xFFFF);
                    switch (usage)
                    {

                        case 4:
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
            Console.Write("Press a key to poll:");
            Console.ReadKey();
            Console.WriteLine();
            foreach (var deviceRec in wrapper.Devices)
            {
                try
                {
                    uint devUsage = deviceRec.TopLevelUsage;
                    Console.Write(((Usage) devUsage).ToString());
                    Console.Write("(" + deviceRec.Name + ")");
                }
                catch (Exception e)
                {
                    //nop
                }
                finally
                {
                    Console.WriteLine();
                }

                foreach (var value in deviceRec.Values)
                {
                    Console.Write("  ");
                    Usage usage = (Usage) (value.Usages.FirstOrDefault());
                    Console.Write(usage.ToString());
                    if (value.IsBoolean)
                    {
                        Console.Write("  Digital: ");
                        Console.WriteLine(value.DigitalValue);
                    }
                    else
                    {
                        Console.Write("  Analog: ");
                        Console.WriteLine(value.AnalogValue);
                    }
                }
            }
            Thread.Sleep(500); // poll 2xsec
        }
    }
}