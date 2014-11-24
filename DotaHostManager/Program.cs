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
        const short g_VERSION = 2;
        const string g_ROOT = "https://dl.dropboxusercontent.com/u/25095474/dotahost/";
        //const string g_ROOT = "http://127.0.0.1/";
        //const string g_ROOT = "http://dotahost.net/";
        static string g_BASEPATH = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\";
        static string g_TEMP = System.IO.Path.GetTempPath() + @"dotahost\";
        static System.Timers.Timer keepAlive;
        static byte canClose = 0;
        static bool plzClose = false;

        // [downloadLocation, targetFile, typeOfDownload, targetFile]
        static List<string[]> toDownload = new List<string[]>();
        static WebClient dlManager = new WebClient();
        static string dotaPath;
        delegate void socketDel(string[] args);
        delegate void downloadProgressDel(string[] args, DownloadProgressChangedEventArgs e);
        delegate void downloadCompleteDel(string[] args, AsyncCompletedEventArgs e);
        static Dictionary<string, socketDel> wSocketHooks = new Dictionary<string, socketDel>();
        static Dictionary<string, downloadProgressDel> downloadProgressHooks = new Dictionary<string, downloadProgressDel>();
        static Dictionary<string, downloadCompleteDel> downloadCompleteHooks = new Dictionary<string, downloadCompleteDel>();
        static WebSocketServer wSocket;
        static UserContext gContext;
        static Dictionary<string, bool> sendQueue = new Dictionary<string, bool>();

        static void exit(object sender, ElapsedEventArgs e)
        {
            plzClose = true;
        }

        static void Main(string[] i)
        {
            File.Delete(g_BASEPATH + "log.txt");
            log("[DotaHost] Version " + g_VERSION);
            if (i.Length > 0) { log("Requested: " + i[0]); }
            string[] args = new string[0];
            if (i.Length > 0)
            {
                args = i[0].Split('/');
                args = RemoveIndex(args, 0);
                args = RemoveIndex(args, 0);
                args = RemoveIndex(args, args.Length - 1);
            }
            hookDownloadEvents();
            setupDownloadEvents();
            beginWSocket();
            if (Properties.Settings.Default.autorun)
            {
                registerProtocol();
            }
            DownloadFile(g_ROOT + "static/software/dotahostmanager/version", g_TEMP + "version", "appVersion");

            if (!checkDotaPath())
            {
                log("[DotaHost] Dota path could not be found. Exiting...");
                System.Threading.Thread.Sleep(5000);
                Environment.Exit(0);
            }
            doEvents();
        }

        static void setupDownloadEvents()
        {

            #region downloadProgressHooks("addon");
            downloadProgressHooks.Add("addon", (x, e) =>
            {
                string id = x[1];
                if (gContext != null)
                {
                    gContext.Send("addon|" + id + "|percent|" + e.ProgressPercentage.ToString());
                }
            });
            #endregion
            #region downloadProgressHooks("appUpdater");
            downloadProgressHooks.Add("appUpdater", (x, e) =>
            {
                string id = x[1];
                if (gContext != null)
                {
                    gContext.Send("appUpdater|percent|" + e.ProgressPercentage.ToString());
                }
            });
            #endregion

            #region downloadCompleteHooks("addonInfo");
            downloadCompleteHooks.Add("addonInfo", (x, e) =>
            {
                string id = x[1];
                string[] info = File.ReadAllLines(g_TEMP + id);
                File.Delete(g_TEMP + id);
                string name = info[0];
                string version = info[1];
                if (Directory.Exists(dotaPath + @"dota\addons\" + id))
                {
                    if (File.Exists(dotaPath + @"dota\addons\" + id + @"\version"))
                    {
                        string currentVersion = File.ReadAllText(dotaPath + @"dota\addons\" + id + @"\version");
                        if (version == currentVersion)
                        {
                            log("[Addon] " + id + " is up to date.");
                            return;
                        }
                    }
                }
                log("[Addon] " + id + " out of date. New version: " + version);
                log("[Downloading] " + "https://codeload.github.com/ash47/" + name + "/zip/" + version + ".zip");
                DownloadFile("https://codeload.github.com/ash47/" + name + "/zip/" + version, g_TEMP + id + ".zip", "addon|" + id + "|" + name + "|" + version);
            });
            #endregion
            #region downloadCompleteHooks("addon")
            downloadCompleteHooks.Add("addon", (x, e) =>
            {
                string[] args = x;
                string id = args[1];
                string name = args[2];
                string version = args[3];
                if (Directory.Exists(dotaPath + @"dota\addons\" + id + @"\")) { Directory.Delete(dotaPath + @"dota\addons\" + id + @"\", true); }
                log("[CRC] Checking CRC...");
                dlManager.DownloadFile(g_ROOT + "static/addons/" + id + "/CRC", g_TEMP + id + "CRC");
                string correctCRC = File.ReadAllText(g_TEMP + id + "CRC");
                File.Delete(g_TEMP + id + "CRC");
                string actualCRC = calculateCRC(g_TEMP + id + ".zip");
                log("CRC: " + correctCRC + " == " + actualCRC);
                if (correctCRC != actualCRC)
                {
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
                    log("[Extract] Extracting...");
                    ZipFile.ExtractToDirectory(g_TEMP + id + ".zip", dotaPath + @"dota\addons\");
                    Directory.Move(dotaPath + @"dota\addons\" + name + "-" + version + @"\", dotaPath + @"dota\addons\" + id + @"\");
                    log("[Extract] Done!");
                    log(" == Installation successful! == ");
                    if (gContext != null)
                    {
                        gContext.Send("installationComplete|addon|" + id, false, true);
                    }
                    else
                    {
                        sendQueue.Add("installationComplete|addon|" + id, true);
                    }
                }
                log("[Cleaning] Cleaning up...");
                File.Delete(g_TEMP + id + ".zip");
                log("[Cleaning] Done!");
                log("[Socket] Sending confirmation update!");
            });
            #endregion
            #region  downloadCompleteHooks("appVersion")
            downloadCompleteHooks.Add("appVersion", (x, e) =>
            {
                log("[Update] Checking for updates...");
                short version;
                try
                {
                    version = Convert.ToInt16(File.ReadAllText(g_TEMP + "version"));
                    File.Delete(g_TEMP + "version");
                    if (version != g_VERSION)
                    {
                        log("[Update] New version detected!");
                        if (!File.Exists(g_TEMP + "DotaHostUpdater.exe"))
                        {
                            log("[Update] Downloading updater...");
                            DownloadFile(g_ROOT + "downloads/software/dotahost/DotaHostUpdater.exe", g_TEMP + "DotaHostUpdater.exe", "appUpdater");
                        }
                        else
                        {
                            log("[Update] Updating DotaHost...");
                            ProcessStartInfo proc = new ProcessStartInfo();
                            proc.UseShellExecute = true;
                            proc.WorkingDirectory = g_TEMP;
                            proc.FileName = "DotaHostUpdater.exe";
                            proc.Verb = "runas";
                            proc.Arguments = "\"" + g_BASEPATH;
                            try
                            {
                                Process.Start(proc);
                                Environment.Exit(0);
                            }
                            catch
                            {

                            }
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
            #region downloadCompleteHooks("appUpdater");
            downloadCompleteHooks.Add("appUpdater", (x, e) =>
            {
                ProcessStartInfo proc2 = new ProcessStartInfo();
                proc2.UseShellExecute = true;
                proc2.WorkingDirectory = g_TEMP;
                proc2.FileName = "DotaHostUpdater.exe";
                proc2.Verb = "runas";
                proc2.Arguments = "\"" + g_BASEPATH;
                try
                {
                    Process.Start(proc2);
                    Environment.Exit(0);
                }
                catch
                {

                }
            });
            #endregion

        }
        
        static void updateAddon(string addonID)
        {
            log("[Downloading] " + g_ROOT + "static/addons/" + addonID + "/" + "info");
            DownloadFile(g_ROOT + "static/addons/" + addonID + "/info", g_TEMP + addonID, "addonInfo|" + addonID);
        }
        static void checkAddons()
        {
            string json = "{";
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
            if (json.Length > 1)
            {
                json = json.Substring(0, json.Length - 1);
            }
            json += "}";
            sendQueue.Add(json, true);
        }

        static bool checkDotaPath()
        {
            bool failed = false;
            if (Properties.Settings.Default.dotaPath == String.Empty)
            {
                string path = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 570", "InstallLocation", null).ToString();

                if (path != String.Empty)
                {
                    updateDotaPath(path + @"\");
                }
            }
            else
            {
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
        static void updateDotaPath(string newPath)
        {
            if (!Directory.Exists(newPath))
            {
                log("Directory does not exist: " +  newPath);
            }
            else
            {
                try
                {
                    dotaPath = newPath;
                    Properties.Settings.Default.dotaPath = dotaPath;
                    Properties.Settings.Default.Save();
                    log("Updated dota path: " + dotaPath);
                }
                catch
                {
                    log("Failed to update path: Uncaught exception");
                }
            }
        }

        static void beginWSocket()
        {
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
            keepAlive = new System.Timers.Timer(60000);
            keepAlive.Elapsed += exit;
            keepAlive.Start();
        }

        static void doEvents()
        {
            while (true)
            {
                System.Windows.Forms.Application.DoEvents();
                if (plzClose && canClose == 0)
                {
                    Environment.Exit(0);
                }
            }
        }

        static void hookDownloadEvents()
        {
            dlManager.DownloadProgressChanged += dlManager_DownloadProgressChanged;
            dlManager.DownloadFileCompleted += dlManager_DownloadFileCompleted;
        }

        static void DownloadFile(string sourceFile, string targetName, string downloadID)
        {
            canClose++;
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
                            canClose--;
                            return;
                        }
                        log("[Download] Begin download of " + sourceFile + " -> " + targetName);
                        canClose++;
                        dlManager.DownloadFileAsync(new Uri(sourceFile), targetName, downloadID);
                    }
                }
                catch (Exception)
                {
                    log("Uncaught exception.");
                    canClose--;
                }
                canClose--;
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
            canClose--;
        }

        static string calculateCRC(string fileName)
        {
            canClose++;
            Crc32 crc32 = new Crc32();
            String hash = String.Empty;
            using (FileStream fs = File.Open(fileName, FileMode.Open))
                foreach (byte b in crc32.ComputeHash(fs))
                {
                    hash += b.ToString("x2").ToLower();
                }
            canClose--;
            return hash;
        }

        static void registerProtocol()
        {
            canClose++;
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
                    canClose--;
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
            canClose--;
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
            File.AppendAllText(g_BASEPATH + "log.txt", str + "\n");
        }

    }

}
