
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DotaHostBoxManager
{
    class Program
    {
        // Where this executable is run from
        static string g_BASEPATH = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\";

        // The path to steamcmd
        static readonly String STEAMCMD_PATH = "steamcmd\\";

        // The steam cmd file to run steam commands with
        static readonly String STEAMCMD = STEAMCMD_PATH + "steamcmd.exe";

        // The path to depot downloader
        static readonly String DEPOT_DOWNLOADER_PATH = "DepotDownloader\\";

        // The execute to run depot downloader
        static readonly String DEPOT_DOWNLOADER = DEPOT_DOWNLOADER_PATH + "DepotDownloader.exe";

        // The path to download steamcmd from
        static readonly String DOWNLOAD_PATH_STEAMCMD = "http://media.steampowered.com/installer/steamcmd.zip";

        // The path to download depot downloader
        static readonly String DOWNLOAD_PATH_DEPOT_DOWNLOADER = "https://github.com/SteamRE/DepotDownloader/releases/download/DepotDownloader_2.1.1/depotdownloader-2.1.1.zip";

        // URL to download SRCDS from (Move this onto our own domain at some stage)
        static readonly String DOWNLOAD_PATH_SRCDS = "https://forums.alliedmods.net/attachment.php?attachmentid=131318&d=1394307441";

        // URL to download metamod from (Move this onto our own domain at some stage)
        static readonly String DOWNLOAD_PATH_METAMOD = "http://sourcemod.gameconnect.net/files/mmsource-1.10.3-windows.zip";

        // URL to download d2fixups from (Move this onto our own domain at some stage)
        static readonly String DOWNLOAD_PATH_D2FIXUPS = "https://forums.alliedmods.net/attachment.php?attachmentid=131627&d=1395058812";

        // The path to the source1 dota 2 server
        static readonly String SOURCE1_PATH = "dota_s1\\";

        // The path to the source2 dota 2 server
        static readonly String SOURCE2_PATH = "dota_s2\\";

        // The username to download files with (Username and password should probably be exported somewhere)
        static readonly String STEAM_USERNAME = "dotahost_net";

        // The password to download files with
        static readonly String STEAM_PASSWORD = "***REMOVED***";

        // The command to update dota (source1)
        static readonly String STEAMCMD_SOURCE1_DOTA = "+login " + STEAM_USERNAME + " " + STEAM_PASSWORD + " +force_install_dir " + g_BASEPATH + "\\" + SOURCE1_PATH + " +app_update 570 +quit";

        // The command to update dota (source2)
        static readonly String STEAMCMD_SOURCE2_DOTA = "-username " + STEAM_USERNAME + " -password " + STEAM_PASSWORD + " -dir " + g_BASEPATH + "\\" + SOURCE2_PATH + " -app 570 -depot 313250";

        // Used for downloading files
        static WebClient dlManager = new WebClient();
           
        // The main entry point into the program
        static void Main(string[] args)
        {
            // Delete the old log file
            File.Delete(g_BASEPATH + "log.txt");
               
            // Update the dota install
            //updateDotaSource1();
        }

        // This function ensures steamcmd is available
        static void verifySteamcmd()
        {
            // Check if steamcmd exists
            if (!File.Exists(g_BASEPATH + STEAMCMD))
            {
                // Debug log
                log("steamcmd.exe not found, downloading...");

                // Name of the zip to use
                String steamZip = "steamcmd.zip";

                // If there is an old version of steamcmd.zip, delete it
                File.Delete(g_BASEPATH + steamZip);

                // NOTE: WE NEED TO CATCH EXCEPTIONS HERE INCASE STEAM UNREACHABLE!

                // Download steamcmd zip
                dlManager.DownloadFile(DOWNLOAD_PATH_STEAMCMD, steamZip);

                // Extract the archive
                ZipFile.ExtractToDirectory(steamZip, g_BASEPATH + STEAMCMD_PATH);

                // Delete the zip
                File.Delete(g_BASEPATH + steamZip);
            }
        }

        // This function ensures depot downloader is available
        static void verifyDepotDownloader()
        {
            // Check if steamcmd exists
            if (!File.Exists(g_BASEPATH + DEPOT_DOWNLOADER))
            {
                // Debug log
                log("depotdownloader.exe not found, downloading...");

                // Name of the zip to use
                String depotDownloaderZip = "depotdownloader.zip";

                // If there is an old version of steamcmd.zip, delete it
                File.Delete(g_BASEPATH + depotDownloaderZip);

                // NOTE: WE NEED TO CATCH EXCEPTIONS HERE INCASE STEAM UNREACHABLE!

                // Download steamcmd zip
                dlManager.DownloadFile(DOWNLOAD_PATH_DEPOT_DOWNLOADER, depotDownloaderZip);

                // Extract the archive
                ZipFile.ExtractToDirectory(depotDownloaderZip, g_BASEPATH + DEPOT_DOWNLOADER_PATH);

                // Delete the zip
                File.Delete(g_BASEPATH + depotDownloaderZip);
            }
        }

        // This function updates dota 2 (source1)
        // If a source1 server isn't installed, this function will install it from scratch
        static void updateDotaSource1()
        {
            // Debug log
            log("Updating dota 2 (source1)...");

            // Ensure steamcmd exists
            verifySteamcmd();

            // Ensure the directory exists
            Directory.CreateDirectory(SOURCE1_PATH);

            // Build the update commmand
            ProcessStartInfo proc = new ProcessStartInfo();
            proc.WorkingDirectory = g_BASEPATH;
            proc.FileName = STEAMCMD;
            proc.Arguments = STEAMCMD_SOURCE1_DOTA;

            // Attempt to run the update
            try
            {
                // Start the process
                Process process = Process.Start(proc);

                // Wait for it to end
                process.WaitForExit();
            }
            catch
            {
                log("Failed to update!");
                return;
            }

            // Ensure everything is installed correctly
            installSRCDS();
            installMetamod();
            installD2Fixups();
            source1GameInfoPatch();
            patchSource1Maps();

            log("Done!");
        }

        // This function updates dota 2 (source2)
        // If a source2 server isn't installed, this function will install it from scratch
        static void updateDotaSource2()
        {
            // Debug log
            log("Updating dota 2 (source2)...");

            // Ensure steamcmd exists
            verifyDepotDownloader();

            // Ensure the directory exists
            Directory.CreateDirectory(SOURCE2_PATH);

            // Build the update commmand
            ProcessStartInfo proc = new ProcessStartInfo();
            proc.WorkingDirectory = g_BASEPATH;
            proc.FileName = DEPOT_DOWNLOADER;
            proc.Arguments = STEAMCMD_SOURCE2_DOTA;

            // Attempt to run the update
            try
            {
                // Start the process
                Process process = Process.Start(proc);

                // Wait for it to end
                process.WaitForExit();
            }
            catch
            {
                log("Failed to update!");
                return;
            }

            log("Done!");
        }

        // Ensures SRCDS is installed
        static void installSRCDS()
        {
            // Check if metamod exists
            if (!File.Exists(g_BASEPATH + SOURCE1_PATH + "srcds.exe"))
            {
                // Debug log
                log("SRCDS.exe not found, downloading...");

                // Local zip name
                String srcdsZip = "srcds.zip";

                // If there is an old version of steamcmd.zip, delete it
                File.Delete(g_BASEPATH + srcdsZip);

                // NOTE: WE NEED TO CATCH EXCEPTIONS HERE INCASE STEAM UNREACHABLE!

                // Download steamcmd zip
                dlManager.DownloadFile(DOWNLOAD_PATH_SRCDS, srcdsZip);

                // Extract the archive
                ZipFile.ExtractToDirectory(srcdsZip, g_BASEPATH + SOURCE1_PATH);

                // Delete the zip
                File.Delete(g_BASEPATH + srcdsZip);
            }
        }

        // Ensures metamod is installed
        static void installMetamod()
        {
            // Check if metamod exists
            if (!File.Exists(g_BASEPATH + SOURCE1_PATH + "dota\\addons\\metamod.vdf"))
            {
                // Debug log
                log("metamod not found, downloading...");

                // Local zip name
                String metamodZip = "metamod.zip";

                // If there is an old version of steamcmd.zip, delete it
                File.Delete(g_BASEPATH + metamodZip);

                // NOTE: WE NEED TO CATCH EXCEPTIONS HERE INCASE STEAM UNREACHABLE!

                // Download steamcmd zip
                dlManager.DownloadFile(DOWNLOAD_PATH_METAMOD, metamodZip);

                // Extract the archive
                ZipFile.ExtractToDirectory(metamodZip, g_BASEPATH + SOURCE1_PATH + "dota\\");

                // Delete the zip
                File.Delete(g_BASEPATH + metamodZip);
            }
        }

        // Ensures d2fixups is installed
        static void installD2Fixups()
        {
            // Check if metamod exists
            if (!File.Exists(g_BASEPATH + SOURCE1_PATH + "dota\\addons\\metamod\\d2fixups.vdf"))
            {
                // Debug log
                log("d2fixups not found, downloading...");

                // Local zip name
                String d2fixupsZip = "metamod.zip";

                // If there is an old version of steamcmd.zip, delete it
                File.Delete(g_BASEPATH + d2fixupsZip);

                // NOTE: WE NEED TO CATCH EXCEPTIONS HERE INCASE STEAM UNREACHABLE!

                // Download steamcmd zip
                dlManager.DownloadFile(DOWNLOAD_PATH_D2FIXUPS, d2fixupsZip);

                // Extract the archive
                ZipFile.ExtractToDirectory(d2fixupsZip, g_BASEPATH + SOURCE1_PATH + "dota\\");

                // Delete the zip
                File.Delete(g_BASEPATH + d2fixupsZip);
            }
        }

        // Patches gameinfo.txt for source1
        static void source1GameInfoPatch()
        {
            // Gameinfo to load metamod
            String gameinfo = 
                "\"GameInfo\"" + Environment.NewLine + 
                    "{" + Environment.NewLine +
                        "game \"DOTA 2\"" + Environment.NewLine+
                        "gamelogo 1"+ Environment.NewLine +
                        "type multiplayer_only"+ Environment.NewLine +
                        "nomodels 1"+ Environment.NewLine +
                        "nohimodel 1"+ Environment.NewLine +
                        "nocrosshair 0" + Environment.NewLine + "GameData \"dota.fgd\"" + Environment.NewLine +
                        "SupportsDX8 0" + Environment.NewLine +
                        "FileSystem" + Environment.NewLine +
                        "{" + Environment.NewLine +
                            "SteamAppId 816"+ Environment.NewLine +
                            "ToolsAppId 211" + Environment.NewLine +
                            "SearchPaths" + Environment.NewLine +
                            "{" + Environment.NewLine +
                                "GameBin |gameinfo_path|addons/metamod/bin" + Environment.NewLine +
                                "Game |gameinfo_path|." + Environment.NewLine +
                                "Game platform" + Environment.NewLine +
                            "}" + Environment.NewLine +
                        "}" + Environment.NewLine +
                    "}";

            // May need to add addon mounting here eventually

            // Write the metamod loader
            File.WriteAllText(g_BASEPATH + SOURCE1_PATH + "dota\\gameinfo.txt", gameinfo);
        }

        // This function patches source1 maps
        // This function could could issues with data[i++] if the map is made to fail on purpose
        // This function won't fail on standard source maps, since we have control over this,
        // Everything is good in the hood
        static void patchSource1Maps()
        {
            // List of maps to patch
            string[] maps = new string[]
            {
                "dota",
                "dota_681",
                "dota_autumn",
                "dota_diretide_12",
                "dota_winter"
            };

            // Path to the map folder
            String mapPath = g_BASEPATH + SOURCE1_PATH + "dota\\maps\\";

            // Loop over each map
            foreach(String map in maps)
            {
                // Debug log
                log("Attempting to patch " + map);

                // Make a copy of the map
                System.IO.File.Copy(mapPath + map + ".bsp", mapPath + map + ".bsp.tmp", true);

                // Delete the original map
                File.Delete(mapPath + map + ".bsp");

                // Load up the map
                using (FileStream fs = File.Open(mapPath + map + ".bsp.tmp", FileMode.Open))
                {
                    // Read in the map data
                    byte[] data = new BinaryReader(fs).ReadBytes((int)fs.Length);

                    // Search over data
                    for (int i = 0; i < data.Length-10; )
                    {
                        // Searching for `world_maxs` basically
                        if (data[i++] == 'w' &&
                            data[i++] == 'o' &&
                            data[i++] == 'r' &&
                            data[i++] == 'l' &&
                            data[i++] == 'd' &&
                            data[i++] == '_' &&
                            data[i++] == 'm' &&
                            data[i++] == 'a' &&
                            data[i++] == 'x' &&
                            data[i++] == 's')
                        {
                            // Matched, lets find where the numbers begin
                            while (i < data.Length && data[i++] != '"') ;
                            while (i < data.Length && data[i++] != '"') ;

                            // i is now positioned at the numbers
                            data[i++] = (byte)'8';
                            data[i++] = (byte)'3';
                            data[i++] = (byte)'2';
                            data[i++] = (byte)'0';

                            // Store that we patched successfully
                            log(map + " was patched successfully!");
                        }
                    }

                    // Write the new map
                    using (BinaryWriter writer = new BinaryWriter(File.Open(mapPath + map + ".bsp", FileMode.Create)))
                    {
                        // Write the data
                        writer.Write(data);
                    }
                }

                // Delete the copy of the map
                File.Delete(mapPath + map + ".bsp.tmp");
            }
        }

        // Outputs to the console and stores a copy into log.txt
        static void log(string str)
        {
            Console.WriteLine(str);
            File.AppendAllText(g_BASEPATH + "log.txt", str + Environment.NewLine);
        }
    }
}
