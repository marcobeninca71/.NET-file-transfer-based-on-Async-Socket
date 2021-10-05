using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace RemoteFile.Interface.Network.FileTransfer
{

	public class FileServerClient : FileClientBase
	{
		private string basePath;
		public string BasePath { get { return basePath; } set { basePath = value; } }
		private FileServer owner;

		public FileServerClient (FileServer owner, TcpClient client)
		{
			this.owner = owner;
			this.client	= client;
			this.stream	= client.GetStream ();
			workingMode	= Constants.WorkingMode.C_WORKMODE_NONE;
		}

		protected override void readCallback (IAsyncResult ar)
		{
			try
			{
				base.readCallback(ar);
			}
			catch (IOException)
			{
				disconnect();
			}
		}

		/// <summary>
		/// Process data
		/// </summary>
		/// <param name="state"></param>
		protected override void processData (AsyncState state)
		{
			base.processData(state);

			byte[]	 data	= new byte[state.dataSize];

			Array.Copy (state.buffer, data, data.Length);

			if (workingMode == Constants.WorkingMode.C_WORKMODE_RECEIVE)
			{
				#region Write Data
				doReceieveOperation(state);
				#endregion
			}
			else
			{
				if (data[0] == 'O')
				{
					if (workingMode == Constants.WorkingMode.C_WORKMODE_SEND)
						doSendOperation();
				}
				else if (data[0] == 'R')
				{
					#region Send Data
					int fileNameSize = BitConverter.ToInt32(data, 1);
					fileName = "";

					#region Get File name
					byte[] filenameData = new byte[fileNameSize];
					Array.Copy(data, 5, filenameData, 0, fileNameSize);
					fileName = Encoding.UTF8.GetString(filenameData);
					#endregion

					if (!System.IO.File.Exists(Path.Combine(BasePath, fileName)))
					{
						write(new byte[] { (byte)'F' });
						workingMode = Constants.WorkingMode.C_WORKMODE_NONE;
						return;
					}
					else
						filestream = System.IO.File.Open(Path.Combine(BasePath, fileName), FileMode.Open, FileAccess.Read, FileShare.Read);

					workingMode = Constants.WorkingMode.C_WORKMODE_SEND;

					#region Write file length
					data = new byte[9];
					data[0] = (byte)'I';
					Array.Copy(BitConverter.GetBytes((Int64)filestream.Length), 0, data, 1, 8);

					write(data);
					#endregion

					#endregion
				}
				else if (data[0] == 'S')
				{
					#region Recieve Data
					if (workingMode != Constants.WorkingMode.C_WORKMODE_NONE)
					{
						write(new byte[] { (byte)'F' });
						return;
					}

					receivedFileLen = BitConverter.ToInt32(data, 1);
					int fileNameSize = BitConverter.ToInt32(data, 5);
					fileName = "";

					byte[] filenameData = new byte[fileNameSize];
					Array.Copy(data, 9, filenameData, 0, fileNameSize);
					fileName = Encoding.UTF8.GetString(filenameData);

					if (!string.IsNullOrEmpty(owner.TargetPath))
						fileName = Path.Combine(owner.TargetPath, fileName);

					if (System.IO.File.Exists(fileName))
						System.IO.File.Delete(fileName);
					filestream = System.IO.File.Open(fileName, FileMode.CreateNew);

					write(new byte[] { (byte)'O' });
					workingMode = Constants.WorkingMode.C_WORKMODE_RECEIVE;

					RaiseOnStartTransfer(new TransferStartEventArgs() { Total = receivedFileLen });

					#endregion
				}
				else if (data[0] == 'F')
				{
					#region Fail
					filestream?.Dispose();
					workingMode = Constants.WorkingMode.C_WORKMODE_NONE;
					RaiseOnTransferError();
					#endregion
				}
			}
		}

	}
}
