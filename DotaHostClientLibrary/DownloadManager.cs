using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;

namespace DotaHostClientLibrary
{
    public class DownloadManager
    {
        // Our download manager
        private readonly WebClient _dlManager = new WebClient();

        // This is a queue of files to download. They are stored in the format of: [downloadLocation, targetFile, typeOfDownload, targetFile]
        private readonly List<DownloadInstruction> _toDownload = new List<DownloadInstruction>();

        // This is an object representative of the current download
        private DownloadInstruction _currentDownload;

        public DownloadManager()
        {
            // Hook the download functions
            _dlManager.DownloadProgressChanged += dlManager_DownloadProgressChanged;
            _dlManager.DownloadFileCompleted += dlManager_DownloadFileCompleted;
        }

        // Request sync download with given parameters
        public void DownloadSync(string sourceFile, string targetFile)
        {
            Helpers.Log("[Download] Begin: " + sourceFile + " -> " + targetFile);
            var dlManagerTemp = new WebClient();
            dlManagerTemp.DownloadFile(sourceFile, targetFile);
            dlManagerTemp.Dispose();
            Helpers.Log("[Download] Complete: " + sourceFile + " -> " + targetFile);
        }

        // Begin or queue download instruction
        public void Download(string sourceFile, string targetFile, DownloadProgressDel downloadProgress, DownloadCompleteDel downloadComplete)
        {
            // Prepares the instruction
            var dlInstruction = new DownloadInstruction(sourceFile, targetFile, downloadProgress, downloadComplete);

            // Checks if there are any downloads int the queue
            if (_currentDownload == null && _toDownload.Count == 0)
            {
                // Sets the current download to the instruction provided
                _currentDownload = dlInstruction;

                // Begins the current download
                BeginDownload();
            }
            else
            {
                // Adds the instruction to the list
                _toDownload.Add(dlInstruction);
            }
        }

        // Begins download of selected "currentDownload"
        private void BeginDownload()
        {
            Helpers.Log("[Download] Begin: " + _currentDownload.SourceFile + " -> " + _currentDownload.TargetFile);
            _dlManager.DownloadFileAsync(new Uri(_currentDownload.SourceFile), _currentDownload.TargetFile);
        }

        // Download progress hook
        private void dlManager_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            _currentDownload.DownloadProgress(e);
        }

        // Download complete hook
        private void dlManager_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            Helpers.Log("[Download] Complete");

            _currentDownload.DownloadComplete(e);

            // No longer downloading anything
            _currentDownload = null;

            // Starts the next download in the queue
            BeginNext();
        }

        // Starts the next download in the queue
        private void BeginNext()
        {
            // If queue is not empty
            if (_toDownload.Count <= 0) return;

            // Take the first item in the list and store it
            var next = _toDownload[0];

            // Remove the first item in the list
            _toDownload.RemoveAt(0);

            // Set the current download to the item removed from the front of the list
            _currentDownload = next;

            // Begins the next download
            BeginDownload();
        }


    }
}