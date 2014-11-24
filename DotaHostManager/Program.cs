using Alchemy;
using Alchemy.Classes;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Reflection;
using System.Text;
using System.Timers;
using System.Windows.Forms;

namespace DotaHostManager
{
    class Program
    {
        // Program version
        const short VERSION = 2;
        // Web root
        const string ROOT = "https://dl.dropboxusercontent.com/u/25095474/dotahost/";
        //const string ROOT = "http://127.0.0.1/";
        //const string ROOT = "http://dotahost.net/";

        // Where this executable is run from
        static string BASE_PATH = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\";

        // GitHub download root
        const string GITHUB = "https://codeload.github.com/ash47/";

        // AppData temporary folder
        static string TEMP = System.IO.Path.GetTempPath() + @"dotahost\";

        // Keep-alive timer
        static System.Timers.Timer keepAlive;

        // Keep-alive duration
        const int KEEP_ALIVE_DURATION = 60000; // 60 seconds

        // Increments by 1 everytime an action is started, decrements every time an action is finished
        // The program will not close on timeout unless this is zero
        static byte zeroCanClose = 0;

        // If this is true, the program requests close, but will not close until zeroCanClose is equal to zero
        static bool requestClose = false;

        // This is a queue of files to download. They are stored in the format of: [downloadLocation, targetFile, typeOfDownload, targetFile]
        static List<string[]> toDownload = new List<string[]>();

        // This is our download manager.
        static WebClient dlManager = new WebClient();

        // Path to dota, eg: C:\Program Files (x86)\Steam\steamapps\dota 2 beta\
        static string dotaPath;

        // Delegates for asynchronous socket and download events
        delegate void socketDel(string[] args);
        delegate void downloadProgressDel(string[] args, DownloadProgressChangedEventArgs e);
        delegate void downloadCompleteDel(string[] args, AsyncCompletedEventArgs e);

        // Dictionaries containing the socket and download functions
        static Dictionary<string, socketDel> wSocketHooks = new Dictionary<string, socketDel>();
        static Dictionary<string, downloadProgressDel> downloadProgressHooks = new Dictionary<string, downloadProgressDel>();
        static Dictionary<string, downloadCompleteDel> downloadCompleteHooks = new Dictionary<string, downloadCompleteDel>();

        // Our web socket server
        static WebSocketServer wSocket;

        // Once a connection to the web socket server is established, this acts as a pointer to the context, so we may send messages to the client
        static UserContext gContext;

        // Web socket send queue, uses gContext
        static Dictionary<string, bool> sendQueue = new Dictionary<string, bool>();


        static void Main(string[] i)
        {
            // Reset log file
            File.Delete(BASE_PATH + "log.txt");
            log("[DotaHost] Version " + VERSION);

            // Sets up uri protocol args if launched from browser
            if (i.Length > 0) { log("Requested: " + i[0]); }
            string[] args = new string[0];
            if (i.Length > 0)
            {
                args = i[0].Split('/');
                args = RemoveIndex(args, 0);
                args = RemoveIndex(args, 0);
                args = RemoveIndex(args, args.Length - 1);
            }

            // Hook download events
            hookDownloadEvents();

            // Sets up custom download events, functions are found here
            setupDownloadEvents();

            // Begins the websocket server
            beginWSocket();

            // If first-run or requested autorun, attempt to register the uri protocol
            if (Properties.Settings.Default.autorun)
            {
                registerProtocol();
            }

            // Download the version file from website
            DownloadFile(ROOT + "static/software/dotahostmanager/version", TEMP + "version", "appVersion");

            // Attempts to find the dota path, if it can't find it, it exits the program
            if (!checkDotaPath())
            {
                log("[DotaHost] Dota path could not be found. Exiting...");
                System.Threading.Thread.Sleep(5000);
                Environment.Exit(0);
            }

            // Event loop to prevent program from exiting
            doEvents();
        }

        // Exits the program as soon as it is finished the current task
        static void exit(object sender, ElapsedEventArgs e)
        {
            requestClose = true;
        }

        // Hooks download events
        static void hookDownloadEvents()
        {
            dlManager.DownloadProgressChanged += dlManager_DownloadProgressChanged;
            dlManager.DownloadFileCompleted += dlManager_DownloadFileCompleted;
        }

        // Starts the updater and closes this program
        static void startUpdater()
        {
            log("[Update] Starting...");
            ProcessStartInfo proc2 = new ProcessStartInfo();
            proc2.UseShellExecute = true;
            proc2.WorkingDirectory = TEMP;
            proc2.FileName = "DotaHostUpdater.exe";
            proc2.Verb = "runas";
            proc2.Arguments = "\"" + BASE_PATH;
            try
            {
                Process.Start(proc2);
                Environment.Exit(0);
            }
            catch
            {

            }
        }

        // Sets up the download functions
        static void setupDownloadEvents()
        {
            // Called every time an addon download progresses
            #region downloadProgressHooks("addon");
            downloadProgressHooks.Add("addon", (x, e) =>
            {
                string id = x[1];
                // If a socket connection has previously been opened, send the progress percentage in a formatted string
                if (gContext != null)
                {
                    gContext.Send("addon|" + id + "|percent|" + e.ProgressPercentage.ToString());
                }
            });
            #endregion
        
            // Called every time the app updater download progresses
            #region downloadProgressHooks("appUpdater");
            downloadProgressHooks.Add("appUpdater", (x, e) =>
            {
                string id = x[1];
                // If a socket connection has previously been opened, send the progress percentage in a formatted string
                if (gContext != null)
                {
                    gContext.Send("appUpdater|percent|" + e.ProgressPercentage.ToString());
                }
            });
            #endregion



            // Called when the addonInfo download is complete
            #region downloadCompleteHooks("addonInfo");
            downloadCompleteHooks.Add("addonInfo", (x, e) =>
            {
                // Sets up properties from arguments and addonInfo file
                string id = x[1];
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
                            log("[Addon] " + id + " is up to date.");
                            return;
                        }
                    }
                }

                // Directory or file does not exist, or version does not match most recent
                log("[Addon] " + id + " out of date. New version: " + version);
                log("[Downloading] " + "https://codeload.github.com/ash47/" + name + "/zip/" + version + ".zip");

                // Begins downloading addon from GitHub
                DownloadFile("https://codeload.github.com/ash47/" + name + "/zip/" + version, TEMP + id + ".zip", "addon|" + id + "|" + name + "|" + version);
            });
            #endregion
          
            // Called when the addon download is complete
            #region downloadCompleteHooks("addon")
            downloadCompleteHooks.Add("addon", (x, e) =>
            {
                // Sets up properties from arguments and addonInfo file
                string[] args = x;
                string id = args[1];
                string name = args[2];
                string version = args[3];

                // Deletes current addon folder if it exists
                if (Directory.Exists(dotaPath + @"dota\addons\" + id + @"\")) { Directory.Delete(dotaPath + @"dota\addons\" + id + @"\", true); }

                // Downloads CRC from website, and stores it
                log("[CRC] Checking CRC...");
                dlManager.DownloadFile(ROOT + "static/addons/" + id + "/CRC", TEMP + id + "CRC");
                string correctCRC = File.ReadAllText(TEMP + id + "CRC");

                // Deletes CRC file
                File.Delete(TEMP + id + "CRC");

                // Generates new CRC of the zip just downloaded
                string actualCRC = calculateCRC(TEMP + id + ".zip");
                log("CRC: " + correctCRC + " == " + actualCRC);

                // Matches the generated CRC with the downloaded CRC
                if (correctCRC != actualCRC)
                {
                    // Installation has failed, send formatted string to most recent connection
                    log("[CRC] Mismatch!");
                    log(" == Installation failed! == ");
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
                    log("[Extract] Extracting...");
                    ZipFile.ExtractToDirectory(TEMP + id + ".zip", dotaPath + @"dota\addons\");

                    // Rename folder from default to addon ID
                    Directory.Move(dotaPath + @"dota\addons\" + name + "-" + version + @"\", dotaPath + @"dota\addons\" + id + @"\");
                    log("[Extract] Done!");
                    log(" == Installation successful! == ");

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
                log("[Cleaning] Cleaning up...");
                File.Delete(TEMP + id + ".zip");
                log("[Cleaning] Done!");
                log("[Socket] Sending confirmation update!");
            });
            #endregion

            // Called when the appVersion download is complete
            #region  downloadCompleteHooks("appVersion")
            downloadCompleteHooks.Add("appVersion", (x, e) =>
            {
                log("[Update] Checking for updates...");
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
                        log("[Update] New version detected!");

                        // If the downloader does not exist, download it
                        if (!File.Exists(TEMP + "DotaHostUpdater.exe"))
                        {
                            log("[Update] Downloading updater...");
                            DownloadFile(ROOT + "downloads/software/dotahost/DotaHostUpdater.exe", TEMP + "DotaHostUpdater.exe", "appUpdater");
                        }
                        else
                        {
                            // Begin the updater
                            startUpdater();
                        }
                    }
                    else
                    {
                        log("[Update] DotaHost up-to-date!");
                    }
                }
                catch
                {
                    log("[Update] Updating failed.");
                }
            });
            #endregion

            // Called when the appUpdater download is complete
            #region downloadCompleteHooks("appUpdater");
            downloadCompleteHooks.Add("appUpdater", (x, e) =>
            {
                // Begin the updater
                startUpdater();
            });
            #endregion

        }
        
        // Begins download of addonInfo for given addon
        static void updateAddon(string addonID)
        {
            log("[Downloading] " + ROOT + "static/addons/" + addonID + "/" + "info");
            DownloadFile(ROOT + "static/addons/" + addonID + "/info", TEMP + addonID, "addonInfo|" + addonID);
        }
        
        // Generates a json structure of installed addon information, sends it to client
        static void checkAddons()
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
        static bool checkDotaPath()
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
                log("Could not find dota path.");
                return false;
            }
            else
            {
                log("Found dota path: " + dotaPath);
                return true;
            }
        }
        
        // Updates the dota path
        static void updateDotaPath(string newPath)
        {
            // Check if newPath is a valid directory
            if (!Directory.Exists(newPath))
            {
                log("Directory does not exist: " +  newPath);
            }
            else
            {
                try
                {
                    // Sets dotaPath and settings to the new path
                    dotaPath = newPath;
                    Properties.Settings.Default.dotaPath = dotaPath;
                    Properties.Settings.Default.Save();
                    log("Updated dota path: " + dotaPath);
                }
                catch
                {
                    // Whoops, something went wrong
                    log("Failed to update path: Uncaught exception");
                }
            }
        }

        // Begins the web socket server
        static void beginWSocket()
        {
            // Hook the socket events and set up web socket server, then start it
            log("[Socket] Connecting...");
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
        static void hookWSocketEvents()
        {
            // "setDotaPath|C:\blah\blah\steam\steamapps\common\dota 2 beta\"
            wSocketHooks.Add("setDotaPath", (x) => { updateDotaPath(x[1]); });
            wSocketHooks.Add("exit", (x) => { Environment.Exit(0); });
            wSocketHooks.Add("connected", (x) => {  });
            wSocketHooks.Add("autorun", (x) => { registerProtocol(); });
            wSocketHooks.Add("update", (x) => { updateAddon(x[1]); });
        }
        static void wSocket_OnReceive(UserContext context)
        {
            appKeepAlive();
            string[] args = context.DataFrame.ToString().Split('|');
            if (wSocketHooks.Keys.Contains(args[0]))
            {
                wSocketHooks[args[0]](args);
            }
        }
        static void wSocket_OnSend(UserContext context)
        {

        }
        static void wSocket_OnConnect(UserContext context)
        {
            log("[Socket] Connecting...");
        }
        static void wSocket_OnConnected(UserContext context)
        {
            log("[Socket] Connected!");
            foreach (var i in sendQueue)
            {
                context.Send(i.Key, false, i.Value);
            }
            sendQueue.Clear();
            gContext = context;
            gContext.Send("dotaPath|" + dotaPath);
        }
        static void wSocket_OnDisconnect(UserContext context)
        {
            log("[Socket] Disconnected!");
        }

        static void appKeepAlive()
        {
            if (keepAlive != null)
            {
                keepAlive.Dispose();
            }
            keepAlive = new System.Timers.Timer(KEEP_ALIVE_DURATION);
            keepAlive.Elapsed += exit;
            keepAlive.Start();
        }

        static void doEvents()
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

       

        static void DownloadFile(string sourceFile, string targetName, string downloadID)
        {
            zeroCanClose++;
            string downloadPath = Path.GetDirectoryName(targetName) + @"\";

            if (dlManager.IsBusy)
            {
                toDownload.Add(new string[] { sourceFile, targetName, downloadID });
            }
            else
            {
                try
                {
                    if (Directory.Exists(downloadPath))
                    {
                        log("[Download] Begin download of " + sourceFile + " -> " + targetName);
                        dlManager.DownloadFileAsync(new Uri(sourceFile), targetName, downloadID);
                        return;
                    }
                    else
                    {
                        try
                        {
                            log("[File System] Creating directory: " + downloadPath);
                            Directory.CreateDirectory(downloadPath);
                        }
                        catch (UnauthorizedAccessException e)
                        {
                            throw e;
                        }
                        catch
                        {
                            log("[File System] Failed to create directory.");
                            zeroCanClose--;
                            return;
                        }
                        log("[Download] Begin download of " + sourceFile + " -> " + targetName);
                        zeroCanClose++;
                        dlManager.DownloadFileAsync(new Uri(sourceFile), targetName, downloadID);
                    }
                }
                catch (Exception)
                {
                    log("Uncaught exception.");
                    zeroCanClose--;
                }
                zeroCanClose--;
            }
        }
        static void dlManager_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            string[] args = e.UserState.ToString().Split('|');
            if (downloadProgressHooks.Keys.Contains(args[0]))
            {
                downloadProgressHooks[args[0]](args, e);
            }
        }
        static void dlManager_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            string[] args = e.UserState.ToString().Split('|');
            log("[Downloading] " + e.UserState.ToString() + " Done!");
            if (downloadCompleteHooks.Keys.Contains(args[0]))
            {
                downloadCompleteHooks[args[0]](args, e);
            }
            if (toDownload.Count > 0)
            {
                DownloadFile(toDownload[0][0], toDownload[0][1], toDownload[0][2]);
                toDownload.RemoveAt(0);
            }
            zeroCanClose--;
        }

        static string calculateCRC(string fileName)
        {
            zeroCanClose++;
            Crc32 crc32 = new Crc32();
            String hash = String.Empty;
            using (FileStream fs = File.Open(fileName, FileMode.Open))
                foreach (byte b in crc32.ComputeHash(fs))
                {
                    hash += b.ToString("x2").ToLower();
                }
            zeroCanClose--;
            return hash;
        }

        static void registerProtocol()
        {
            zeroCanClose++;
            string applicationPath = Assembly.GetEntryAssembly().Location;
            RegistryKey key = Registry.ClassesRoot.OpenSubKey("DotaHost");
            if (key == null)  //if the protocol is not registered yet...we register it
            {
                try
                {
                    key = Registry.ClassesRoot.CreateSubKey("DotaHost");
                    key.SetValue(string.Empty, "URL:DotaHost Protocol");
                    key.SetValue("URL Protocol", string.Empty);

                    key = key.CreateSubKey(@"shell\open\command");
                    key.SetValue(string.Empty, "\"" + applicationPath + "\" " + "%1");

                    log("Registry added.");
                }
                catch
                {
                    if (MessageBox.Show("Would you like DotaHostManager to launch itself automatically when you visit the DotaHost.net website? (Requires Administrator Privillages)", "Autorun", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    {
                        log("Failed to add registry. Launching as admin...");
                        const int ERROR_CANCELLED = 1223; //The operation was canceled by the user.

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
                                Console.Write("Admin request denied. Exiting.");
                                for (int i = 0; i < 2; ++i)
                                {
                                    System.Threading.Thread.Sleep(1000);
                                    Console.Write(".");
                                }
                                System.Threading.Thread.Sleep(1000);
                                Environment.Exit(0);
                            }
                        }
                    }
                    else
                    {
                        Properties.Settings.Default.autorun = false;
                        Properties.Settings.Default.Save();
                    }
                    zeroCanClose--;
                    return;
                }
            }
            else if (key.OpenSubKey(@"shell\open\command").GetValue(string.Empty).ToString().ToLower() != ("\"" + applicationPath + "\" " + "%1").ToLower())
            {
                key = key.OpenSubKey(@"shell\open\command", true);
                key.SetValue(string.Empty, "\"" + applicationPath + "\" " + "%1");

                log("Registry updated.");
            }
            else
            {
                log("No registry action taken.");
            }
            key.Close();
            zeroCanClose--;
        }


        static string[] RemoveIndex(string[] IndicesArray, int RemoveAt)
        {
            string[] newIndicesArray = new string[IndicesArray.Length - 1];
            int i = 0;
            int j = 0;
            while (i < IndicesArray.Length)
            {
                if (i != RemoveAt)
                {
                    newIndicesArray[j] = IndicesArray[i];
                    j++;
                }
                i++;
            }
            return newIndicesArray;
        }

        static void log(string str)
        {
            Console.WriteLine(str);
            File.AppendAllText(BASE_PATH + "log.txt", str + "\n");
        }

    }

}
