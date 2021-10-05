using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace RemoteFile.Interface.Network
{
    public static class NetworkHelper
    {
        public static List<IPAddress> GetLocalAddresses()
        {
            List<IPAddress> ips = new List<IPAddress>();
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    ips.Add(ip);
                }
            }
            return ips;
        }

        public static IPAddress GetLocalAddress()
        {
            return GetLocalAddresses().FirstOrDefault();
        }
    }
}
