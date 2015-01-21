using Alchemy.Classes;
using DotaHostClientLibrary;
using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Timers;
using System.Windows.Forms;

namespace DotaHostManager
{
    class Program
    {
        // Addon status consts
        private const byte AddonStatusError = 0;
        private const byte AddonStatusMissing = 1;
        private const byte AddonStatusUpdate = 2;
        private const byte AddonStatusReady = 3;

        private static readonly string GameInfo = "\"GameInfo\"" + Environment.NewLine +
                    "{" + Environment.NewLine +
                        "game \"DOTA 2\"" + Environment.NewLine +
                        "gamelogo 1" + Environment.NewLine +
                        "type multiplayer_only" + Environment.NewLine +
                        "nomodels 1" + Environment.NewLine +
                        "nohimodel 1" + Environment.NewLine +
                        "nocrosshair 0" + Environment.NewLine + "GameData \"dota.fgd\"" + Environment.NewLine +
                        "SupportsDX8 0" + Environment.NewLine +
                        "FileSystem" + Environment.NewLine +
                        "{" + Environment.NewLine +
                            "SteamAppId 816" + Environment.NewLine +
                            "ToolsAppId 211" + Environment.NewLine +
                            "SearchPaths" + Environment.NewLine +
                            "{" + Environment.NewLine +
                                "GameBin |gameinfo_path|addons/metamod/bin" + Environment.NewLine +
                                "Game |gameinfo_path|." + Environment.NewLine +
                                "Game platform" + Environment.NewLine +
                                "Game |gameinfo_path|addons_dotahost/active" + Environment.NewLine +
                            "}" + Environment.NewLine +
                        "}" + Environment.NewLine +
                    "}";

        // CRC of this exe
        private static string _crc = "";

        // Keep-alive timer
        private static System.Timers.Timer _keepAlive;

        // Keep-alive duration
        private const int KeepAliveDuration = 60000; // 60 seconds

        // Increments by 1 everytime an action is started, decrements every time an action is finished. The program will not close on timeout unless this is zero
        private static byte _zeroCanClose;

        // If this is true, the program requests close, but will not close until zeroCanClose is equal to zero
        private static bool _requestClose;

        // This is our download manager.
        private static readonly DownloadManager DlManager = new DownloadManager();

        // Path to dota, eg: C:\Program Files (x86)\Steam\steamapps\dota 2 beta\
        private static string _dotaPath = "";

        // Prevent running exit code more than once
        private static bool _exiting;

        // Our websocket server
        private static readonly WebSocketServer WsServer = new WebSocketServer(2074);

        private static void Main(string[] i)
        {
            // Reset log file
            File.Delete(Global.BasePath + "log.txt");

            Application.ThreadException += Application_ThreadException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;


            if (i.Length > 1 && i[0] == "crc")
            {
                Console.WriteLine(Helpers.CalculateCrc(i[1]));
                Console.ReadLine();
                return;
            }

            File.Delete(Global.BasePath + "DotaHostManagerUpdater.exe");

            // Create temp directory if it doesn't exist
            Directory.CreateDirectory(Global.Temp);

            if (Global.Temp != Global.BasePath)
            {
                if (!File.Exists(Global.Temp + "DotaHostManager.exe"))
                {
                    CopyAndDeleteSelf();
                }
                else
                {
                    File.Delete(Global.Temp + "DotaHostManager.exe");
                    CopyAndDeleteSelf();
                }
                return;
            }

            // Hook the dotaHostManager socket events
            HookWSocketEvents();

            // Download the version file from website
            DownloadAppVersion();

            // Attempts to find the dota path, if it can't find it, sets it to 'unknown'
            CheckDotaPath();

            // Try to patch gameinfo
            if (!GetGameInfoPatched())
            {
                PatchGameInfo();
            }

            // Start websocket server
            Timers.SetTimeout(500, Timers.Milliseconds, WsServer.Start);

            // If first-run or requested autorun, attempt to register the uri protocol
            Console.WriteLine(Properties.Settings.Default.autorun);
            if (Properties.Settings.Default.shouldRegister)
            {
                RegisterProtocol();
            }
            if (Properties.Settings.Default.shouldDeregister)
            {
                DeregisterProtocol();
            }

            // Begin exit timer
            AppKeepAlive();

            // Event loop to prevent program from exiting
            Timers.SetInterval(1, Timers.Seconds, DoEvents);
        }

        private static bool GetGameInfoPatched()
        {
            return File.ReadAllText(_dotaPath + @"dota\gameinfo.txt") == GameInfo;
        }

        // Copies this application to temp, then deletes itself
        private static void CopyAndDeleteSelf()
        {
            using (var inputFile = new FileStream(
                        Helpers.FullExePath,
                        FileMode.Open,
                        FileAccess.Read,
                        FileShare.ReadWrite
                    ))
            {
                using (var outputFile = new FileStream(Global.Temp + "DotaHostManager.exe", FileMode.Create))
                {
                    var buffer = new byte[0x10000];
                    int bytes;

                    while ((bytes = inputFile.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        outputFile.Write(buffer, 0, bytes);
                    }
                }
            }
            Process.Start(Global.Temp + "DotaHostManager.exe");
            Exit();
        }

        // Download the most up-to-date version file of the app
        private static void DownloadAppVersion()
        {
            Helpers.Log(string.Format(Global.DownloadPathAddonInfo, "DotaHostManager"));
            Helpers.Log(Global.Temp + "DotaHostManager.txt");
            DlManager.Download(string.Format(Global.DownloadPathAddonInfo, "DotaHostManager"), Global.Temp + "DotaHostManager.txt", e => { }, e =>
            {
                Helpers.Log("[Update] Checking for updates...");
                //try
                //{
                Console.WriteLine("1");
                // Reads the version file from temp
                string[] managerVersionCrc = File.ReadAllLines(Global.Temp + "DotaHostManager.txt");
                Console.WriteLine("2");
                // Clean up file
                File.Delete(Global.Temp + "DotaHostManager.txt");

                Console.WriteLine("3");
                Console.WriteLine(GetCrc());
                // Checks if the read version matches the const version
                if (managerVersionCrc[1] != GetCrc())
                {
                    // They do not match, download new version
                    Helpers.Log("[Update] New version detected!");

                    DlManager.DownloadSync(string.Format(Global.DownloadPathAddonInfo, "DotaHostManagerUpdater"), Global.Temp + "DotaHostManagerUpdater.txt");

                    string[] updaterVersionCrc = File.ReadAllLines(Global.Temp + "DotaHostManagerUpdater.txt");

                    Helpers.Log("[Update] Downloading updater...");

                    DlManager.Download(updaterVersionCrc[0], Global.Temp + "DotaHostManagerUpdater.exe", e2 =>
                    {
                        AppUpdaterDownloadProgress(e2.ProgressPercentage);
                    }, e2 =>
                    {
                        // Begin the updater
                        StartUpdater();
                    });
                }
                else
                {
                    Helpers.Log("[Update] DotaHost up-to-date!");
                }
                //}
                //catch
                //{
                //    Helpers.log("[Update] Updating failed.");
                //}
            });
        }

        // Calculate CRC and store it in CRC variable, if already calculated, just return CRC variable
        private static string GetCrc()
        {
            Console.WriteLine("getCRC 1");
            if (_crc == "")
            {
                Console.WriteLine("getCRC 2");
                _crc = Helpers.CalculateCrc(Helpers.FullExePath);
            }
            Console.WriteLine("getCRC 3");
            return _crc;
        }

        // Called every time the app updater download progresses
        private static void AppUpdaterDownloadProgress(int percentage)
        {
            WsServer.Send(Helpers.PackArguments("appUpdater", "percent", percentage.ToString()));
        }

        // Exits the program as soon as it is finished the current task
        private static void Exit(object sender, ElapsedEventArgs e)
        {
            _requestClose = true;
            Helpers.Log("[Time Out] Exiting...");
        }

        // Starts the updater and closes this program
        private static void StartUpdater()
        {
            Helpers.Log("[Update] Starting...");
            var proc = new ProcessStartInfo
            {
                UseShellExecute = true,
                WorkingDirectory = Global.Temp,
                FileName = "DotaHostManagerUpdater.exe"
            };
            try
            {
                Process.Start(proc);
                Exit();
            }
            catch
            {
                // ignored
            }
        }

        // Generates a json structure of installed addon information, sends it to client
        private static void CheckAddons(UserContext c)
        {
            if (!Directory.Exists(_dotaPath + @"dota\addons_dotahost"))
            {
                Directory.CreateDirectory(_dotaPath + @"dota\addons_dotahost");
            }
            DlManager.DownloadSync(Global.Root + "addons/addons.txt", Global.Temp + "addons.txt");
            var addonsList = File.ReadAllLines(Global.Temp + "addons.txt");
            Helpers.DeleteSafe(Global.Temp + "addons.txt");
            foreach (string t in addonsList)
            {
                try
                {
                    string addonId = t;
                    string downloadPath = string.Format(Global.DownloadPathAddonInfo, addonId);
                    DlManager.DownloadSync(downloadPath, Global.Temp + addonId + ".txt");
                    var info = File.ReadAllLines(Global.Temp + addonId + ".txt");
                    Helpers.DeleteSafe(Global.Temp + addonId + ".txt");
                    if (info.Length != 2)
                    {
                        Helpers.Log("ERROR: Infopacket for " + addonId + " is corrupted! Got " + info.Length + " lines instead of 2.");
                        c.Send(Helpers.PackArguments("addonStatus", AddonStatusError.ToString(), addonId));
                        continue;
                    }

                    string correctCrc = info[1];

                    // Check if the addon is already downloaded
                    if (File.Exists(AddonDownloader.GetAddonInstallLocation() + addonId + ".zip"))
                    {
                        // Check the CRC
                        var actualCrc = Helpers.CalculateCrc(AddonDownloader.GetAddonInstallLocation() + addonId + ".zip");

                        // If it matches, we're already upto date
                        c.Send(actualCrc == correctCrc
                            ? Helpers.PackArguments("addonStatus", AddonStatusReady.ToString(), addonId)
                            : Helpers.PackArguments("addonStatus", AddonStatusUpdate.ToString(), addonId));
                    }
                    else
                    {
                        c.Send(Helpers.PackArguments("addonStatus", AddonStatusMissing.ToString(), addonId));
                    }
                }
                catch
                {
                    // ignored
                }
            }
        }

        // Attempts to find the dota path, returns false if not found
        private static bool CheckDotaPath()
        {
            try
            {
                // Checks if the path is already set
                if (Properties.Settings.Default.dotaPath == String.Empty)
                {
                    // Gets dota path from registry
                    string path = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 570", "InstallLocation", null).ToString();

                    // If path is found, update it in the program
                    if (path != String.Empty)
                    {
                        UpdateDotaPath(path + @"\");
                    }
                }
                else
                {
                    // Path has already been found, set it to the stored setting
                    _dotaPath = Properties.Settings.Default.dotaPath;

                    // Update addon downloader
                    AddonDownloader.SetAddonInstallLocation(string.Format(Global.ClientAddonInstallLocation, _dotaPath));
                }
                Helpers.Log("Found dota path: " + _dotaPath);
                return true;
            }
            catch
            {
                Helpers.Log("Could not find dota path. Enter dota path manually on website.");
                UpdateDotaPath("");
                return false;
            }
        }

        // Updates the dota path
        private static void UpdateDotaPath(string newPath)
        {
            // Check if newPath is a valid directory
            if (!Directory.Exists(newPath))
            {
                Helpers.Log("Directory does not exist: " + newPath);
            }
            else
            {
                try
                {
                    // Sets dotaPath and settings to the new path
                    _dotaPath = newPath;
                    Properties.Settings.Default.dotaPath = _dotaPath;
                    Properties.Settings.Default.Save();
                    Helpers.Log("Updated dota path: " + _dotaPath);

                    // Update the addon downloads
                    AddonDownloader.SetAddonInstallLocation(string.Format(Global.ClientAddonInstallLocation, _dotaPath));

                    WsServer.Send(Helpers.PackArguments("dotaPath", newPath));
                }
                catch
                {
                    // Whoops, something went wrong
                    Helpers.Log("Failed to update path: Uncaught exception");
                    WsServer.Send(Helpers.PackArguments("dotaPath", ""));
                }
            }
        }

        // Create and bind the functions for web socket events
        private static void HookWSocketEvents()
        {
            WsServer.AddHook("setDotaPath", SetDotaPathHook);

            WsServer.AddHook("exit", ExitHook);

            WsServer.AddHook(WebSocketServer.TypeConnected, ConnectedHook);

            WsServer.AddHook("autorun", AutorunHook);

            WsServer.AddHook("getAutorun", GetAutorunHook);

            WsServer.AddHook("getDotapath", GetDotapathHook);

            WsServer.AddHook("uninstall", UninstallHook);

            WsServer.AddHook("update", UpdateHook);

            WsServer.AddHook("getAddonStatus", GetAddonStatusHook);

            WsServer.AddHook(WebSocketServer.TypeReceive, ReceiveHook);

            WsServer.AddHook("gameServerInfo", GameServerInfoHook);

            WsServer.AddHook("getPatchGameInfo", GetPatchGameInfoHook);

            WsServer.AddHook("patchGameInfo", PatchGameInfoHook);
        }

        private static void SetDotaPathHook(UserContext c, string[] x)
        {

            if (!ValidateConnection(c)) { return; }
            UpdateDotaPath(x[1]);
        }

        private static void ExitHook(UserContext c, string[] x)
        {

            if (!ValidateConnection(c)) { return; }
            _requestClose = true;
        }

        private static void ConnectedHook(UserContext c)
        {

            if (!ValidateConnection(c)) { return; }
            c.Send(Helpers.PackArguments("autorun", (Properties.Settings.Default.autorun ? "1" : "0")));
            c.Send(Helpers.PackArguments("dotaPath", Properties.Settings.Default.dotaPath));
        }

        private static void AutorunHook(UserContext c, string[] x)
        {

            if (!ValidateConnection(c)) { return; }
            Console.WriteLine("autorun receive");
            Properties.Settings.Default.shouldRegister = true;
            Properties.Settings.Default.Save();
            RegisterProtocol();
        }

        private static void GetAutorunHook(UserContext c, string[] x)
        {

            if (!ValidateConnection(c)) { return; }
            c.Send(Helpers.PackArguments("autorun", (Properties.Settings.Default.autorun ? "1" : "0")));
        }

        private static void GetDotapathHook(UserContext c, string[] x)
        {
            if (!ValidateConnection(c)) { return; }
            c.Send(Helpers.PackArguments("dotaPath", _dotaPath));
        }

        private static void UninstallHook(UserContext c, string[] x)
        {

            if (!ValidateConnection(c)) { return; }
            DeregisterProtocol();
            Helpers.Log("Uninstall received");
        }

        private static void UpdateHook(UserContext c, string[] x)
        {
            if (!ValidateConnection(c)) { return; }
            c.Send("startInstall");
            AddonDownloader.UpdateAddon(x[1], (addonId, success) =>
            {
                // Tell the server what happened
                WsServer.Send(Helpers.PackArguments(success ? "installationComplete" : "installationFailed"));
            }, (addonId, e) =>
            {
                // If a socket connection has previously been opened, send the progress percentage in a formatted string
                WsServer.Send(Helpers.PackArguments("addon", addonId, e.ProgressPercentage.ToString()));
            });
        }

        private static void GetAddonStatusHook(UserContext c, string[] x)
        {

            if (!ValidateConnection(c)) { return; }
            CheckAddons(c);
        }

        private static void ReceiveHook(UserContext c)
        {
            if (!ValidateConnection(c)) { return; }
            AppKeepAlive();
        }

        private static void GameServerInfoHook(UserContext c, string[] x)
        {
            if (!ValidateConnection(c)) { return; }
            Lobby lobby = new Lobby(Kv.Parse(x[2], true));
            AddonCompiler.CompileAddons(lobby, AddonDownloader.GetAddonInstallLocation(), _dotaPath + @"dota\addons_dotahost\active\");
            c.Send(Helpers.PackArguments("connectToServer", x[1]));
        }

        private static void GetPatchGameInfoHook(UserContext c, string[] x)
        {

            if (!ValidateConnection(c)) { return; }
            c.Send(Helpers.PackArguments("patchGameInfo", GetGameInfoPatched() ? "1" : "0"));
        }

        private static void PatchGameInfoHook(UserContext c, string[] x)
        {

            if (!ValidateConnection(c)) { return; }
            c.Send(Helpers.PackArguments("tryPatchGameInfo", PatchGameInfo() ? "1" : "0"));
        }


        private static bool ValidateConnection(UserContext c)
        {
            if (c == WsServer.GetConnections()[WsServer.GetConnectionsCount() - 1])
            {
                return true;
            }
            else
            {
                c.Send("invalid");
                return false;
            }
        }

        // Attempt to patch gameinfo.txt
        private static bool PatchGameInfo()
        {
            Helpers.Log("Patching gameinfo.txt...");
            if (!ProcessIsRunning("dota"))
            {
                if (Source1GameInfoPatch())
                {
                    WsServer.Send(Helpers.PackArguments("gameinfo", "1"));
                    Helpers.Log("Patching gameinfo.txt success!");
                    return true;
                };
            }
            Helpers.Log("Patching gameinfo.txt failure: dota.exe running");
            return false;
        }

        // Check if a process name is running.
        private static bool ProcessIsRunning(string process)
        {
            return (Process.GetProcessesByName(process).Length != 0);
        }

        // Updates gameinfo.txt to match DotaHost
        private static bool Source1GameInfoPatch()
        {
            // May need to add addon mounting here eventually

            // Write the metamod loader
            try
            {
                File.WriteAllText(_dotaPath + @"dota\gameinfo.txt", GameInfo);
                return true;
            }
            catch
            {
                return false;
            }
        }

        // Removes the old timer, and ccreates and binds another one
        private static void AppKeepAlive()
        {
            if (_keepAlive != null)
            {
                _keepAlive.Dispose();
            }
            _keepAlive = new System.Timers.Timer(KeepAliveDuration);
            _keepAlive.Elapsed += Exit;
            _keepAlive.Start();
        }

        // Application.DoEvents() + check closeRequest and zeroCanClose
        private static void DoEvents()
        {
            Application.DoEvents();
            if (_requestClose && _zeroCanClose == 0)
            {
                Exit();
            }
        }

        // Function to request registration of the dotahost uri protocol
        private static void RegisterProtocol()
        {

            // Begin task
            _zeroCanClose++;

            Helpers.Log("[Protocol] Begin register...");

            // Stores the full executable path of this application, file name included
            string applicationPath = Helpers.FullExePath;

            // Opens the key "dotahost" and stores it in key
            var key = Registry.ClassesRoot.OpenSubKey("dotahost");

            string[] subkeys = Registry.ClassesRoot.GetSubKeyNames();

            bool found = subkeys.Any(t => t == "dotahost");

            // If the protocol is not registered yet, we register it
            if (!found)
            {
                try
                {
                    Helpers.Log("[Protocol] Key not found, attempting create...");

                    // Creates the subkey in the registry
                    key = Registry.ClassesRoot.CreateSubKey("dotahost");

                    // Register the URI protocol in registry
                    if (key == null)
                    {
                        throw new Exception("Registry key 1 was null.");
                    }

                    key.SetValue(string.Empty, "URL:DotaHost Protocol");
                    key.SetValue("URL Protocol", string.Empty);


                    // Set URI to launch the application with one given argument
                    key = key.CreateSubKey(@"shell\open\command");
                    if (key == null)
                    {
                        throw new Exception("Registry key 2 was null.");
                    }
                    key.SetValue(string.Empty, "\"" + applicationPath + "\" " + "%1");

                    Properties.Settings.Default.shouldRegister = false;
                    Properties.Settings.Default.autorun = true;
                    Properties.Settings.Default.Save();

                    WsServer.Send(Helpers.PackArguments("autorun", (Properties.Settings.Default.autorun ? "1" : "0")));

                    Helpers.Log("[Protocol] Registry added.");
                }
                catch
                {
                    Helpers.Log("[Protocol] Failed to add registry. Requesting launch as admin...");
                    if (MessageBox.Show("Would you like DotaHostManager to launch itself automatically when you visit the DotaHost.net website? (Requires Administrator Privillages)", "Enable Autorun", MessageBoxButtons.YesNo, MessageBoxIcon.None, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly) == DialogResult.Yes)
                    {
                        const int errorCancelled = 1223; //The operation was canceled by the user.

                        // Start a new instance of this executable as administrator
                        var info = new ProcessStartInfo(Helpers.FullExePath)
                        {
                            UseShellExecute = true,
                            Verb = "runas"
                        };
                        try
                        {
                            Properties.Settings.Default.shouldRegister = true;
                            Properties.Settings.Default.Save();
                            Process.Start(info);
                            Exit();
                        }
                        catch (Win32Exception ex)
                        {
                            if (ex.NativeErrorCode == errorCancelled)
                            {
                                Properties.Settings.Default.shouldRegister = false;
                                Properties.Settings.Default.Save();
                                Helpers.Log("[Protocol] Admin request denied.");

                                WsServer.Send(Helpers.PackArguments("autorun", (Properties.Settings.Default.autorun ? "1" : "0")));
                            }
                        }
                    }
                    else
                    {
                        // Save user preference
                        Properties.Settings.Default.shouldRegister = false;
                        Properties.Settings.Default.Save();
                        Helpers.Log("[Protocol] User declined autorun option.");

                        WsServer.Send(Helpers.PackArguments("autorun", (Properties.Settings.Default.autorun ? "1" : "0")));
                    }
                }
            }
            else
            {
                try
                {
                    key = key.CreateSubKey(@"shell\open\command");
                    if (key == null)
                    {
                        throw new Exception("Registry key 3 was null.");
                    }
                    key.SetValue(string.Empty, "\"" + applicationPath + "\" " + "%1");
                    Helpers.Log("[Protocol] Registry updated.");
                }
                catch
                {
                    Helpers.Log("[Protocol] No registry action taken: Not running as Admin.");
                }
            }

            // We're done with the registry, close the key
            if (key != null)
            {
                key.Close();
            }

            Helpers.Log("[Protocol] Done.");

            // End task
            _zeroCanClose--;
        }

        // Function to request registration of the dotahost uri protocol
        private static void DeregisterProtocol()
        {

            // Begin task
            _zeroCanClose++;


            string[] subkeys = Registry.ClassesRoot.GetSubKeyNames();

            bool found = subkeys.Any(t => t == "dotahost");

            if (found)
            {
                try
                {
                    Registry.ClassesRoot.DeleteSubKeyTree("dotahost");
                    Properties.Settings.Default.shouldDeregister = false;
                    Properties.Settings.Default.autorun = false;
                    Properties.Settings.Default.Save();



                    WsServer.Send(Helpers.PackArguments("autorun", (Properties.Settings.Default.autorun ? "1" : "0")));
                }
                catch
                {
                    if (MessageBox.Show("Would you like to remove Autorun functionality from the DotaHost ModManager? (Requires Administrator Privillages)", "Disable Autorun", MessageBoxButtons.YesNo, MessageBoxIcon.None, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly) == DialogResult.Yes)
                    {
                        Helpers.Log("[Protocol] Failed to remove registry. Launching as admin...");
                        const int errorCancelled = 1223; //The operation was canceled by the user.

                        // Start a new instance of this executable as administrator
                        var info = new ProcessStartInfo(Helpers.FullExePath)
                        {
                            UseShellExecute = true,
                            Verb = "runas"
                        };
                        try
                        {
                            Properties.Settings.Default.shouldDeregister = true;
                            Properties.Settings.Default.Save();
                            Process.Start(info);
                            Exit();
                        }
                        catch (Win32Exception ex)
                        {
                            if (ex.NativeErrorCode == errorCancelled)
                            {
                                Properties.Settings.Default.shouldDeregister = false;
                                Properties.Settings.Default.Save();
                                Helpers.Log("[Protocol] Admin request denied.");

                                WsServer.Send(Helpers.PackArguments("autorun", (Properties.Settings.Default.autorun ? "1" : "0")));
                            }
                        }
                    }
                    else
                    {
                        Helpers.Log("[Protocol] User declined disable autorun option.");

                        Properties.Settings.Default.shouldDeregister = false;
                        Properties.Settings.Default.Save();

                        WsServer.Send(Helpers.PackArguments("autorun", (Properties.Settings.Default.autorun ? "1" : "0")));
                    }
                }
            }
            else
            {
                Helpers.Log("[Protocol] Key not found.");

                Properties.Settings.Default.shouldDeregister = false;
                Properties.Settings.Default.Save();

                WsServer.Send(Helpers.PackArguments("autorun", (Properties.Settings.Default.autorun ? "1" : "0")));
            }


            // End task
            _zeroCanClose--;
        }

        // Deletes the exe if autorun is false
        private static void Exit()
        {
            if (!_exiting)
            {
                _exiting = true;
                if (!Properties.Settings.Default.autorun)
                {
                    var info = new ProcessStartInfo("cmd.exe", "/C ping 1.1.1.1 -n 1 -w 1000 > Nul & Del \"" + Helpers.FullExePath + "\"")
                    {
                        WindowStyle = ProcessWindowStyle.Hidden
                    };
                    var process = Process.Start(info);
                    if (process != null) process.Dispose();
                    File.Delete(Global.BasePath + "log.txt");
                }
                Timers.SetTimeout(500, Timers.Milliseconds, () => { Environment.Exit(0); });
            }
        }

        private static void Application_ThreadException(object sender, ThreadExceptionEventArgs eo)
        {
            var e = eo.Exception;
            MessageBox.Show(e.Message + Environment.NewLine + Environment.NewLine + "Please check reddit.com/r/dotahost for possible solutions. If you cannot find an answer, useful information has been copied to the clipboard, and you may text post the contents.", "DotaHost ModManager - Error");
            Helpers.Log(e.ToString());
            Exit();
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs eo)
        {
            var e = (eo.ExceptionObject as Exception);
            MessageBox.Show(e.Message + Environment.NewLine + Environment.NewLine + "Please check reddit.com/r/dotahost for possible solutions. If you cannot find an answer, useful information has been copied to the clipboard, and you may text post the contents.", "DotaHost ModManager - Error");
            Helpers.Log(e.ToString());
            Exit();
        }
    }

}
