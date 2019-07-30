using System;
using System.Threading;
using System.Diagnostics;

using Windows.Devices.Adc;
using nanoFramework.Targets.STM32F429I_DISCOVERY;

namespace ADC
{
    public class Program
    {
        public static void Main()
        {
            try
            {
                // See if any analog stuff is still out there
                Console.WriteLine(AdcController.GetDeviceSelector());

                // Assign first ADC
                AdcController _ctl1 = AdcController.GetDefault();

                // Some stats to see we are alive
                Console.WriteLine("Channels    : " + _ctl1.ChannelCount.ToString());
                Console.WriteLine("Active mode : " + _ctl1.ChannelMode.ToString()); // 0=SingleEnded, 1=Differential
                Console.WriteLine("Resolution  : " + _ctl1.ResolutionInBits.ToString() + " bits");
                Console.WriteLine("Min value   : " + _ctl1.MinValue);
                Console.WriteLine("Max value   : " + _ctl1.MaxValue);

                // Now open a channel. 
                // We don't need additional HW to test ADC.
                // if we use the internal temp sensor 
                AdcChannel _ac0 = _ctl1.OpenChannel(AdcChannels.Channel_0);

                int _val1 = -1;

                // Loopie
                for (; ; )
                {
                    _val1 = _ac0.ReadValue();
                    Console.WriteLine("Value read from ADC = " + _val1);

                    Thread.Sleep(1000);
                }
            }
            catch (Exception ex)
            {
                // Do whatever please you with the exception caught
                Console.WriteLine(ex.ToString());
                // Loopie
                for (; ; )
                {
                    Thread.Sleep(1000);
                }
            }
        }
    }
}
