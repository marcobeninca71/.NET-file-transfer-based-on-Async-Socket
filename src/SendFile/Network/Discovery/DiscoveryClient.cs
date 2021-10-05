using RemoteFile.Interface.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace RemoteFile.Interface.Network.DiscoveryService
{
    public class DeviceInfo
    { 
        public string IpAddress { get; set; }
        public int Port { get; set; }
        public string SharedFolder { get; set; }
        public string DeviceModel { get; set; }
        public string DeviceName { get; set; }
    }

    public class DeviceDiscoveredEventArgs : EventArgs
    { 
        public DeviceInfo Info { get; set; }
    }

    public class DeviceDiscoverException : Exception
    {
        public DeviceDiscoverException(string message) : base(message)
        {
        }
    }

    public delegate void DeviceDiscoveredHandler(object sender, DeviceDiscoveredEventArgs e);

    public class DiscoveryClient
    {
        public event DeviceDiscoveredHandler OnDeviceDiscovered;

        private List<DeviceInfo> devices = new List<DeviceInfo>();
        public List<DeviceInfo> Devices
        {
            get { return new List<DeviceInfo>(devices); }
        }

        public DiscoveryClient()
        { 
        }

        public void Close()
        {
            foreach (UdpClient c in clients)
            {
                c.Close();
            }
        }

        public void DiscoverDevices()
        {
            Task.Run(() => discover());
        }

        private object synObj = new object();
        private List<UdpClient> clients = new List<UdpClient>();
        private void addClient(UdpClient c)
        {
            lock (synObj)
            {
                clients.Add(c);
            }
        }

        private int port = 5051;
        public int Port
        {
            get
            {
                lock (synObj)
                {
                    return port;
                }
            }
            set 
            {
                lock (synObj)
                {
                    port = value;
                }
            }
        }

        private void discover()
        {
            var ips = NetworkHelper.GetLocalAddresses();
            ips.Add(IPAddress.Any);
            ips.Add(IPAddress.Parse("127.0.0.1"));
            foreach (var ip in ips)
            {
                try
                {
                    string id = Guid.NewGuid().ToString();
                    var RequestData = Encoding.ASCII.GetBytes($"DFAB.Request.{id}");
                    var ServerEp = new IPEndPoint(IPAddress.Any, 0);
                    UdpClient client = null;
                    if (ip == IPAddress.Any)
                        client = new UdpClient();
                    else
                        client = new UdpClient(new IPEndPoint(ip, Port));
                    client.EnableBroadcast = true;
                    client.Send(RequestData, RequestData.Length, new IPEndPoint(IPAddress.Broadcast, Port));
                    addClient(client);
                    Task.Run(()=>waitForAnswer(client, id));
                }
                catch (Exception)
                {
                }
            }
        }

        private void waitForAnswer(UdpClient client, string id)
        {
            try
            {
                var ServerEp = new IPEndPoint(IPAddress.Any, 0);
                Task<UdpReceiveResult> t = client.ReceiveAsync();
                t.Wait();
                var ServerResponseData = t.Result.Buffer;
                ServerEp = t.Result.RemoteEndPoint;
                var ServerResponse = Encoding.ASCII.GetString(ServerResponseData);
                string[] tokens = ServerResponse.Split(';');
                if (tokens.Length == 6)
                {
                    if (tokens[0] == id)
                    {
                        DeviceInfo info = new DeviceInfo();
                        info.IpAddress = tokens[1];
                        info.Port = Convert.ToInt32(tokens[2]);
                        info.SharedFolder = tokens[3];
                        info.DeviceModel = tokens[4];
                        info.DeviceName = tokens[5];
                        Devices.Add(info);
                        OnDeviceDiscovered?.Invoke(this, new DeviceDiscoveredEventArgs() { Info = info });
                    }
                }
                Task.Run(() => waitForAnswer(client, id));
            }
            catch (Exception)
            {
            }
        }
    }
}
