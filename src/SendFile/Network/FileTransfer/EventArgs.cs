using System;

namespace RemoteFile.Interface.Network.FileTransfer
{
	public class ProgressEventArgs : EventArgs
	{
		public string ServerAddress { get; set; }
		public string FileName { get; set; }
		public long Position { get; set; }
		public long Total { get; set; }
	}

	public class TransferStartEventArgs : EventArgs
	{
		public string ServerAddress { get; set; }
		public string FileName { get; set; }
		public long Total { get; set; }
	}

	public class TransferFinishedEventArgs : EventArgs
	{
		public string ServerAddress { get; set; }
		public string FileName { get; set; }
		public long Total { get; set; }
	}
	public class TransferErrorEventArgs : EventArgs
	{
		public string ServerAddress { get; set; }
		public string FileName { get; set; }
		public int Error { get; set; }
	}

	public delegate void ProgressHandler(object sender, ProgressEventArgs e);
	public delegate void StartTransferHandler(object sender, TransferStartEventArgs e);
	public delegate void FinishTransferHandler(object sender, TransferFinishedEventArgs e);
	public delegate void TransferErrorHandler(object sender, TransferErrorEventArgs e);
	public delegate void EndTransferhandler();
}