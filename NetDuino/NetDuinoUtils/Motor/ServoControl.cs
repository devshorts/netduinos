using System;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware;

namespace Onoffswitch.NetDuinoUtils.Motor
{
    public class ServoControl : IDisposable
    {
        /// <summary>
        /// PWM handle
        /// </summary>
        private PWM servo;

        /// <summary>
        /// Timings range
        /// </summary>
        private int[] range = new int[2];

        /// <summary>
        /// Set servo inversion
        /// </summary>
        public bool inverted = false;

        /// <summary>
        /// Create the PWM pin, set it low and configure timings
        /// </summary>
        /// <param name="pin"></param>
        public ServoControl(Cpu.Pin pin)
        {
            try
            {
                // Init the PWM pin
                servo = new PWM(pin);

                servo.SetDutyCycle(0);

                // Typical settings
                range[0] = 1000;
                range[1] = 2000;
            }
            catch(Exception ex)
            {
                
            }
        }

        public void Dispose()
        {
            Disengage();
            servo.Dispose();
        }

        /// <summary>
        /// Allow the user to set cutom timings
        /// </summary>
        /// <param name="fullLeft"></param>
        /// <param name="fullRight"></param>
        public void SetRange(int fullLeft, int fullRight)
        {
            range[1] = fullLeft;
            range[0] = fullRight;
        }

        /// <summary>
        /// Disengage the servo. 
        /// The servo motor will stop trying to maintain an angle
        /// </summary>
        public void Disengage()
        {
            // See what the Netduino team say about this... 
            servo.SetDutyCycle(0);
        }

        public void Drive(int speed)
        {
            if(speed > 100)
            {
                speed = 100;
            }
            if(speed < -100)
            {
                speed = -100;
            }

            uint realSpeed = 0;
            if(speed > 0)
            {
                realSpeed = (uint)(1500 * (100-speed)/100);
            }
            if(speed < 0)
            {
                realSpeed = (uint)(1500 + (1500*(-100 - speed)*-1/100));
            }

            servo.SetPulse(10000, realSpeed);
        }

        /// <summary>
        /// Set the servo degree
        /// </summary>
        public double Degree
        {
            set
            {
                if (value > 180)
                    value = 180;

                if (value < 0)
                    value = 0;

                // Are we inverted?
                if (inverted)
                    value = 180 - value;

                // Set the pulse
                servo.SetPulse(20000, (uint)Map((long)value, 0, 180, range[0], range[1]));
            }
        }

        /// <summary>
        /// Used internally to map a value of one scale to another
        /// </summary>
        /// <param name="x"></param>
        /// <param name="in_min"></param>
        /// <param name="in_max"></param>
        /// <param name="out_min"></param>
        /// <param name="out_max"></param>
        /// <returns></returns>
        private static long Map(long input, long minScale1, long masxScale1, long minScale2, long maxScale2)
        {
            return (input - minScale1) * (maxScale2 - minScale2) / (masxScale1 - minScale1) + minScale2;
        }
    }
}