﻿using System;
using System.Net;
using System.IO;
using System.Diagnostics;

namespace DotaHostManagerUpdater
{
    class Program
    {
        // Download manager
        static WebClient dlManager = new WebClient();

        // Website root
        const string ROOT = "https://dl.dropboxusercontent.com/u/25095474/dotahost/";

        static void Main(string[] args)
        {
            try
            {
                string basePath = args[0];
                Console.WriteLine(basePath + "DotaHostManager.exe");
                Console.WriteLine(ROOT + "/static/software/dotahostmanager/DotaHostManager.exe");
                dlManager.DownloadFile(new Uri(ROOT + "/static/software/dotahostmanager/DotaHostManager.exe"), basePath + @"\DotaHostManager.exe");
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

                }
            }catch{ }
        }
    }
}