using DotaHostClientLibrary;
using System;
using System.Diagnostics;
using System.IO;

namespace DotaHostManagerUpdater
{
    class Program
    {
        // Download manager
        private static DownloadManager dlManager = new DownloadManager();

        private static string version;

        static void Main(string[] args)
        {
            Console.WriteLine(string.Join(" ", args));
            version = args[0];
            update();
        }

        private static void update()
        {
            try
            {
                // Download version info
                dlManager.downloadSync(string.Format(Global.DOWNLOAD_PATH_ADDON_INFO, "DotaHostManager"), Global.TEMP + "DotaHostManager.txt");

                // Store version info
                string[] versionCRC = File.ReadAllLines(Global.TEMP + "DotaHostManager.txt");

                // Download manager
                dlManager.downloadSync(string.Format(Global.DOWNLOAD_PATH_APP, versionCRC[0]), Global.BASE_PATH + @"DotaHostManager.exe");

                ProcessStartInfo proc = new ProcessStartInfo();
                proc.UseShellExecute = true;
                proc.WorkingDirectory = Global.BASE_PATH;
                proc.FileName = "DotaHostManager.exe";
                try
                {
                    Process.Start(proc);
                    Environment.Exit(0);
                }
                catch
                {
                    Timers.setTimeout(1, Timers.SECONDS, update);
                }
            }
            catch
            {
                Timers.setTimeout(1, Timers.SECONDS, update);
            }
        }
    }
}
