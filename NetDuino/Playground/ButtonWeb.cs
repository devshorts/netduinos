using System;
using System.Collections;
using Microsoft.SPOT.Hardware;
using NetDuinoPlusHelloWorld.Utils;
using Onoffswitch.NetDuinoUtils.Utils;
using Onoffswitch.NetDuinoUtils.Web;
using SecretLabs.NETMF.Hardware.NetduinoPlus;

namespace Playground
{
    public class ButtonWeb : WebProgramBase
    {
        #region Data

        private static OutputPort Led;

        private static Boolean _buttonPushed;

        private static readonly object LockObject = new object();

        private static bool _previousButtonStatus = false;

        #endregion

        public ButtonWeb()
        {
            Led = new OutputPort(Pins.GPIO_PIN_D13, false);
        }
        #region Web Server Endpoint Implementation

        private static string ButtonStatus(EndPointActionArguments misc, object[] items)
        {
            lock (LockObject)
            {
               return "Button status is " + _buttonPushed + ", arguments: " + items.FormatAsCommaList();
            }
        }

        #endregion

        #region Endpoint initialization

        public override void Initialize()
        {
            // set our pin listners on the button
            ThreadUtil.Start(() => ButtonUtils.OnBoardButtonPushed(WriteToLed));
        }

        protected override ArrayList GetEndPoints()
        {
            var list = new ArrayList
                           {
                               new EndPoint
                                   {
                                       Action = ButtonStatus,
                                       Name = "ButtonStatus",
                                       Description = "Outputs the button status of the on-board push button"
                                   }
                           };
            return list;

        }

        #endregion

        #region Button Handlers

        
        private static void WriteToLed(bool buttonpushed)
        {
            lock (LockObject)
            {
                _buttonPushed = buttonpushed;
            }

            if (_previousButtonStatus != buttonpushed)
            {
                _previousButtonStatus = buttonpushed;
                LcdWriter.Instance.Write("Button " + (buttonpushed ? "pushed" : "released"));
            }

            Led.Write(buttonpushed);

        }

        #endregion
    }
}
