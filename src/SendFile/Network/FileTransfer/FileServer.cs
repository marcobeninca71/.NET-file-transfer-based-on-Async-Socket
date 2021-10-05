using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.ServiceModel;
using System.Text;

namespace RemoteFile.Interface.Network.FileTransfer
{
	public class FileServer
	{
		public event ProgressHandler onProgress;
		public event StartTransferHandler onStartTransfer;
		public event FinishTransferHandler onFinishTransfer;
		public event TransferErrorHandler onTransferError;

		public bool	connected;
		public TcpListener	server;

		public string TargetPath { get; set; }

		public string IpAddress { get; set; }

		public FileServer (IPAddress ipAddr, int port)
		{
			IpAddress = ipAddr.ToString();
			IPAddress localAddr = ipAddr;
			server	= new TcpListener (localAddr, port);
		}
		public void Start ()
		{
			if (!connected)
			{
				server.Start ();
				connected	= true;

				beginAccept ();
			}
		}

		public void Stop ()
		{
			if (connected)
			{
				server.Stop ();
			}
		}

		private void beginAccept ()
		{
			if (connected)
				server.BeginAcceptTcpClient (acceptCallback, server);
		}

		private void acceptCallback (IAsyncResult ar)
		{
			try
			{
				TcpClient newClient	= server.EndAcceptTcpClient(ar);

				if (null != newClient)
				{
					FileServerClient client	= new FileServerClient (this, newClient);

                    client.onFinishTransfer += Client_onFinishTransfer;
                    client.onProgress += Client_onProgress;
                    client.onStartTransfer += Client_onStartTransfer;
                    client.onTransferError += Client_onTransferError;

					client.beginRead ();
				}

				beginAccept ();
			}
			catch (ObjectDisposedException)
			{
			}
		}

        private void Client_onTransferError(object sender, TransferErrorEventArgs e)
        {
			e.ServerAddress = this.IpAddress;
			onTransferError?.Invoke(sender, e);
		}

		private void Client_onStartTransfer(object sender, TransferStartEventArgs e)
        {
			e.ServerAddress = this.IpAddress;
			onStartTransfer?.Invoke(sender, e);
		}

		private void Client_onProgress(object sender, ProgressEventArgs e)
        {
			e.ServerAddress = this.IpAddress;
			onProgress?.Invoke(sender, e);
		}

		private void Client_onFinishTransfer(object sender, TransferFinishedEventArgs e)
        {
			e.ServerAddress = this.IpAddress;
			onFinishTransfer?.Invoke(sender, e);
		}
	}
}
