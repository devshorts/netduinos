
using System;
using Microsoft.SPOT.Hardware;
using AnalogInput = SecretLabs.NETMF.Hardware.AnalogInput;
namespace Onoffswitch.NetDuinoUtils
{
    public class IRSensor
    {
        private int _minCm;
        private int _maxCm;

        private AnalogInput inputPin;
        public IRSensor(Cpu.Pin pin, int minCm, int maxCm)
        {
            try
            {
                _minCm = minCm;
                _maxCm = maxCm;

                inputPin = new AnalogInput(pin);
            }
            catch(Exception ex)
            {
                
            }
        }

        public int Distance()
        {
            return (int)inputPin.Read();
        }

        public string PrettyDistance()
        {
            var ratio = 1.0/1023.0;

            var readDistance = 1023 - Distance();

            var approxCm = (ratio * readDistance * _maxCm) ;
            return "about " + approxCm + " cm";
        }
    }
}
