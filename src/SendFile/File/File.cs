using RemoteFile.Interface.Network;
using RemoteFile.Interface.Network.FileTransfer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace RemoteFile.Interface.File
{
    public class FileException : Exception
    {
        public FileException(string message) : base(message)
        {
        }
    }

    public class FileLoadException : FileException
    {
        public FileLoadException(string message) : base(message)
        {
        }
    }
    public class FileExocadFileNotExistsException : FileLoadException
    {
        public FileExocadFileNotExistsException(string message) : base(message)
        {
        }
    }
    public class FileSaveException : FileException
    {
        public FileSaveException(string message) : base(message)
        {
        }
    }
    public class EmptyFileException : FileSaveException
    {
        public EmptyFileException(string message) : base(message)
        {
        }
    }

    public class FileFactory
    {
        private static FileFactory instance = null;
        public static FileFactory Instance
        {
            get { return instance; }
            set { instance = value; }
        }

        public virtual File CreateFile(string fileName)
        {
            FileInfo info = new FileInfo(fileName);
            string ext = info.Extension;
            // eventually place here a switch to create the rigth file class based on the extension
            return new GenericFile();
        }
    }

    public abstract class File
    {
        public event ProgressHandler onProgress;
        public event TransferErrorHandler onError;
        public void RaiseOnProgress(ProgressEventArgs e) { onProgress?.Invoke(this, e); }
        public void RaiseOnError(TransferErrorEventArgs e) { onError?.Invoke(this, e); }
        public string Path { get; set; }
        public string RemotePath { get; set; }
        public abstract void Load();
        public abstract void Save(string path);
        public abstract void PrepareForTransfer();
        public abstract void Transfer(ConnectionInfo info);
    }

    public class GenericFile : File
    {
        public override void Load()
        {
        }

        public override void PrepareForTransfer()
        {
        }

        public override void Save(string path)
        {
        }

        public override void Transfer(ConnectionInfo info)
        {
            FileClient c = new FileClient();
            c.connect(info);
            c.onProgress += (obj, e) => { RaiseOnProgress(e); };
            c.onTransferError += (obj, e) => { RaiseOnError(e); };
            c.sendFile(Path);
        }
    }

}
