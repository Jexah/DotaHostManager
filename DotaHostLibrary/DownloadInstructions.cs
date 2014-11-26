using System.ComponentModel;
using System.Net;

namespace DotaHostLibrary
{
    // Define the delegates in a public scope so everything can use them
    public delegate void DownloadProgressDel(DownloadProgressChangedEventArgs e);
    public delegate void DownloadCompleteDel(AsyncCompletedEventArgs e);

    public class DownloadInstruction
    {
        private string sourceFile;
        private string targetFile;

        private DownloadProgressDel downloadProgressDel;
        private DownloadCompleteDel downloadCompleteDel;

        public DownloadInstruction(string sourceFile, string targetFile, DownloadProgressDel downloadProgressDel, DownloadCompleteDel downloadCompleteDel)
        {
            this.sourceFile = sourceFile;
            this.targetFile = targetFile;

            this.downloadProgressDel = downloadProgressDel;
            this.downloadCompleteDel = downloadCompleteDel;
        }

        public string getSourceFile()
        {
            return this.sourceFile;
        }

        public string getTargetFile()
        {
            return this.targetFile;
        }

        public DownloadProgressDel getDownloadProgress()
        {
            return this.downloadProgressDel;
        }

        public DownloadCompleteDel getDownloadComplete()
        {
            return this.downloadCompleteDel;
        }
    }
}
