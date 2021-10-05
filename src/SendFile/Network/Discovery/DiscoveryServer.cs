using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RemoteFile.Interface.Network.DiscoveryService
{
    public class RemoteRequestEventArgs : EventArgs
    { 
        public string Data { get; set; }
    }

    public class RemoteRequestDataFormatException : Exception
    {
        public RemoteRequestDataFormatException(string message) : base(message)
        {
        }
    }

    public delegate void RemoteRequestDataHandler(object sender, RemoteRequestEventArgs e);

    public class RemoteRequestData
    {
        public RemoteRequestData(string data)
        {
            string[] tokens = data.Split('.');
            if (tokens.Length == 3)
            {
                if (tokens[0] == "DFAB" && tokens[1] == "Request")
                {
                    Guid = tokens[2];
                }
                else
                {
                    throw new RemoteRequestDataFormatException("Wrong request data format");
                }
            }
            else
            {
                throw new RemoteRequestDataFormatException("Wrong request data format");
            }
        }

        public string Guid { get; set; }
    }

    public class DiscoveryServer
    {
        public event RemoteRequestDataHandler OnRemoteRequestData;

        private object syncObj = new object();
        private int cyleTime = 100;
        public int CycleTime
        {
            get
            {
                lock (syncObj)
                {
                    return cyleTime;
                }
            }
            set
            {
                lock (syncObj)
                {
                    cyleTime = value;
                }
            }
        }

        private int port = 5051;
        public int Port
        {
            get
            {
                lock (syncObj)
                {
                    return port;
                }
            }
            set
            {
                lock (syncObj)
                {
                    port = value;
                }
            }
        }

        private int servicePort = 5050;
        public int ServicePort
        {
            get
            {
                lock (syncObj)
                {
                    return servicePort;
                }
            }
            set
            {
                lock (syncObj)
                {
                    servicePort = value;
                }
            }
        }

        private string deviceName = "";
        public string DeviceName
        {
            get
            {
                lock (syncObj)
                {
                    return deviceName;
                }
            }
            set
            {
                lock (syncObj)
                {
                    deviceName = value;
                }
            }
        }

        private string basePath = "";
        public string BasePath
        {
            get
            {
                lock (syncObj)
                {
                    return basePath;
                }
            }
            set
            {
                lock (syncObj)
                {
                    basePath = value;
                }
            }
        }

        private string deviceModel = "";
        public string DeviceModel
        {
            get
            {
                lock (syncObj)
                {
                    return deviceModel;
                }
            }
            set
            {
                lock (syncObj)
                {
                    deviceModel = value;
                }
            }
        }

        public void Stop()
        {
            lock (syncObj)
            {
                if (Server != null)
                {
                    Server.Close();
                }
            }
        }
        private string _ipAdrr = "";
        public string IpAddr
        {
            get
            {
                lock (syncObj)
                {
                    return _ipAdrr;
                }
            }
            set
            {
                lock (syncObj)
                {
                    _ipAdrr = value;
                }
            }
        }

        private string _responseIpAdrr = "";
        public string ResponseIpAdrr
        {
            get
            {
                lock (syncObj)
                {
                    return _responseIpAdrr;
                }
            }
            set
            {
                lock (syncObj)
                {
                    _responseIpAdrr = value;
                }
            }
        }

        private Thread receiver = null;
        public DiscoveryServer(string deviceName, string deviceModel, string ipAddr, int servicePort, int port, string basePath)
        {
            this.DeviceName = deviceName;
            this.BasePath = basePath;
            this.ServicePort = servicePort;
            this.Port = port;
            this.IpAddr = ipAddr;
            this.ResponseIpAdrr = ipAddr;
            this.DeviceModel = deviceModel;
        }

        public void Start()
        {
            receiver = new Thread(Run);
            receiver.Start();
        }

        UdpClient Server = null;

        private void waitForRequest()
        {
            try
            {
                var ClientEp = new IPEndPoint(IPAddress.Any, 0);
                var ClientRequestData = Server.Receive(ref ClientEp);
                var ClientRequest = Encoding.ASCII.GetString(ClientRequestData);

                RemoteRequestData data = new RemoteRequestData(ClientRequest);

                string responseString = string.Format("{0};{1};{2};{3};{4};{5}", data.Guid, ResponseIpAdrr, ServicePort, BasePath, DeviceModel, DeviceName);

                var ResponseData = Encoding.ASCII.GetBytes(responseString);

                OnRemoteRequestData?.Invoke(this, new RemoteRequestEventArgs() { Data = responseString });

                Server.Send(ResponseData, ResponseData.Length, ClientEp);
            }
            catch (Exception)
            {
            }

            Task.Run(() => { waitForRequest(); });
        }

        private void Run()
        {
            Server = new UdpClient();
            Server.Client.Bind(new IPEndPoint(IPAddress.Parse(IpAddr), Port));
            waitForRequest();
        }
    }
}
