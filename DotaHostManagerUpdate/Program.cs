using System;
using System.Net;
using System.IO;

namespace DotaHostManagerUpdater
{
    class Program
    {
        static WebClient dlManager = new WebClient();
        const string g_ROOT = "localhost/";
        static void Main(string[] args)
        {
            string g_BASEPATH = String.Empty;
            try
            {

                g_BASEPATH = args[0];
            }
            catch
            {
                File.WriteAllText("errorlog.txt", "Failed to write to file:\n" + g_BASEPATH);
                Environment.Exit(0);
            }
            try
            {
                //dlManager.DownloadFile(new Uri(g_ROOT + "downloads/software/moddota/moddota.exe"), g_BASEPATH + "moddota.exe");
            }
            catch
            {
                File.WriteAllText("errorlog.txt", "Failed to write to file:\n" + g_BASEPATH);
                Environment.Exit(0);
            }
        }
    }
}
