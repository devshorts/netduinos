using System.Collections;
using Onoffswitch.NetDuinoUtils.Web;

namespace Playground
{
    public class BasicPage : WebProgramBase
    {
       
        #region Endpoint initialization

        public override void Initialize()
        {
            
        }

        protected override ArrayList GetEndPoints()
        {
            var list = new ArrayList
                           {
                               new EndPoint
                                   {
                                       Action = Test,
                                       Name = "test.html",
                                       Description = "Writes the URL arguments to a serial LCD hooked up to COM1"
                                   }
                           };
            return list;
        }

        private string Test(EndPointActionArguments misc, object[] items)
        {
            return "<html><body><img height=120 width=160 src='liveCam.jpeg/mjpeg'/></body></html>";
        }

        #endregion

    }
}
