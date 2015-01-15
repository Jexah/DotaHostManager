using System.ComponentModel;
using System.Net;

namespace DotaHostClientLibrary
{
    // Define the delegates in a public scope so everything can use them
    public delegate void DownloadProgressDel(DownloadProgressChangedEventArgs e);
    public delegate void DownloadCompleteDel(AsyncCompletedEventArgs e);

    public class DownloadInstruction
    {
        private readonly string _sourceFile;
        public string SourceFile
        {
            get
            {
                return _sourceFile;
            }
        }

        private readonly string _targetFile;
        public string TargetFile
        {
            get
            {
                return _targetFile;
            }
        }

        private readonly DownloadProgressDel _downloadProgress;
        public DownloadProgressDel DownloadProgress
        {
            get
            {
                return _downloadProgress;
            }
        }

        private readonly DownloadCompleteDel _downloadComplete;
        public DownloadCompleteDel DownloadComplete
        {
            get
            {
                return _downloadComplete;
            }
        }

        public DownloadInstruction(string sourceFile, string targetFile, DownloadProgressDel downloadProgress, DownloadCompleteDel downloadComplete)
        {
            this._sourceFile = sourceFile;
            this._targetFile = targetFile;

            this._downloadProgress = downloadProgress;
            this._downloadComplete = downloadComplete;
        }

    }
}
