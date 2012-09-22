using System.Collections;
using Onoffswitch.NetDuinoUtils.Utils;
using Onoffswitch.NetDuinoUtils.Web;

namespace Playground
{
    public class Program
    {
        public static void Main()
        {
            LcdWriter.Instance.Write("ready!");
            
            WebServerWrapper.InitializeWebEndPoints(new ArrayList
                                                        {
                                                            new BasicPage(),
                                                            new CameraControl()
                                                        });

            WebServerWrapper.StartWebServer();

            NetDuinoUtils.KeepRunning();
        }
    }
}
