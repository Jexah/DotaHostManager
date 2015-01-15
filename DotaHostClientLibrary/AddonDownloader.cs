using System.IO;
using System.Net;

namespace DotaHostClientLibrary
{
    public static class AddonDownloader
    {
        // Delegate for download progress
        public delegate void DelegateOnProgress(string addonId, DownloadProgressChangedEventArgs e);

        // Delegate for when a download completes
        public delegate void DelegateOnComplete(string addonId, bool success);

        // The download manager
        private static readonly DownloadManager DlManager = new DownloadManager();

        // The location to install addons to (note: this can be changed via setAddonInstallLocation)
        private static string _addonInstallLocation = Global.BasePath + @"addons_dotahost\";

        // Changes where addons are installed to
        public static void SetAddonInstallLocation(string newInstallLocation)
        {
            // Change the install location
            _addonInstallLocation = newInstallLocation;
        }

        // Gets the addon path
        public static string GetAddonInstallLocation()
        {
            return _addonInstallLocation;
        }

        // Begins download of addonInfo for given addon
        public static void UpdateAddon(string addonId, DelegateOnComplete onComplete, DelegateOnProgress onProgress = null, string altPath = "")
        {
            // The path to the info file
            var downloadPath = string.Format(Global.DownloadPathAddonInfo, addonId);

            Helpers.Log("[Downloading] " + downloadPath);
            DlManager.Download(downloadPath, (altPath == "" ? Global.Temp + addonId : altPath), e =>
            {
                // Check if we have a callback
                if (onProgress != null)
                {
                    // yep, run it
                    onProgress(addonId, e);
                }
            }, e => DownloadAddonInfoComplete(addonId, onComplete, onProgress));
        }

        private static void DownloadAddonInfoComplete(string addonId, DelegateOnComplete onComplete, DelegateOnProgress onProgress = null)
        {
            // Sets up properties from arguments and addonInfo file
            string[] info = File.ReadAllLines(Global.Temp + addonId);
            Helpers.DeleteSafe(Global.Temp + addonId);

            // Verify the size of the info packet
            if (info.Length != 2)
            {
                Helpers.Log("ERROR: Infopacket for " + addonId + " is corrupted! Got " + info.Length + " lines instead of 2.");
                onComplete(addonId, false);
                return;
            }

            var downloadLink = info[0];
            var correctCrc = info[1];

            // Check if the addon is already downloaded
            if (File.Exists(_addonInstallLocation + addonId + ".zip"))
            {
                // Check the CRC
                var actualCrc = Helpers.CalculateCrc(_addonInstallLocation + addonId + ".zip");

                // If it matches, we're already upto date
                if (actualCrc == correctCrc)
                {
                    Helpers.Log("[Addon] " + addonId + " is up to date.");
                    onComplete(addonId, true);
                    return;
                }
            }

            // Directory or file does not exist, or version does not match most recent
            Helpers.Log("[Addon] " + addonId + " out of date. New version: " + correctCrc);
            Helpers.Log("[Downloading] " + downloadLink);

            // Begins downloading addon from GitHub
            DlManager.Download(downloadLink, Global.Temp + addonId + ".zip", e =>
            {
                // Check if we have a callback
                if (onProgress != null)
                {
                    // yep, run it
                    onProgress(addonId, e);
                }
            }, e => DownloadAddonComplete(addonId, correctCrc, onComplete));
        }

        // Function run when an addon finishes getting downloaded
        private static void DownloadAddonComplete(string addonId, string correctCrc, DelegateOnComplete onComplete)
        {
            // Deletes current addon zip (if it exists)
            Helpers.DeleteSafe(_addonInstallLocation + addonId + ".zip");

            // Generates new CRC of the zip just downloaded
            string actualCrc = Helpers.CalculateCrc(Global.Temp + addonId + ".zip");
            Helpers.Log("CRC: " + correctCrc + " == " + actualCrc);

            // Matches the generated CRC with the downloaded CRC
            if (correctCrc != actualCrc)
            {
                // Installation has failed, send formatted string to most recent connection
                Helpers.Log("[CRC] Mismatch!");
                Helpers.Log(" == Installation failed! == ");

                // Attempt to run the callback
                if (onComplete != null)
                {
                    onComplete(addonId, false);
                }
            }
            else
            {
                // CRC check has passed, download was successful
                Helpers.Log(" == Installation successful! == ");

                // Ensure the path exists
                Directory.CreateDirectory(_addonInstallLocation);

                // Delete the old addon_name.zip
                File.Delete(_addonInstallLocation + addonId + ".zip");

                // Copy the file to the addons folder
                File.Move(Global.Temp + addonId + ".zip", _addonInstallLocation + addonId + ".zip");

                // Attempt to run the callback
                if (onComplete != null)
                {
                    onComplete(addonId, true);
                }
            }

            // Deletes the downloaded zip file
            Helpers.Log("[Cleaning] Cleaning up...");
            Helpers.Log("[Cleaning] Done!");
            Helpers.Log("[Socket] Sending confirmation update!");
        }
    }
}
