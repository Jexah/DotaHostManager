using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DotaHostLibrary
{
    public delegate void DownloadProgressDel(string[] args, DownloadProgressChangedEventArgs e);
    public delegate void DownloadCompleteDel(string[] args, AsyncCompletedEventArgs e);

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
