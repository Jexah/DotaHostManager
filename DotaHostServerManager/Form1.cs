using Alchemy.Classes;
using DotaHostClientLibrary;
using DotaHostLibrary;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace DotaHostServerManager
{
    public partial class Form1 : Form
    {
        // Colours for UI
        private static readonly Color Success = Color.Green;
        private static readonly Color Warning = Color.Orange;
        private static readonly Color Danger = Color.Red;

        // Initialize boxManagers dictionary
        private static readonly BoxManagers BoxManagers = new BoxManagers();

        // Create WebSocketServer
        private static readonly WebSocketServer WsServer = new WebSocketServer(Runabove.ServerManagerPort);

        // This is our lobby manager 
        private static UserContext _lobbyManager;

        // Soft BoxManager limit
        private static byte _serverSoftCap;

        // Hard BoxManager limit
        private const byte ServerHardCap = 2;


        public Form1()
        {
            File.Delete("log.txt");

            _serverSoftCap = 2;

            InitializeComponent();

            // Hook socket events
            HookWSocketServerEvents();

            // Start the websocket server, wait for incomming connections
            WsServer.Start();
        }

        private void UpdateCurrentBoxGameServers()
        {
            ModGui(boxesList, () =>
            {
                if (boxesList.SelectedItem != null)
                {
                    string ip = boxesList.SelectedItem.ToString();
                    WsServer.Send("gameServers", ip);
                }
                Timers.SetTimeout(5, Timers.Seconds, UpdateCurrentBoxGameServers);
            });

        }

        // Socket hooks go here
        private void HookWSocketServerEvents()
        {
            // When a server is started, it sends box function, so this tells the servermanager "Hey, there's a new box in town" and the server manager does it's things to accomodate
            WsServer.AddHook("box", BoxHook);

            // Receives the system status from the box manager, loops
            WsServer.AddHook("system", SystemHook);

            // Adds disconnect hook, removes box manager from list, refreshes server browser listbox
            WsServer.AddHook(WebSocketServer.TypeDisconnected, DisconnectedHook);

            // Receive game server request from webserver
            WsServer.AddHook("createGameServer", CreateGameServerHook);

            // Receive confirmation of createGameServer from BoxManager
            WsServer.AddHook("gameServerInfo", GameServerInfoHook);

            // A game server has exited
            WsServer.AddHook("gameServerExit", GameServerExitHook);

            WsServer.AddHook("lobbyManager", LobbyManagerHook);

        }

        private void BoxHook(UserContext c, string[] x)
        {
            if (BoxManagers.ContainsKey(c.ClientAddress.ToString()))
            {
                return;
            }


            // Initialize the new BoxManager
            var boxManager = new BoxManager
            {
                Ip = c.ClientAddress.ToString(),
                Region = "None",
                InstanceId = "None",
                ThirdParty = true
            };



            // Get a list of servers
            Runabove.GetServers(serverList =>
            {
                // Finds the box manager in the list of servers by matching the IPs
                serverList.ForEach((server, i) =>
                {
                    string serverIp = server.Ip;
                    string boxIp = c.ClientAddress.ToString().Split(':')[0];

                    Helpers.Log("ServerIP: " + serverIp);
                    Helpers.Log("Box IP  " + boxIp);

                    if (serverIp != boxIp && serverIp.Split(':')[0] != "127.0.0.1") return false;

                    boxManager.ThirdParty = false;

                    // Sets the subID so it can be destroyed later
                    boxManager.InstanceId = server.InstanceId;

                    // Sets the region so we know where it is hosted
                    boxManager.Region = server.Region;

                    return true;
                });


                // Send BoxManager object to server so it knows its place
                c.Send(Helpers.PackArguments("box", boxManager.ToString()));
            });

            // Add the box manager to the list
            BoxManagers.AddBoxManager(boxManager);

            ModGui(boxesList, () =>
            {
                if (boxesList.SelectedItem != null && boxesList.SelectedItem.ToString() == boxManager.Ip)
                {
                    SetBoxStatsGui(boxManager);
                }
            });
        }

        private void SystemHook(UserContext c, string[] x)
        {
            if (!BoxManagers.ContainsKey(c.ClientAddress.ToString()))
            {
                return;
            }

            var boxManager = new BoxManager(Kv.Parse(x[1]));

            BoxManagers.AddBoxManager(boxManager);

            // Request GUI-safe thread
            ModGui(boxesList, () =>
            {
                // Add BoxManager to the GUI List if it's not there
                if (!boxesList.Items.Contains(c.ClientAddress.ToString()))
                {
                    boxesList.Items.Add(c.ClientAddress.ToString());
                }

                // If the box manager is in the list and is selected, update the stats
                if (boxesList.SelectedItem != null && boxesList.SelectedItem.ToString() == c.ClientAddress.ToString())
                {
                    SetBoxStatsGui(boxManager);
                }
            });

            // Set timeout to request the system info again
            Timers.SetTimeout(1, Timers.Seconds, () => c.Send("system"));
        }

        private void DisconnectedHook(UserContext c)
        {

            // Remove boxmanager from the list
            BoxManagers.RemoveBoxManager(c.ClientAddress.ToString());

            // Update listbox
            ModGui(boxesList, () => boxesList.Items.Remove(c.ClientAddress));
        }

        private static void CreateGameServerHook(UserContext c, string[] x)
        {

            Helpers.Log("Received createGameServer from lobby");
            var lobby = new Lobby(Kv.Parse(x[1]));

            var boxManager = FindBoxManager(lobby);

            if (boxManager != null)
            {
                Helpers.Log("Found game server");
                CreateGameServer(boxManager, lobby);
            }
            else
            {
                c.Send(Helpers.PackArguments("gameServerInfo", "failed", lobby.ToString()));
            }
        }

        private static void GameServerInfoHook(UserContext c, string[] x)
        {
            if (x[1] != "success") return;

            var gameServer = new GameServer(Kv.Parse(x[2]));

            Helpers.Log("GAME SERVER: " + gameServer.ToString());

            gameServer.Lobby.ForEachPlayer(player =>
            {
                _lobbyManager.Send(Helpers.PackArguments("gameServerInfo", "success", gameServer.ToString()));
            });
        }

        private static void GameServerExitHook(UserContext c, string[] x)
        {
            if (x[2] == "good")
            {

            }
            else if (x[2] == "error")
            {

            }
            Helpers.Log("gameServerExit received (and sent)");
            _lobbyManager.Send(Helpers.PackArguments("gameServerExit", x[2]));
        }

        private static void LobbyManagerHook(UserContext c, string[] x)
        {
            if (c.ClientAddress.ToString().Split(':')[0] == "127.0.0.1")
            {
                _lobbyManager = c;
            }
        }




        // All BoxManager/Gameserver related events here
        #region BoxManager/GameServer related events

        // Create a new box instance using snapshot
        private static void AddBoxManager(string region)
        {
            Runabove.GetServers(serverList =>
            {
                if (serverList.Count >= ServerHardCap)
                {
                    Helpers.Log("SERVER HARD CAP REACHED: SERVER NOT CREATED");
                }
                else if (serverList.Count >= _serverSoftCap)
                {
                    Helpers.Log("SERVER SOFT CAP REACHED: SERVER NOT CREATED");
                }
                else
                {
                    Runabove.CreateServer(region);
                }
            });
        }

        // Destroy box instance
        private void RemoveBoxManager(BoxManager boxManager, bool now = false)
        {
            string ip = boxManager.Ip;
            if (!Convert.ToBoolean(boxManager.ThirdParty))
            {
                WsServer.Send(now ? "destroy|hard" : "destroy", ip);
            }

            // Remove boxmanager from list
            BoxManagers.RemoveBoxManager(ip);

            // Update the listbox
            ModGui(boxesList, () => { boxesList.Items.Remove(ip); });
        }

        // Finds a server to host the gamemode selected, in the region selected
        private static BoxManager FindBoxManager(Lobby lobby)
        {
            int totalRam = 0;
            int totalCpu = 0;
            foreach (Addon addon in lobby.Addons.GetAddons())
            {

            }
            return BoxManagers.GetBoxManagers().Where(boxManager => boxManager.Region == lobby.Region).FirstOrDefault(boxManager => boxManager.GameServers.GetGameServers().Count < 2);
        }

        // Creates a game server
        private static void CreateGameServer(BoxManager boxManager, Lobby lobby)
        {
            Helpers.Log("createGameServer");

            var ports = boxManager.GameServers.GetGameServers().Select(gs => gs.Port).ToList();

            GameServer gameServer;
            for (ushort i = 27015; i < 27025; ++i)
            {
                if (ports.Contains(i)) continue;

                Helpers.Log("Port Found");

                gameServer = new GameServer
                {
                    Port = i,
                    Lobby = lobby,
                    Ip = boxManager.Ip
                };

                Helpers.Log("sent create gameserver to boxmanager");
                WsServer.Send(Helpers.PackArguments("create", gameServer.ToString()), boxManager.Ip);

                break;
            }
        }

        // Reboots the selected box
        private void RestartBox(BoxManager boxManager)
        {
            // TODO: Add box restart code here
        }

        #endregion

        // All form-related events go here
        #region FORM EVENTS

        // When the box managers list box selection changes
        private void boxesList_SelectedIndexChanged(object sender, EventArgs e)
        {
            ModGui(boxGameServerList, () =>
            {
                boxGameServerList.Items.Clear();
                boxGameServerList.Enabled = false;
            });

            // Stores selected item
            var selectedItem = boxesList.SelectedItem;

            // If the selected item is actually something
            if (selectedItem != null && selectedItem.ToString() != "")
            {
                // Set the current visible stats to those of the box manager
                string boxIp = boxesList.SelectedItem.ToString();
                SetBoxStatsGui(BoxManagers.GetBoxManager(boxIp));
                WsServer.Send("gameServers", boxIp);
            }
            else
            {
                // If there is still more than zero items in the listbox, then select index zero
                if (boxesList.Items.Count > 0)
                {
                    boxesList.SelectedIndex = 0;
                }
                else
                {
                    // Restore default values in GUI
                    SetBoxDefaultGui();
                }
            }
        }

        // Form 1 load
        private void Form1_Load(object sender, EventArgs e)
        {
            SetBoxDefaultGui();

            UpdateCurrentBoxGameServers();
        }

        // Form 1 shown (after GUI loads)
        private void Form1_Shown(object sender, EventArgs e)
        {
        }

        // Ensure the application exits when the form is closed
        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            Environment.Exit(0);
        }

        // Temporary button, creates a new server in Australia
        private void button1_Click(object sender, EventArgs e)
        {
            AddBoxManager(Runabove.Canada);
        }

        // Temporary button, destroys the selected server
        private void button2_Click(object sender, EventArgs e)
        {
            RemoveBoxManager(BoxManagers.GetBoxManager(boxesList.SelectedItem.ToString()));
        }

        #endregion

        // All pure GUI updating code goes here
        #region UPDATE GUI

        // Set the default values for the elements of the GUI
        private void SetBoxDefaultGui()
        {
            SetBoxNameGui("None");
            SetBoxStatusGui(Runabove.BoxInactive);
            SetBoxRamGui(0, 0);
            SetBoxCpuGui(0);
            SetBoxNetworkGui(0, 0);
            SetBoxVerifiedGui(false);
            ModGui(boxGameServerList, () => { boxGameServerList.Enabled = false; });
        }

        // Wrapper to find thread-safe thread to change GUI elements
        private static void ModGui(Control o, Action a)
        {
            // Begins a function invoke using a thread safe for object o
            o.BeginInvoke(new MethodInvoker(delegate { a(); }));
        }

        // Sets all the stats of the GUI to that of the given boxmanager
        private void SetBoxStatsGui(BoxManager boxManager)
        {
            SetBoxNameGui(boxManager);
            SetBoxStatusGui(boxManager);
            SetBoxCpuGui(boxManager);
            SetBoxRamGui(boxManager);
            SetBoxNetworkGui(boxManager);
            SetBoxVerifiedGui(boxManager);
            SetBoxRegionGui(boxManager);
            UpdateGameServerListGui(boxManager);
        }

        // Sets the name label to that of the name of the given name
        private void SetBoxNameGui(BoxManager boxManager)
        {
            SetBoxNameGui(boxManager.Ip);
        }
        private void SetBoxNameGui(string name)
        {
            ModGui(boxNameLabel, () => { boxNameLabel.Text = name; });
        }

        // Sets the value and color of the status label to that of the status of the given status
        private void SetBoxStatusGui(BoxManager boxManager)
        {
            SetBoxStatusGui(boxManager.Status);
        }
        private void SetBoxStatusGui(byte status)
        {
            ModGui(boxStatusLabel, () =>
            {
                switch (status)
                {
                    case Runabove.BoxActive:
                        boxStatusLabel.Text = "Active";
                        boxStatusLabel.ForeColor = Success;
                        break;
                    case Runabove.BoxMia:
                        boxStatusLabel.Text = "MIA";
                        boxStatusLabel.ForeColor = Warning;
                        break;
                    case Runabove.BoxIdle:
                        boxStatusLabel.Text = "Idle";
                        boxStatusLabel.ForeColor = Success;
                        break;
                    case Runabove.BoxInactive:
                        boxStatusLabel.Text = "Inactive";
                        boxStatusLabel.ForeColor = Danger;
                        break;
                    case Runabove.BoxDeactivated:
                        boxStatusLabel.Text = "Deactivated";
                        boxStatusLabel.ForeColor = Warning;
                        break;
                }
            });
        }

        // Sets ram labels color and value to that of the given ram levels
        private void SetBoxRamGui(BoxManager boxManager)
        {
            var ram = new ushort[2];
            ram[0] = boxManager.RamAvailable;
            ram[1] = boxManager.RamTotal;
            SetBoxRamGui(ram[0], ram[1]);
        }
        private void SetBoxRamGui(ushort remaining, ushort total)
        {
            short current = (short)(total - remaining);
            ModGui(boxRAMBar, () =>
            {
                boxRAMBar.Maximum = total;
                boxRAMBar.Value = current;
            });
            ModGui(boxRAMLabel, () =>
            {
                boxRAMLabel.Text = current + " / " + total;
                float percent = (float)current / (float)total;
                if (percent < 0.75)
                {
                    boxRAMLabel.ForeColor = Success;
                }
                else if (percent < 0.9)
                {
                    boxRAMLabel.ForeColor = Warning;
                }
                else
                {
                    boxRAMLabel.ForeColor = Danger;
                }
            });
        }

        // Sets cpu label and bar to that of the % cpu usage given
        private void SetBoxCpuGui(BoxManager boxManager)
        {
            SetBoxCpuGui(boxManager.Cpu);
        }
        private void SetBoxCpuGui(int percent)
        {
            ModGui(boxCPUBar, () => { boxCPUBar.Value = percent; });
            ModGui(boxCPULabel, () =>
            {
                boxCPULabel.Text = percent + "%";
                if (percent < 75)
                {
                    boxCPULabel.ForeColor = Success;
                }
                else if (percent < 90)
                {
                    boxCPULabel.ForeColor = Warning;
                }
                else
                {
                    boxCPULabel.ForeColor = Danger;
                }
            });
        }

        // Sets the network labels
        private void SetBoxNetworkGui(BoxManager boxManager)
        {
            uint[] network = new uint[2];
            network[0] = boxManager.Upload;
            network[1] = boxManager.Download;
            SetBoxNetworkGui(network[0], network[1]);
        }
        private void SetBoxNetworkGui(uint upload, uint download)
        {
            ModGui(boxUploadLabel, () =>
            {
                boxUploadLabel.Text = (upload * 8 / 1000) + " kb/s";
                if (upload < 7500000)
                {
                    boxUploadLabel.ForeColor = Success;
                }
                else if (upload < 9000000)
                {
                    boxUploadLabel.ForeColor = Warning;
                }
                else
                {
                    boxUploadLabel.ForeColor = Danger;
                }
            });
            ModGui(boxDownloadLabel, () =>
            {
                boxDownloadLabel.Text = (download * 8 / 1000) + " kb/s";
                if (download < 7500000)
                {
                    boxDownloadLabel.ForeColor = Success;
                }
                else if (upload < 9000000)
                {
                    boxDownloadLabel.ForeColor = Warning;
                }
                else
                {
                    boxDownloadLabel.ForeColor = Danger;
                }
            });

        }

        // Sets the network labels
        private void SetBoxVerifiedGui(BoxManager boxManager)
        {
            bool verified = !Convert.ToBoolean(boxManager.ThirdParty);
            SetBoxVerifiedGui(verified);
        }
        private void SetBoxVerifiedGui(bool verified)
        {
            ModGui(boxVerifiedLabel, () =>
            {
                boxVerifiedLabel.Text = verified.ToString();
                boxVerifiedLabel.ForeColor = verified ? Success : Danger;
            });
        }

        // Update region label
        private void SetBoxRegionGui(BoxManager boxManager)
        {
            SetBoxRegionGui(boxManager.Region);
        }
        private void SetBoxRegionGui(string region)
        {
            ModGui(boxRegionLabel, () =>
            {
                if (Runabove.RegionIdToName.ContainsKey(region))
                {
                    boxRegionLabel.Text = Runabove.RegionIdToName[region];
                    boxRegionLabel.ForeColor = Success;
                }
                else
                {
                    boxRegionLabel.Text = "None";
                    boxRegionLabel.ForeColor = Color.Black;
                }
            });
        }

        // Refreshes the GameServer list
        private void UpdateGameServerListGui(BoxManager boxManager)
        {
            if (boxManager.GameServers != null && boxManager.GameServers.GetKeys() != null)
            {
                ModGui(boxGameServerList, () =>
                {
                    boxGameServerList.Items.Clear();
                    foreach (var gameServer in boxManager.GameServers.GetKeys().Select(kvp => new GameServer(kvp.Value)))
                    {
                        boxGameServerList.Items.Add(gameServer.Ip);
                    }
                });
            }
        }

        #endregion

        private void boxUpdateButton_Click(object sender, EventArgs e)
        {
            WsServer.Send("updateServer", boxesList.SelectedItem.ToString());
        }


    }
}
