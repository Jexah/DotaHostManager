using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DotaHostClientLibrary
{
    public static class AddonDownloader
    {
        // Delegate for download progress
        public delegate void DelegateOnProgress(string addonID, DownloadProgressChangedEventArgs e);

        // Delegate for when a download completes
        public delegate void DelegateOnComplete(string addonID, bool success);

        // The download manager
        private static DownloadManager dlManager = new DownloadManager();

        // The location to install addons to (note: this can be changed via setAddonInstallLocation)
        private static string addonInstallLocation = Global.BASE_PATH + @"addonZips\";

        // Changes where addons are installed to
        public static void setAddonInstallLocation(string newInstallLocation)
        {
            // Change the install location
            addonInstallLocation = newInstallLocation;
        }

        // Gets the addon path
        public static string getAddonInstallLocation()
        {
            return addonInstallLocation;
        }

        // Begins download of addonInfo for given addon
        public static void updateAddon(string addonID, DelegateOnComplete onComplete, DelegateOnProgress onProgress = null)
        {
            // The path to the info file
            string downloadPath = string.Format(Global.DOWNLOAD_PATH_ADDON_INFO, addonID);

            Helpers.log("[Downloading] " + downloadPath);
            dlManager.download(downloadPath, Global.TEMP + addonID, (e) =>
            {
                // Check if we have a callback
                if (onProgress != null)
                {
                    // yep, run it
                    onProgress(addonID, e);
                }
            }, (e) =>
            {
                downloadAddonInfoComplete(addonID, onComplete, onProgress);
            });
        }

        private static void downloadAddonInfoComplete(string addonID, DelegateOnComplete onComplete, DelegateOnProgress onProgress = null)
        {
            // Sets up properties from arguments and addonInfo file
            string[] info = File.ReadAllLines(Global.TEMP + addonID);
            Helpers.deleteSafe(Global.TEMP + addonID);

            // Verify the size of the info packet
            if(info.Length != 2)
            {
                Helpers.log("ERROR: Infopacket for " + addonID + " is corrupted! Got " + info.Length + " lines instead of 2.");
                onComplete(addonID, false);
                return;
            }

            string version = info[0];
            string correctCRC = info[1];

            // Check if the addon is already downloaded
            if (File.Exists(addonInstallLocation + addonID + ".zip"))
            {
                // Check the CRC
                string actualCRC = Helpers.calculateCRC(addonInstallLocation + addonID + ".zip");

                // If it matches, we're already upto date
                if (actualCRC == correctCRC)
                {
                    Helpers.log("[Addon] " + addonID + " is up to date.");
                    return;
                }
            }

            // The path to te download
            string downloadPath = string.Format(Global.DOWNLOAD_PATH_ADDONS, addonID, version);

            // Directory or file does not exist, or version does not match most recent
            Helpers.log("[Addon] " + addonID + " out of date. New version: " + version);
            Helpers.log("[Downloading] " + downloadPath);

            // Begins downloading addon from GitHub
            dlManager.download(downloadPath, Global.TEMP + addonID + ".zip", (e) =>
            {
                // Check if we have a callback
                if (onProgress != null)
                {
                    // yep, run it
                    onProgress(addonID, e);
                }
            }, (e) =>
            {
                // Run the completion function
                downloadAddonComplete(addonID, version, correctCRC, onComplete);
            });
        }

        // Function run when an addon finishes getting downloaded
        private static void downloadAddonComplete(string addonID, string version, string correctCRC, DelegateOnComplete onComplete)
        {
            // Deletes current addon zip (if it exists)
            Helpers.deleteSafe(addonInstallLocation + addonID + ".zip");

            // Generates new CRC of the zip just downloaded
            string actualCRC = Helpers.calculateCRC(Global.TEMP + addonID + ".zip");
            Helpers.log("CRC: " + correctCRC + " == " + actualCRC);

            // Matches the generated CRC with the downloaded CRC
            if (correctCRC != actualCRC)
            {
                // Installation has failed, send formatted string to most recent connection
                Helpers.log("[CRC] Mismatch!");
                Helpers.log(" == Installation failed! == ");

                // Attempt to run the callback
                if (onComplete != null)
                {
                    onComplete(addonID, false);
                }
            }
            else
            {
                // CRC check has passed, download was successful
                Helpers.log(" == Installation successful! == ");

                // Ensure the path exists
                Directory.CreateDirectory(addonInstallLocation);

                // Copy the file to the addons folder
                File.Copy(Global.TEMP + addonID + ".zip", addonInstallLocation + addonID + ".zip");

                // Attempt to run the callback
                if (onComplete != null)
                {
                    onComplete(addonID, true);
                }
            }

            // Deletes the downloaded zip file
            Helpers.log("[Cleaning] Cleaning up...");
            Helpers.deleteSafe(Global.TEMP + addonID + ".zip");
            Helpers.log("[Cleaning] Done!");
            Helpers.log("[Socket] Sending confirmation update!");
        }
    }
}
