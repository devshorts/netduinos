using System;
using System.Collections;
using Microsoft.SPOT.Hardware;
using Onoffswitch.NetDuinoUtils;
using Onoffswitch.NetDuinoUtils.Utils;
using Onoffswitch.NetDuinoUtils.Web;
using SecretLabs.NETMF.Hardware.NetduinoPlus;

namespace Playground
{
    public class CameraControl : WebProgramBase
    {
        private readonly TTLCamera _camera = new TTLCamera();

        private static readonly object Lock = new object();

        private OutputPort _led ;

        private static bool _usingPort;

        public override void Initialize()
        {
            _camera.Initialize(SerialPorts.COM1, TTLCamera.PortSpeed.Baud115200, TTLCamera.ImageSize.Res160x120);
            _camera.SetCompression(60);
            _led = new OutputPort(Pins.GPIO_PIN_D13, false);
        }

        protected override ArrayList GetEndPoints()
        {
            return new ArrayList
                       {
                           new EndPoint
                               {
                                   Action = TakePicture,
                                   Name = "liveCam.jpeg",
                                   UsesManualSocket = true,
                                   Description = "Writes a 320x240 image live to the screen as it reads from COM1 of the Adafruit TTL camera"
                               }
                       };
        }

        private string TakePicture(EndPointActionArguments misc, object[] items)
        {
            lock (Lock)
            {
                if (_usingPort)
                {
                    return "Whoa whoa, someone is already reading from the camera. Wait a hot sec and try again later";
                }

                _usingPort = true;
            }

            try
            {
                var mjpeg = items != null && items.Length > 0 && items[0].ToString() == "mjpeg";

                if (mjpeg)
                {
                    misc.Connection.Send(WebUtils.StartMJPEGHeader());
                }

                var count = 0;
                do
                {
                    _led.Write(true);
                    var now = DateTime.Now;
                    _camera.TakePictureToSocket(misc.Connection, mjpeg);
                    LcdWriter.Instance.Write("pic taken #" + count + ": " + (DateTime.Now - now).Seconds + " s");
                    count++;
                } while (misc.Connection != null && mjpeg);

                return null;
            }
            catch (Exception ex)
            {

            }
            finally
            {
                lock (Lock)
                {
                    _led.Write(false);
                    _usingPort = false;
                }
            }
            return null;
        }
    }
}
