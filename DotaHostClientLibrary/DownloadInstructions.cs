using System.ComponentModel;
using System.Net;

namespace DotaHostClientLibrary
{
    // Define the delegates in a public scope so everything can use them
    public delegate void DownloadProgressDel(DownloadProgressChangedEventArgs e);
    public delegate void DownloadCompleteDel(AsyncCompletedEventArgs e);

    public class DownloadInstruction
    {
        private string sourceFile;
        public string SourceFile
        {
            get
            {
                return sourceFile;
            }
        }

        private string targetFile;
        public string TargetFile
        {
            get
            {
                return targetFile;
            }
        }

        private DownloadProgressDel downloadProgress;
        public DownloadProgressDel DownloadProgress
        {
            get
            {
                return downloadProgress;
            }
        }

        private DownloadCompleteDel downloadComplete;
        public DownloadCompleteDel DownloadComplete
        {
            get
            {
                return downloadComplete;
            }
        }

        public DownloadInstruction(string sourceFile, string targetFile, DownloadProgressDel downloadProgress, DownloadCompleteDel downloadComplete)
        {
            this.sourceFile = sourceFile;
            this.targetFile = targetFile;

            this.downloadProgress = downloadProgress;
            this.downloadComplete = downloadComplete;
        }

    }
}
