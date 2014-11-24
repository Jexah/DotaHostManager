
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net;

namespace DotaHostBoxManager
{
    class Program
    {
        // Where this executable is run from
        static string g_BASEPATH = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\";

        // The steam cmd file to run steam commands with
        static readonly String STEAMCMD = "steamcmd.exe";

        // The path to download steamcmd from
        static readonly String STEAMCMD_DOWNLOAD_PATH = "http://media.steampowered.com/installer/steamcmd.zip";

        // Used for downloading files
        static WebClient dlManager = new WebClient();
           
        // The main entry point into the program
        static void Main(string[] args)
        {
            // Check if steamcmd exists
            if (File.Exists(g_BASEPATH+STEAMCMD))
            {
                log("It exists");
            }
            else
            {
                log("We need to download steamcmd.");
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
