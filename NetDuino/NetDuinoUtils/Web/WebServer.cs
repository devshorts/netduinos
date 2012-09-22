using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using Microsoft.SPOT.Net.NetworkInformation;
using Onoffswitch.NetDuinoUtils.Utils;
using SecretLabs.NETMF.Hardware.NetduinoPlus;

namespace Onoffswitch.NetDuinoUtils.Web
{
    /// <summary>
    /// Simple multithreaded webserver for Netduino.
    /// Lets user define a set of allowed commands (+ parameters) that can be called on the server.
    /// When the server receives a valid command, it fires the EndPointReceived event with details
    /// stored in the EndPoinEventArgs parameter.
    /// 
    /// Usage:
    /// - Instantiate a new instance of WebServer:
    ///   WebServer server = new WebServer(80);
    ///   
    /// - Add a handler to the EndPointReceived event:
    ///   server.EndPointReceived += new WebServer.EndPointReceivedHandler(server_CommandReceived);
    ///   
    /// - Add one or more commands:
    ///   server.AllowedEndPoints.Add(new WebCommand("setled", 1));        // Means that you can call http://[ip-address]/setled/argument1
    ///   server.AllowedEndPoints.Add(new WebCommand("writechannel", 2));  // Means that you can call http://[ip-address]/writechannel/argument1/argument2 (e.g. channel/value)
    /// 
    /// - Initialize the server:
    ///   server.Initialize();
    ///   
    /// Author:             Jasper Schuurmans
    /// Contact:            jasper@schuurmans.cc
    /// Latest revision:    06 april 2011
    /// </summary>
    internal class WebServer
    {
        private bool _cancel;
        private readonly Thread _serverThread;

        private readonly bool _enableLedStatus = true;

        private OutputPort _led; 

        #region Constructors

        /// <summary>
        /// Instantiates a new webserver.
        /// </summary>
        /// <param name="port">Port number to listen on.</param>
        /// <param name="enableLedStatus"></param>
        public WebServer(int port, bool enableLedStatus = true)
        {
            Port = port;

            _serverThread = new Thread(StartServer);

            _enableLedStatus = enableLedStatus;

            Debug.Print("WebControl started on port " + port);
        }

        #endregion

        #region Events

        /// <summary>
        /// Delegate for the EndPointReceived event.
        /// </summary>
        public delegate void EndPointReceivedHandler(object source, EndPoinEventArgs e);

        /// <summary>
        /// EndPointReceived event is triggered when a valid command (plus parameters) is received.
        /// Valid commands are defined in the AllowedEndPoints property.
        /// </summary>
        public event EndPointReceivedHandler EndPointReceived;

        #endregion

        #region Public and private methods

        /// <summary>
        /// Initialize the multithreaded server.
        /// </summary>
        public void Start()
        {
            // List ethernet interfaces, so we can determine the server's address
            ListInterfaces();

            // start server
            _cancel = false;
            _serverThread.Start();
            Debug.Print("Started server in thread " + _serverThread.GetHashCode());
        }

        /// <summary>
        /// Parses a raw web request and filters out the command and arguments.
        /// </summary>
        /// <param name="rawData">The raw web request (including headers).</param>
        /// <returns>The parsed WebCommand if the request is valid, otherwise Null.</returns>
        private EndPoint InterpretRequest(string rawData)
        {
            if(rawData == null)
            {
                return null;
            }
            string commandData;

            // Remove GET + Space
            if (rawData.Length > 5)
                commandData = rawData.Substring(5, rawData.Length - 5);
            else
                return null;

            // Remove everything after first space
            int idx = commandData.IndexOf("HTTP/1.1");
            commandData = commandData.Substring(0, idx - 1);

            // Split command and arguments
            string[] parts = commandData.Split('/');

            string command = null;
            if (parts.Length > 0)
            {
                // Parse first part to command
                command = parts[0].ToLower();
            }

            // http://url/foo/test
            // Check if this is a valid command
            EndPoint returnEndPoint = null;
            foreach (EndPoint endPoint in _allowedEndPoints)
            {
                if (command != null && endPoint.Name.ToLower() == command.ToLower())
                {
                    returnEndPoint = endPoint;
                    break;
                }
            }
            if (returnEndPoint == null)
            {
                return null;
            }
            
            var arguments = new string[parts.Length - 1];

            for (int i = 1; i < parts.Length; i++)
            {
                arguments[i - 1] = parts[i];
            }

            returnEndPoint.Arguments = arguments;

            return returnEndPoint;
        }

        /// <summary>
        /// Starts the server.
        /// </summary>
        private void StartServer()
        {
            using (var server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                server.Bind(new IPEndPoint(IPAddress.Any, Port));
                server.Listen(1);

                while (!_cancel)
                {
                    var connection = server.Accept();

                    if (connection.Poll(-1, SelectMode.SelectRead))
                    {
                        // Create buffer and receive raw bytes.
                        var bytes = new byte[connection.Available];
                        int count = connection.Receive(bytes);

                        // Convert to string, will include HTTP headers.
                        var rawData = new string(Encoding.UTF8.GetChars(bytes));
                        var endPoint = InterpretRequest(rawData);

                        if (endPoint != null)
                        {
                            if (_enableLedStatus)
                            {
                                PingLed();
                            }

                            // dispatch the endpoint
                            var e = new EndPoinEventArgs(endPoint, connection);

                            if (EndPointReceived != null)
                            {
                                ThreadUtil.SafeQueueWorkItem(() =>
                                                     {
                                                         try
                                                         {
                                                             EndPointReceived(null, e);

                                                             if (!e.ManualSent)
                                                             {
                                                                 var response = e.ReturnString;

                                                                 SendResponse(response, connection);
                                                             }
                                                             else
                                                             {
                                                                 connection.Close();
                                                             }
                                                         }
                                                         catch(Exception ex)
                                                         {
                                                             SendResponse("error: " + ex, connection);
                                                         }
                                                     });
                            }
                        }
                        else
                        {
                            SendResponse(GetApiList(), connection);
                        }
                    }

                }
            }
        }

        private string GetApiList()
        {
            try
            {
                string returnString = @"
<body>
<head>
<style type=""text/css"">"
                                      + GetStylings() +
                                      @"</style>
</head>
<div id=""container"">
	<div id=""title"">Netduino Api List</div>
    <div id=""main"">
        <ul>
";
                foreach (EndPoint endpoint in _allowedEndPoints)
                {
                    returnString +=
                        @"           <li><a href=""" + endpoint.Name + "\">" + endpoint.Name +
                        "</a><span id=\"description\">(" + endpoint.Description + ")</span></li>";
                    returnString += "\r\n";
                }
                returnString += "</ul></body>";
                return returnString;
            }
            catch(Exception ex)
            {
                return "";
            }
        }

        private static string GetStylings()
        {
            return
                @"
body{
    background:#A67000;
}

ul { 
    list-style-type: circle; 
    font-size:20px;
} 

#container
{
	margin: 0 auto;
	width: 600px;
	height:100%;
}                

#description{
	font-size:12px;
	padding:10px;
}	

#main{
	background:#FFAD00;
	height:100%;
	padding:5px;
	border-bottom-left-radius: 15px;
	border-bottom-right-radius: 15px;
	-moz-border-botom-left-radius: 15px;
	-moz-border-botom-right-radius: 15px;
	
}

#title{
	background:#ffd273;
	font-size:50px;
	font-variant: small-caps;
	margin:0;
	text-align:center;
	padding:20px;
	border-top-left-radius: 15px;
	border-top-right-radius: 15px;
	-moz-border-botom-left-radius: 15px;
	-moz-border-botom-right-radius: 15px;
}


a:link {color: black;}
a:visited {color: black;}
a:active {color: black;}
a:hover {color: white;}
a {
    text-decoration: underline;
    font-variant:small-caps;
}
";

        }

        private static void WriteBytes(byte[] bytes, Socket connection)
        {
            try
            {
                connection.Send(bytes, 0, bytes.Length, SocketFlags.None);
                using (connection)
                {
                }
            }
            catch(Exception ex)
            {
                
            }
        }

        private static void SendResponse(string response, Socket connection)
        {
            try
            {
                byte[] returnBytes = Encoding.UTF8.GetBytes(response);
                WriteBytes(returnBytes, connection);
            }
            catch(Exception ex)
            {
                
            }
        }

        private void PingLed()
        {
            if(_led == null)
            {
                _led = new OutputPort(Pins.ONBOARD_LED, false);
            }

            _led.Write(true);
            Thread.Sleep(50);
            _led.Write(false);
        }

        private static void ListInterfaces()
        {
            NetworkInterface[] ifaces = NetworkInterface.GetAllNetworkInterfaces();
            Debug.Print("Number of Interfaces: " + ifaces.Length);
            foreach (NetworkInterface iface in ifaces)
            {
                Debug.Print("IP:  " + iface.IPAddress + "/" + iface.SubnetMask);
            }
        }
        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the port the server listens on.
        /// </summary>
        private int Port { get; set; }

        /// <summary>
        /// List of commands that can be handled by the server.
        /// Because of the limited support for generics on the .NET micro framework,
        /// this property is implemented as an ArrayList. Make sure you only add
        /// objects of type Blinq.Netduino.WebCommand to this list.
        /// </summary>
        private readonly System.Collections.ArrayList _allowedEndPoints = new System.Collections.ArrayList();

        #endregion

        public void RegisterEndPoint(EndPoint endPoint)
        {
            _allowedEndPoints.Add(endPoint);
        }
    }
}
