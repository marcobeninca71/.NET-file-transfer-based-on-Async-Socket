# A .NET File transfer class library to exchange files based on TcpClient and TcpListener

This library gives a series classes to send files between a client and a server.
It also expose a discovery service to find the various servers on the local network.
The servers answer to the discovery message with host and port to connect to send files.

Code exmpale below
Client:

    class Program
    {
        static void Main(string[] args)
        {
            
            DiscoveryClient discoveryClient = new DiscoveryClient();
            discoveryClient.OnDeviceDiscovered += DiscoveryClient_OnDeviceDiscovered;
            discoveryClient.DiscoverDevices();

            Console.WriteLine("Application running.... press any key to terminate");
            Console.ReadKey();
        }

        private static void DiscoveryClient_OnDeviceDiscovered(object sender, DeviceDiscoveredEventArgs e)
        {
            Console.WriteLine($"FILE SERVER FOUND AT {e.Info.IpAddress} PORT {e.Info.Port}!");

            GenericFile file = new GenericFile() { Path = "E:\\temp\\test1.txt" };
            file.onProgress += File_onProgress;
            file.RemotePath = e.Info.SharedFolder;
            file.Transfer(new ConnectionInfo() { Host = e.Info.IpAddress, Port = e.Info.Port});
            Console.WriteLine("FILE TRANSFER RUNNING.... PRESS ANY KEY TO TERMINATE PROGRAM!");
        }

        private static void File_onProgress(object sender, ProgressEventArgs e)
        {
            Console.WriteLine($"Transferring {e.FileName} - {e.ServerAddress} - {e.Position} of {e.Total}!");
        }
    }
	
Server:

    class Program
    {
        static void Main(string[] args)
        {
            
            LocalIPAddress = NetworkHelper.GetLocalAddress();
            DiscoveryServer discoveryServer = new DiscoveryServer("My PC", "My type of PC", LocalIPAddress.ToString(), 5050, 5051, "C:\\temp");
			discoveryServer.Start();

            FileServer fileServer = new FileServer(LocalIPAddress, 5050) { TargetPath = "C:\\temp" };
			fileServer.Start();

            Console.WriteLine("Application running.... press any key to terminate");
            Console.ReadKey();
        }
    }
