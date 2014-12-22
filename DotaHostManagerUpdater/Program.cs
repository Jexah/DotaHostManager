using DotaHostClientLibrary;
using System;
using System.Diagnostics;
using System.Net;

namespace DotaHostManagerUpdater
{
    class Program
    {
        // Download manager
        private static WebClient dlManager = new WebClient();

        private static string basePath;
        private static string version;

        static void Main(string[] args)
        {
            basePath = args[0];
            version = args[1];
            update();
        }

        private static void update()
        {
            try
            {
                Console.WriteLine(basePath + "DotaHostManager.exe");
                Console.WriteLine(Global.DOWNLOAD_PATH_APP);
                dlManager.DownloadFile(new Uri(Global.DOWNLOAD_PATH_APP.Replace("{0}", version)), basePath + @"\DotaHostManager.exe");
                ProcessStartInfo proc = new ProcessStartInfo();
                proc.UseShellExecute = true;
                proc.WorkingDirectory = basePath;
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
