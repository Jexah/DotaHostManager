using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;

namespace DotaHostClientLibrary
{
    public class DownloadManager
    {
        // Our download manager
        private WebClient dlManager = new WebClient();

        // This is a queue of files to download. They are stored in the format of: [downloadLocation, targetFile, typeOfDownload, targetFile]
        private List<DownloadInstruction> toDownload = new List<DownloadInstruction>();

        // This is an object representative of the current download
        private DownloadInstruction currentDownload;

        public DownloadManager()
        {
            // Hook the download functions
            dlManager.DownloadProgressChanged += dlManager_DownloadProgressChanged;
            dlManager.DownloadFileCompleted += dlManager_DownloadFileCompleted;
        }

        // Request sync download with given parameters
        public void downloadSync(string sourceFile, string targetFile)
        {
            Helpers.log("[Download] Begin: " + sourceFile + " -> " + targetFile);
            dlManager.DownloadFile(sourceFile, targetFile);
            Helpers.log("[Download] Complete: " + sourceFile + " -> " + targetFile);
        }

        // Begin or queue download instruction
        public void download(string sourceFile, string targetFile, DownloadProgressDel downloadProgress, DownloadCompleteDel downloadComplete)
        {
            // Prepares the instruction
            DownloadInstruction dlInstruction = new DownloadInstruction(sourceFile, targetFile, downloadProgress, downloadComplete);

            // Checks if there are any downloads int the queue
            if (toDownload.Count == 0)
            {
                // Sets the current download to the instruction provided
                currentDownload = dlInstruction;

                // Begins the current download
                beginDownload();
            }
            else
            {
                // Adds the instruction to the list
                toDownload.Add(dlInstruction);
            }
        }

        // Begins download of selected "currentDownload"
        private void beginDownload()
        {
            Helpers.log("[Download] Begin: " + currentDownload.SourceFile + " -> " + currentDownload.TargetFile);
            dlManager.DownloadFileAsync(new Uri(currentDownload.SourceFile), currentDownload.TargetFile);
        }

        // Download progress hook
        private void dlManager_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            currentDownload.DownloadProgress(e);
        }

        // Download complete hook
        private void dlManager_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            Helpers.log("[Download] Complete");

            currentDownload.DownloadComplete(e);

            // Starts the next download in the queue
            beginNext();
        }

        // Starts the next download in the queue
        private void beginNext()
        {
            // If queue is not empty
            if (toDownload.Count > 0)
            {
                // Take the first item in the list and store it
                DownloadInstruction next = toDownload[0];

                // Remove the first item in the list
                toDownload.RemoveAt(0);

                // Set the current download to the item removed from the front of the list
                currentDownload = next;

                // Begins the next download
                beginDownload();
            }
        }


    }
}