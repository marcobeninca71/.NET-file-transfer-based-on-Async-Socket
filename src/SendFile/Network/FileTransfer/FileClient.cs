using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace RemoteFile.Interface.Network.FileTransfer
{
	public class ConnectionInfo
	{ 
		public int Port { get; set; }
		public string Host { get; set; }
	}

	public class FileClient : FileClientBase
	{

		public FileClient ()
		{
			client	= new TcpClient ();
			workingMode	= Constants.WorkingMode.C_WORKMODE_NONE;
		}

		public void connect (ConnectionInfo info)
		{
			HostAddress = info.Host;
			client.Connect (new IPEndPoint (IPAddress.Parse (info.Host), info.Port));
			stream	= client.GetStream ();
			beginRead ();
		}

		/// <summary>
		/// Process data
		/// </summary>
		/// <param name="state"></param>
		protected override void processData (AsyncState state)
		{
			base.processData(state);
			if (workingMode == Constants.WorkingMode.C_WORKMODE_RECEIVE)
			{
				doReceieveOperation(state);
			}
			else
			{
				if (state.buffer[0] == (byte)'O')
				{
					if (workingMode == Constants.WorkingMode.C_WORKMODE_SEND)
						doSendOperation();
				}
				else if (state.buffer[0] == (byte)'I')
				{
					if (workingMode == Constants.WorkingMode.C_WORKMODE_RECEIVE)
						receivedFileLen = BitConverter.ToInt32(state.buffer, 1);
					write(new byte[] { (byte)'O' });
				}
				else if (state.buffer[0] == (byte)'F')
				{
					filestream?.Dispose();
					RaiseOnTransferError();

					workingMode = Constants.WorkingMode.C_WORKMODE_NONE;
				}
			}
		}

		#region Receive
		public void receiveFile (string filename, string newFileName)
		{
			if (workingMode != Constants.WorkingMode.C_WORKMODE_NONE)
				return;

			fileName = filename;
			byte[] filenameData = Encoding.UTF8.GetBytes (filename);
			byte[] fileInfoData;
			MemoryStream ms = new MemoryStream ();
			BinaryWriter bw = new BinaryWriter (ms);

			bw.Write ('R');                         // Header
			bw.Write ((Int32)filenameData.Length);
			bw.Write (filenameData);

			fileInfoData    = ms.ToArray ();
			bw.Close ();
			ms.Close ();

			// Open file
			if (System.IO.File.Exists(newFileName))
				System.IO.File.Delete(newFileName);

			filestream  = System.IO.File.Open(newFileName, FileMode.CreateNew, FileAccess.Write, FileShare.None);

			// Write to server
			write (fileInfoData);

			workingMode = Constants.WorkingMode.C_WORKMODE_RECEIVE;
			RaiseOnStartTransfer(new TransferStartEventArgs() { Total = filestream.Length } );
		} 
		#endregion

		#region Send

		public void sendFile (string filename)
		{
			if (workingMode != Constants.WorkingMode.C_WORKMODE_NONE)
				return;

			if (!System.IO.File.Exists(filename))
				return;

			fileName = filename;

			FileInfo fileInfo = new FileInfo(filename);
			byte[] fileInfoData;
			MemoryStream ms = new MemoryStream();
			BinaryWriter bw = new BinaryWriter(ms);

			byte[] filenameData = Encoding.UTF8.GetBytes(fileInfo.Name);

			bw.Write ('S');                         // Header
			bw.Write ((Int32)fileInfo.Length);
			bw.Write ((Int32)filenameData.Length);
			bw.Write (filenameData);

			fileInfoData    = ms.ToArray ();
			bw.Close ();
			ms.Close ();

			// Open file
			filestream  = System.IO.File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read);

			// Write to server
			write (fileInfoData);

			workingMode = Constants.WorkingMode.C_WORKMODE_SEND;
			RaiseOnStartTransfer(new TransferStartEventArgs() { Total = filestream.Length });
		} 
		#endregion
	}
}
