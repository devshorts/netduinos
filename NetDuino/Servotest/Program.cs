using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using Onoffswitch.NetDuinoUtils.Motor;
using Onoffswitch.NetDuinoUtils.Utils;
using SecretLabs.NETMF.Hardware;
using SecretLabs.NETMF.Hardware.NetduinoPlus;

namespace Servotest
{
    public class Program
    {
        private static ServoControl motorControl; // testing svn

        private static int speed = 0;
        public static void Main()
        {
            LcdWriter.Instance.Write("ready!");
            Thread.Sleep(500);
            // write your code here
            motorControl = new ServoControl(Pins.GPIO_PIN_D9);
            ButtonUtils.OnBoardButtonPushed(SpinMotor);
                
            NetDuinoUtils.KeepRunning();
        }

        private static void SpinMotor(bool buttonpushed)
        {
            if (!buttonpushed)
            {
                LcdWriter.Instance.Write("Disengaging");
                motorControl.Disengage();
            }
            else
            {
                LcdWriter.Instance.Write("Speed is " + speed);
                motorControl.Drive(speed);
                speed += 5;
                if(speed > 100)
                {
                    speed = -100;
                }
            }
        }
    }
}
