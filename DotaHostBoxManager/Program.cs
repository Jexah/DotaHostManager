
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Compression;
using System.Net;

namespace DotaHostBoxManager
{
    class Program
    {
        // Where this executable is run from
        static string g_BASEPATH = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\";

        // The steam cmd file to run steam commands with
        static readonly String STEAMCMD = "steamcmd.exe";

        // The zip containing steamcmd
        static readonly String STEAMCMD_ZIP = "steamcmd.zip";

        // The path to download steamcmd from
        static readonly String STEAMCMD_DOWNLOAD_PATH = "http://media.steampowered.com/installer/steamcmd.zip";

        // Used for downloading files
        static WebClient dlManager = new WebClient();
           
        // The main entry point into the program
        static void Main(string[] args)
        {
            // Delete the old log file
            File.Delete(g_BASEPATH + "log.txt");

            // Ensure steamcmd exists
            verifySteamcmd();


        }

        // This function ensures steamcmd is available
        static void verifySteamcmd()
        {
            // Check if steamcmd exists
            if (!File.Exists(g_BASEPATH + STEAMCMD))
            {
                // If there is an old version of steamcmd.zip, delete it
                File.Delete(g_BASEPATH + STEAMCMD_ZIP);

                // NOTE: WE NEED TO CATCH EXCEPTIONS HERE INCASE STEAM UNREACHABLE!

                // Download steamcmd zip
                dlManager.DownloadFile(STEAMCMD_DOWNLOAD_PATH, STEAMCMD_ZIP);

                // Extract the archive
                ZipFile.ExtractToDirectory(STEAMCMD_ZIP, g_BASEPATH);

                // Delete the zip
                File.Delete(g_BASEPATH + STEAMCMD_ZIP);
            }
        }

        // Outputs to the console and stores a copy into log.txt
        static void log(string str)
        {
            Console.WriteLine(str);
            File.AppendAllText(g_BASEPATH + "log.txt", str + "\n");
        }
    }
}
