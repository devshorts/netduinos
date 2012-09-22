using System.Net.Sockets;

namespace Onoffswitch.NetDuinoUtils.Web
{
    /// <summary>
    /// Event arguments of an incoming web command.
    /// </summary>
    public class EndPoinEventArgs
    {
        public bool ManualSent { get; set; }

        public EndPoinEventArgs()
        {
        }

        public EndPoinEventArgs(EndPoint command)
        {
            Command = command;
        }

        public EndPoinEventArgs(EndPoint command, Socket connection)
        {
            Command = command;
            Connection = connection;
            Connection.SendTimeout = 5000;
        }

        public EndPoint Command { get; set; }
        public string ReturnString { get; set; }
        public Socket Connection { get; set; }
    }
}