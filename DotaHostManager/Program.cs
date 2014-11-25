﻿using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Timers;
using System.Windows.Forms;
using DotaHostLibrary;

namespace DotaHostManager
{
    class Program
    {
        // Program version
        private const short VERSION = 1;
        // Web root
        private const string ROOT = "https://dl.dropboxusercontent.com/u/25095474/dotahost/";
        //const string ROOT = "http://127.0.0.1/";
        //const string ROOT = "http://dotahost.net/";

        // Where this executable is run from
        private static readonly string BASE_PATH = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\";

        // GitHub download root
        private const string GITHUB = "https://codeload.github.com/ash47/";

        // AppData temporary folder
        private static string TEMP = System.IO.Path.GetTempPath() + @"dotahost\";

        // Keep-alive timer
        private static System.Timers.Timer keepAlive;

        // Keep-alive duration
        private const int KEEP_ALIVE_DURATION = 60000; // 60 seconds

        // Increments by 1 everytime an action is started, decrements every time an action is finished. The program will not close on timeout unless this is zero
        private static byte zeroCanClose = 0;

        // If this is true, the program requests close, but will not close until zeroCanClose is equal to zero
        private static bool requestClose = false;

        // This is our download manager.
        private static DownloadManager dlManager = new DownloadManager();

        // Path to dota, eg: C:\Program Files (x86)\Steam\steamapps\dota 2 beta\
        private static string dotaPath;

        // Dictionaries containing the socket and download functions
        private static Dictionary<string, socketDel> wSocketHooks = new Dictionary<string, socketDel>();

        // Our web socket server
        private static WebSocketServer wSocket;

        // Once a connection to the web socket server is established, this acts as a pointer to the context, so we may send messages to the client
        private static UserContext gContext;

        // Web socket send queue, uses gContext
        private static Dictionary<string, bool> sendQueue = new Dictionary<string, bool>();


        private static void Main(string[] i)
        {
            // Reset log file
            File.Delete(BASE_PATH + "log.txt");
            Helpers.log("[DotaHost] Version " + VERSION);


            // Sets up uri protocol args if launched from browser
            if (i.Length > 0) { Helpers.log("Requested: " + i[0]); }
            string[] args = new string[0];
            if (i.Length > 0)
            {
                args = i[0].Split('/');
                args = Helpers.RemoveIndex(args, 0);
                args = Helpers.RemoveIndex(args, 0);
                args = Helpers.RemoveIndex(args, args.Length - 1);
            }

            // Begins the websocket server
            beginWSocket();

            // If first-run or requested autorun, attempt to register the uri protocol
            if (Properties.Settings.Default.autorun)
            {
                registerProtocol();
            }

            // Download the version file from website
            downloadAppVersion();

            // Attempts to find the dota path, if it can't find it, it exits the program
            if (!checkDotaPath())
            {
                Helpers.log("[DotaHost] Dota path could not be found. Exiting...");
                Environment.Exit(0);
            }

            // Event loop to prevent program from exiting
            doEvents();
        }

        // Download the most up-to-date version file of the app
        private static void downloadAppVersion()
        {
            dlManager.download(ROOT + "static/software/dotahostmanager/version", TEMP + "version", (e) => { }, (e) =>
            {
                Helpers.log("[Update] Checking for updates...");
                short version;
                try
                {
                    // Reads the version file from temp
                    version = Convert.ToInt16(File.ReadAllText(TEMP + "version"));
                    File.Delete(TEMP + "version");

                    // Checks if the read version matches the const version
                    if (version != VERSION)
                    {
                        // They do not match, download new version
                        Helpers.log("[Update] New version detected!");

                        // If the downloader does not exist, download it
                        if (!File.Exists(TEMP + "DotaHostManagerUpdater.exe"))
                        {
                            Helpers.log("[Update] Downloading updater...");

                            dlManager.download(ROOT + "static/software/dotahostmanager/DotaHostManagerUpdater.exe", TEMP + "DotaHostManagerUpdater.exe", (e2) => {
                                appUpdaterDownloadProgress(e2.ProgressPercentage);
                            }, (e2) => {
                                // Begin the updater
                                startUpdater();
                            });
                        }
                        else
                        {
                            // Begin the updater
                            startUpdater();
                        }
                    }
                    else
                    {
                        Helpers.log("[Update] DotaHost up-to-date!");
                    }
                }
                catch
                {
                    Helpers.log("[Update] Updating failed.");
                }
            });
        }

        // Called every time the app updater download progresses
        private static void appUpdaterDownloadProgress(int percentage)
        {
            // If a socket connection has previously been opened, send the progress percentage in a formatted string
            if (gContext != null)
            {
                gContext.Send("appUpdater|percent|" + percentage.ToString());
            }
        }

        // Exits the program as soon as it is finished the current task
        private static void exit(object sender, ElapsedEventArgs e)
        {
            requestClose = true;
        }

        // Starts the updater and closes this program
        private static void startUpdater()
        {
            Helpers.log("[Update] Starting...");
            ProcessStartInfo proc = new ProcessStartInfo();
            proc.UseShellExecute = true;
            proc.WorkingDirectory = TEMP;
            proc.FileName = "DotaHostManagerUpdater.exe";
            //proc2.Verb = "runas";
            proc.Arguments = "\"" + BASE_PATH;
            try
            {
                Process.Start(proc);
                Environment.Exit(0);
            }
            catch
            {

            }
        }

        // Function run when an addon finishes getting downloaded
        private static void downloadAddonComplete(string[] args)
        {
            // Sets up properties from arguments and addonInfo file
            string id = args[0];
            string name = args[1];
            string version = args[2];

            // Deletes current addon folder if it exists
            if (Directory.Exists(dotaPath + @"dota\addons\" + id + @"\")) { Directory.Delete(dotaPath + @"dota\addons\" + id + @"\", true); }

            // Downloads CRC from website, and stores it
            Helpers.log("[CRC] Checking CRC...");
            dlManager.downloadSync(ROOT + "static/addons/" + id + "/CRC", TEMP + id + "CRC");
            string correctCRC = File.ReadAllText(TEMP + id + "CRC");

            // Deletes CRC file
            File.Delete(TEMP + id + "CRC");

            // Generates new CRC of the zip just downloaded
            string actualCRC = Helpers.calculateCRC(TEMP + id + ".zip");
            Helpers.log("CRC: " + correctCRC + " == " + actualCRC);

            // Matches the generated CRC with the downloaded CRC
            if (correctCRC != actualCRC)
            {
                // Installation has failed, send formatted string to most recent connection
                Helpers.log("[CRC] Mismatch!");
                Helpers.log(" == Installation failed! == ");
                if (gContext != null)
                {
                    gContext.Send("installationFailed|addon|" + id, false, true);
                }
                else
                {
                    sendQueue.Add("installationFailed|addon|" + id, true);
                }
            }
            else
            {
                // CRC check has passed, extract file to addons folder
                Helpers.log("[Extract] Extracting...");
                ZipFile.ExtractToDirectory(TEMP + id + ".zip", dotaPath + @"dota\addons\");

                // Rename folder from default to addon ID
                Directory.Move(dotaPath + @"dota\addons\" + name + "-" + version + @"\", dotaPath + @"dota\addons\" + id + @"\");
                Helpers.log("[Extract] Done!");
                Helpers.log(" == Installation successful! == ");

                // Installation was successful, send formatted string to most recent connection
                if (gContext != null)
                {
                    gContext.Send("installationComplete|addon|" + id, false, true);
                }
                else
                {
                    sendQueue.Add("installationComplete|addon|" + id, true);
                }
            }

            // Deletes the downloaded zip file
            Helpers.log("[Cleaning] Cleaning up...");
            File.Delete(TEMP + id + ".zip");
            Helpers.log("[Cleaning] Done!");
            Helpers.log("[Socket] Sending confirmation update!");
        }

        // Function run when the addon info download is complete
        private static void downloadAddonInfoComplete(string addonID)
        {
            // Sets up properties from arguments and addonInfo file
            string id = addonID;
            string[] info = File.ReadAllLines(TEMP + id);
            File.Delete(TEMP + id);
            string name = info[0];
            string version = info[1];

            // Checks if the addon directory exists
            if (Directory.Exists(dotaPath + @"dota\addons\" + id))
            {
                // Checks if the version file exists in the addon directory
                if (File.Exists(dotaPath + @"dota\addons\" + id + @"\version"))
                {
                    // Sets the current version to the value in the version file
                    string currentVersion = File.ReadAllText(dotaPath + @"dota\addons\" + id + @"\version");
                    // Checks current version against the version found in the addonInfo file
                    if (version == currentVersion)
                    {
                        Helpers.log("[Addon] " + id + " is up to date.");
                        return;
                    }
                }
            }

            // Directory or file does not exist, or version does not match most recent
            Helpers.log("[Addon] " + id + " out of date. New version: " + version);
            Helpers.log("[Downloading] " + "https://codeload.github.com/ash47/" + name + "/zip/" + version + ".zip");

            // Begins downloading addon from GitHub
            dlManager.download("https://codeload.github.com/ash47/" + name + "/zip/" + version, TEMP + id + ".zip", (e) => 
            {
                if (gContext != null)
                {
                    gContext.Send("addon|" + id + "|percent|" + e.ProgressPercentage.ToString());
                }
            }, (e) => 
            {
                string[] args = { id, name, version };
                downloadAddonComplete(args);
            });
        }

        // Begins download of addonInfo for given addon
        private static void updateAddon(string addonID)
        {
            Helpers.log("[Downloading] " + ROOT + "static/addons/" + addonID + "/" + "info");
            dlManager.download(ROOT + "static/addons/" + addonID + "/info", TEMP + addonID, (e) =>
            {
                // If a socket connection has previously been opened, send the progress percentage in a formatted string
                if (gContext != null)
                {
                    gContext.Send("addon|" + addonID + "|percent|" + e.ProgressPercentage.ToString());
                }
            }, (e) =>
            {
                downloadAddonInfoComplete(addonID);
            });
        }
        
        // Generates a json structure of installed addon information, sends it to client
        private static void checkAddons()
        {
            // Define json structure
            string json = "{";

            // Generates json structure
            foreach (var dir in Directory.GetDirectories(dotaPath + @"dota\addons\"))
            {
                string[] path = dir.Split('\\');
                byte length = (byte)path.Length;
                string addonID = path[length - 1];
                if (File.Exists(dir + @"\version"))
                {
                    byte version = (byte)Convert.ToInt16(File.ReadAllText(dir + @"\version"));
                    json += "\"" + addonID + "\":\"" + version + "\",";
                }
            }

            // If the structure contains anything, remove the last comma
            if (json.Length > 1)
            {
                json = json.Substring(0, json.Length - 1);
            }

            // Close and send
            json += "}";
            sendQueue.Add(json, true);
        }

        // Attempts to find the dota path, returns false if not found
        private static bool checkDotaPath()
        {
            bool failed = false;

            // Checks if the path is already set
            if (Properties.Settings.Default.dotaPath == String.Empty)
            {
                // Gets dota path from registry
                string path = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 570", "InstallLocation", null).ToString();

                // If path is found, update it in the program
                if (path != String.Empty)
                {
                    updateDotaPath(path + @"\");
                }
            }
            else
            {
                // Path has already been found, set it to the stored setting
                dotaPath = Properties.Settings.Default.dotaPath;
            }
            if (failed)
            {
                Helpers.log("Could not find dota path.");
                return false;
            }
            else
            {
                Helpers.log("Found dota path: " + dotaPath);
                return true;
            }
        }
        
        // Updates the dota path
        private static void updateDotaPath(string newPath)
        {
            // Check if newPath is a valid directory
            if (!Directory.Exists(newPath))
            {
                Helpers.log("Directory does not exist: " +  newPath);
            }
            else
            {
                try
                {
                    // Sets dotaPath and settings to the new path
                    dotaPath = newPath;
                    Properties.Settings.Default.dotaPath = dotaPath;
                    Properties.Settings.Default.Save();
                    Helpers.log("Updated dota path: " + dotaPath);
                }
                catch
                {
                    // Whoops, something went wrong
                    Helpers.log("Failed to update path: Uncaught exception");
                }
            }
        }

        // Begins the web socket server
        private static void beginWSocket()
        {
            // Hook the socket events and set up web socket server, then start it
            Helpers.log("[Socket] Connecting...");
            hookWSocketEvents();
            wSocket = new WebSocketServer(2074, new IPAddress(new byte[] { 127, 0, 0, 1 }))
            {
                OnReceive = wSocket_OnReceive,
                OnSend = wSocket_OnSend,
                OnConnect = wSocket_OnConnect,
                OnConnected = wSocket_OnConnected,
                OnDisconnect = wSocket_OnDisconnect,
                TimeOut = new TimeSpan(0, 5, 0),
            };
            wSocket.Start();
        }
       
        // Create and bind the functions for web socket events
        private static void hookWSocketEvents()
        {
            // "setDotaPath|C:\blah\blah\steam\steamapps\common\dota 2 beta\"
            wSocketHooks.Add("setDotaPath", (x) => { updateDotaPath(x[1]); });
            wSocketHooks.Add("exit", (x) => { Environment.Exit(0); });
            wSocketHooks.Add("connected", (x) => {  });
            wSocketHooks.Add("autorun", (x) => { registerProtocol(); });
            wSocketHooks.Add("update", (x) => { updateAddon(x[1]); });
        }
      
        // Web socket server onReceive function
        private static void wSocket_OnReceive(UserContext context)
        {
            // Refresh keep-alive timer
            appKeepAlive();
            
            // Find and execute function for given input
            string[] args = context.DataFrame.ToString().Split('|');
            if (wSocketHooks.Keys.Contains(args[0]))
            {
                wSocketHooks[args[0]](args);
            }
        }
       
        // Web socket server onSend function
        private static void wSocket_OnSend(UserContext context)
        {

        }

        // Web socket server onConnect function
        private static void wSocket_OnConnect(UserContext context)
        {
            Helpers.log("[Socket] Connecting...");
        }

        // Web socket server onConnected function
        private static void wSocket_OnConnected(UserContext context)
        {
            // Once connected, send all messages in send queue
            Helpers.log("[Socket] Connected!");
            foreach (var i in sendQueue)
            {
                context.Send(i.Key, false, i.Value);
            }
            sendQueue.Clear();

            // Set gContext to this connection
            gContext = context;
            gContext.Send("dotaPath|" + dotaPath);
        }

        // Web socket server onDisconnect function
        private static void wSocket_OnDisconnect(UserContext context)
        {
            Helpers.log("[Socket] Disconnected!");
        }

        // Removes the old timer, and ccreates and binds another one
        private static void appKeepAlive()
        {
            if (keepAlive != null)
            {
                keepAlive.Dispose();
            }
            keepAlive = new System.Timers.Timer(KEEP_ALIVE_DURATION);
            keepAlive.Elapsed += exit;
            keepAlive.Start();
        }

        // Application.DoEvents() + check closeRequest and zeroCanClose
        private static void doEvents()
        {
            while (true)
            {
                System.Windows.Forms.Application.DoEvents();
                if (requestClose && zeroCanClose == 0)
                {
                    Environment.Exit(0);
                }
            }
        }

        // Function to request registration of the dotahost uri protocol
        private static void registerProtocol()
        {
            // Begin task
            zeroCanClose++;

            // Stores the full executable path of this application, file name included
            string applicationPath = Assembly.GetEntryAssembly().Location;

            // Opens the key "DotaHost" and stores it in key
            RegistryKey key = Registry.ClassesRoot.OpenSubKey("DotaHost");

            // If the protocol is not registered yet, we register it
            if (key == null)
            {
                try
                {
                    // Creates the subkey in the registry
                    key = Registry.ClassesRoot.CreateSubKey("DotaHost");

                    // Register the URI protocol in registry
                    key.SetValue(string.Empty, "URL:DotaHost Protocol");
                    key.SetValue("URL Protocol", string.Empty);

                    // Set URI to launch the application with one given argument
                    key = key.CreateSubKey(@"shell\open\command");
                    key.SetValue(string.Empty, "\"" + applicationPath + "\" " + "%1");

                    Helpers.log("Registry added.");
                }
                catch
                {
                    if (MessageBox.Show("Would you like DotaHostManager to launch itself automatically when you visit the DotaHost.net website? (Requires Administrator Privillages)", "Autorun", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    {
                        Helpers.log("Failed to add registry. Launching as admin...");
                        const int ERROR_CANCELLED = 1223; //The operation was canceled by the user.

                        // Start a new instance of this executable as administrator
                        ProcessStartInfo info = new ProcessStartInfo("DotaHostManager.exe");
                        info.UseShellExecute = true;
                        info.Verb = "runas";
                        try
                        {
                            Process.Start(info);
                        }
                        catch (Win32Exception ex)
                        {
                            if (ex.NativeErrorCode == ERROR_CANCELLED)
                            {
                                Helpers.log("Admin request denied.");
                            }
                        }
                    }
                    else
                    {
                        // Save user preference
                        Properties.Settings.Default.autorun = false;
                        Properties.Settings.Default.Save();
                    }
                }
            }
            // Check if the current executing path does not match the one in the registry
            else if (key.OpenSubKey(@"shell\open\command").GetValue(string.Empty).ToString().ToLower() != ("\"" + applicationPath + "\" " + "%1").ToLower())
            {
                // Open the subkey
                key = key.OpenSubKey(@"shell\open\command", true);

                // Change the path to the current executing path
                key.SetValue(string.Empty, "\"" + applicationPath + "\" " + "%1");

                Helpers.log("Registry updated.");
            }
            else
            {
                Helpers.log("No registry action taken.");
            }

            // We're done with the registry, close the key
            if (key != null)
            {
                key.Close();
            }

            // End task
            zeroCanClose--;
        }

    }

}
