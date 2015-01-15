using DotaHostClientLibrary;
using System;
using System.Diagnostics;
using System.IO;

namespace DotaHostManagerUpdater
{
    class Program
    {
        // Download manager
        private static readonly DownloadManager DlManager = new DownloadManager();

        static void Main()
        {
            Update();
        }

        private static void Update()
        {
            try
            {
                // Download version info
                DlManager.DownloadSync(string.Format(Global.DownloadPathAddonInfo, "DotaHostManager"), Global.Temp + "DotaHostManager.txt");

                // Store version info
                var versionCrc = File.ReadAllLines(Global.Temp + "DotaHostManager.txt");

                // Download manager
                DlManager.DownloadSync(string.Format(Global.DownloadPathApp, versionCrc[0]), Global.BasePath + @"DotaHostManager.exe");

                var proc = new ProcessStartInfo
                {
                    UseShellExecute = true,
                    WorkingDirectory = Global.BasePath,
                    FileName = "DotaHostManager.exe"
                };
                try
                {
                    Process.Start(proc);
                    Environment.Exit(0);
                }
                catch
                {
                    Timers.SetTimeout(1, Timers.Seconds, Update);
                }
            }
            catch
            {
                Timers.SetTimeout(1, Timers.Seconds, Update);
            }
        }
    }
}
