using System.IO.Ports;
using System.Threading;
using Onoffswitch.NetDuinoUtils.SerLCD;

namespace ResetLCD
{
    public class Program
    {   
        public static void Main()
        {
            // write your code here
            var serial = new SerialLcd(Serial.COM2);

            Thread.Sleep(1000);
            serial.SetSplashScreenText("Bootup!");
        }

    }
}
