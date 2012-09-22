using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using Onoffswitch.NetDuinoUtils;
using Onoffswitch.NetDuinoUtils.Utils;
using SecretLabs.NETMF.Hardware;
using SecretLabs.NETMF.Hardware.NetduinoPlus;

namespace DistanceTest
{
    public class Program
    {
        public static void Main()
        {
            var irSensor = new IRSensor(Pins.GPIO_PIN_A0, 10, 80);
            while(true)
            {
                LcdWriter.Instance.Write(irSensor.PrettyDistance());
                Thread.Sleep(1000);
            }
        }

    }
}
