using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RemoteFile.Interface.Network.FileTransfer
{
	public class FileClientBase
	{
		public event ProgressHandler onProgress;
		public event StartTransferHandler onStartTransfer;
		public event FinishTransferHandler onFinishTransfer;
		public event TransferErrorHandler onTransferError;

		protected string fileName;

		private object syncObj = new object();
		private Constants.WorkingMode _workingMode = Constants.WorkingMode.C_WORKMODE_NONE;

		protected Constants.WorkingMode workingMode
		{
            get
            {
                lock (syncObj)
                {
					return _workingMode;
				}
			}
			set
			{
                lock (syncObj)
                {
					if (value != _workingMode)
					{
						if (_workingMode == Constants.WorkingMode.C_WORKMODE_NONE)
						{
							Task.Run(() => manageTimeoutCyle());
						}
						else
						{
							InTimeoutCyle = false;
						}
					}
					_workingMode = value;
				}
			}
		}
		private int _timeoutCycle = 500;
		protected int TimeoutCycle
		{
			get
			{
				lock (syncObj)
				{
					return _timeoutCycle;
				}
			}
			set
			{
				lock (syncObj)
				{
					_timeoutCycle = value;
				}
			}
		}
		private int _maxTimeoutCycle = 1500;
		protected int MaxTimeoutCycle
		{
			get
			{
				lock (syncObj)
				{
					return _maxTimeoutCycle;
				}
			}
			set
			{
				lock (syncObj)
				{
					_maxTimeoutCycle = value;
				}
			}
		}
		private bool _inTimeoutCycle = false;
		protected bool InTimeoutCyle
		{
			get
			{
				lock (syncObj)
				{
					return _inTimeoutCycle;
				}
			}
			set
			{
				lock (syncObj)
				{
					_inTimeoutCycle = value;
				}
			}
		}

		private DateTime _lastEvent = DateTime.Now;
		protected DateTime LastEvent
		{
			get
			{
				lock (syncObj)
				{
					return _lastEvent;
				}
			}
			set
			{
				lock (syncObj)
				{
					_lastEvent = value;
				}
			}
		}

		private void manageTimeoutCyle()
		{
			InTimeoutCyle = true;
			LastEvent = DateTime.Now;
			while (InTimeoutCyle)
			{
				if ((DateTime.Now - LastEvent).TotalMilliseconds > MaxTimeoutCycle)
				{
					Task.Run(() => manageTimeoutError());
				}

				Thread.Sleep(TimeoutCycle);
			}
		}

		private void manageTimeoutError()
		{
			RaiseOnTransferError();
			workingMode = Constants.WorkingMode.C_WORKMODE_NONE;
			InTimeoutCyle = false;
		}

		protected FileStream filestream;
		protected TcpClient client;
		protected NetworkStream stream;
		protected int receivedFileLen = 0;
		private EndTransferhandler endTransferAction = null;
		public EndTransferhandler EndTransferAction { get { return endTransferAction; } set { endTransferAction = value; } }

		public string HostAddress { get; set; }

		protected void RaiseProgressEvent(ProgressEventArgs e)
		{
			e.ServerAddress = HostAddress;
			onProgress?.Invoke(this, e);
		}
		protected void RaiseOnStartTransfer(TransferStartEventArgs e)
		{
			e.ServerAddress = HostAddress;
			onStartTransfer?.Invoke(this, e);
		}
		protected void RaiseOnFinishTransfer()
		{
			onFinishTransfer?.Invoke(this, new TransferFinishedEventArgs() { FileName = this.fileName, Total = receivedFileLen, ServerAddress = HostAddress });
		}
		protected void RaiseOnTransferError()
		{
			onTransferError?.Invoke(this, new TransferErrorEventArgs() { FileName = fileName, Error = 0, ServerAddress = HostAddress });
		}

		public virtual void disconnect()
		{
			stream.Close();
			stream.Dispose();

			try
			{
				client?.Client?.Disconnect(false);
			}
			catch
			{
			}
			client?.Close();
		}
		
		public virtual void beginRead()
		{
            try
            {
				if (stream?.CanRead == true)
				{
					AsyncState state = new AsyncState();
					state.client = client;
					state.buffer = new byte[Constants.C_BUFFER_SIZE];
					stream.BeginRead(state.buffer, 0, state.buffer.Length, readCallback, state);
				}
			}
			catch (Exception)
            {
				manageTimeoutError();
				this.client.Close();
			}
		}

		protected virtual void readCallback(IAsyncResult ar)
		{
			AsyncState state = ar.AsyncState as AsyncState;
			state.dataSize = stream.EndRead(ar);
			processData(state);
			beginRead();
		}
		public virtual void write(byte[] data)
		{
            try
            {
				if (stream?.CanWrite == true)
					stream.Write(data, 0, data.Length);
			}
			catch(Exception)
            {
				manageTimeoutError();
				this.client.Close();
			}
		}
		protected virtual void doReceieveOperation(AsyncState state)
		{
			byte[] data = new byte[state.dataSize];

			Array.Copy(state.buffer, data, data.Length);

			bool EOF = false;
			int writeLen = data.Length;
			if (data.Length == 3)
			{
				if (data[0] == 'E' && data[1] == 'O' && data[2] == 'F')
				{
					EOF = true;
					writeLen = 0;
				}
			}
			else if (data.Length > 3)
			{
				if (data[data.Length - 3] == 'E' && data[data.Length - 2] == 'O' && data[data.Length - 1] == 'F')
				{
					EOF = true;
					writeLen -= 3;
				}
			}


			if (writeLen > 0)
			{
				filestream.Write(data, 0, writeLen);

				if (!EOF)
					write(new byte[] { (byte)'O' });
			}

			if (EOF)
			{
				RaiseOnFinishTransfer();
				filestream.Close();
				filestream.Dispose();
				filestream = null;

				workingMode = Constants.WorkingMode.C_WORKMODE_NONE;
			}
			else
				onProgress?.Invoke(this, new ProgressEventArgs() { FileName = fileName, Position = filestream.Length, Total = receivedFileLen });
		}
		protected virtual void doSendOperation()
		{
			if (null != filestream)
			{
				System.Threading.Thread.Sleep(25);

				byte[] data;
				byte[] fileData = new byte[Constants.C_BUFFER_SIZE];        // flag + len

				int size = filestream.Read(fileData, 0, fileData.Length);

				if (size > 0)
				{
					data = null;

					#region Prepare data
					byte[] sendData = new byte[size];
					Array.Copy(fileData, sendData, size);


					MemoryStream ms = new MemoryStream();
					BinaryWriter bw = new BinaryWriter(ms);

					bw.Write(sendData);

					data = ms.ToArray();
					bw.Close();
					ms.Dispose();
					#endregion

					write(data);

					onProgress?.Invoke(this, new ProgressEventArgs() { FileName = fileName, Position = filestream.Position, Total = filestream.Length, ServerAddress = HostAddress });
				}
				else
				{
					filestream.Close();
					write(new byte[] { (byte)'E', (byte)'O', (byte)'F' });
					workingMode = Constants.WorkingMode.C_WORKMODE_NONE;
					onFinishTransfer?.Invoke(this, new TransferFinishedEventArgs() { FileName = fileName, Total = receivedFileLen });

					endTransferAction?.Invoke();
				}
			}
			else
			{
				write(new byte[] { (byte)'F' });
				workingMode = Constants.WorkingMode.C_WORKMODE_NONE;
			}
		}

		protected virtual void processData(AsyncState state) 
		{
			LastEvent = DateTime.Now;
		}
	}
}