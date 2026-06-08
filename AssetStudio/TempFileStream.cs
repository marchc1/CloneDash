using System.IO;
using System.Runtime.InteropServices;

namespace AssetStudio
{
    public class TempFileStream : FileStream
    {
        private readonly string _tempFilePath;
        private bool _disposed;

        public TempFileStream(string path, FileMode fileMode, FileAccess fileAccess = FileAccess.ReadWrite, FileShare fileShare = FileShare.Read, int bufferSize = 4096)
            : base(path, fileMode, fileAccess, fileShare, bufferSize, FileOptions.DeleteOnClose)
        {
            _tempFilePath = path;
        }

        public override void Close()
        {
            Dispose(true);
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed) 
                return;

            base.Dispose(disposing);
            
            if (disposing)
            {
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && File.Exists(_tempFilePath))
                {
                    File.Delete(_tempFilePath);
                }

                _disposed = true;
            }
        }
    }
}
