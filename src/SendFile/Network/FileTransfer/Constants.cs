using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RemoteFile.Interface.Network.FileTransfer
{
	public class Constants
	{
		public const int C_BUFFER_SIZE = 1024 * 1024;

		public enum WorkingMode
		{
			C_WORKMODE_NONE = 0,
			C_WORKMODE_SEND = 1,
			C_WORKMODE_RECEIVE = 2,
		}
}
}
