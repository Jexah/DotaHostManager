using Alchemy.Classes;
using DotaHostClientLibrary;
using DotaHostLibrary;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace DotaHostServerManager
{
    public partial class Form1 : Form
    {
        // Colours for UI
        private static Color success = Color.Green;
        private static Color warning = Color.Orange;
        private static Color danger = Color.Red;

        // Initialize boxManagers dictionary
        private static BoxManagers boxManagers = new BoxManagers();

        // Initialize addonRequirements dictionary
        private static Dictionary<string, AddonRequirements> addonRequirements = new Dictionary<string, AddonRequirements>();

        // Create WebSocketServer
        private static WebSocketServer wsServer = new WebSocketServer(Runabove.SERVER_MANAGER_PORT);

        // This is our lobby manager 
        private static UserContext lobbyManager;

        // Soft BoxManager limit
        private static byte serverSoftCap;

        // Hard BoxManager limit
        private const byte SERVER_HARD_CAP = 2;


        public Form1()
        {
            File.Delete("log.txt");

            setAddonRequirements();

            serverSoftCap = 2;

            InitializeComponent();

            // Hook socket events
            hookWSocketServerEvents();

            // Start the websocket server, wait for incomming connections
            wsServer.start();
        }

        private void updateCurrentBoxGameServers()
        {
            modGUI(boxesList, () =>
            {
                if (boxesList.SelectedItem != null)
                {
                    string ip = boxesList.SelectedItem.ToString();
                    wsServer.send("gameServers", ip);
                }
                Timers.setTimeout(5, Timers.SECONDS, () =>
                {
                    updateCurrentBoxGameServers();
                });
            });

        }

        // Set hardcoded benchmarks here
        private void setAddonRequirements()
        {
            AddonRequirements lod = new AddonRequirements(350, 15);
            addonRequirements.Add("lod", lod);
            AddonRequirements csp = new AddonRequirements(350, 15);
            addonRequirements.Add("csp", csp);
        }

        // Socket hooks go here
        private void hookWSocketServerEvents()
        {
            // Print received messages to console for debugging
            #region wsServer.addHook(WebSocketServer.RECEIVE);
            wsServer.addHook(WebSocketServer.RECEIVE, (c) =>
            {
                //Helpers.log("Receive: " + c.DataFrame.ToString());
            });
            #endregion

            // When a server is started, it sends box function, so this tells the servermanager "Hey, there's a new box in town" and the server manager does it's things to accomodate
            #region wsServer.addHook("box");
            wsServer.addHook("box", (c, x) =>
            {

                if (boxManagers.containsKey(c.ClientAddress.ToString()))
                {
                    return;
                }


                // Temporary while we don't have Runabove API working

                // Initialize the new BoxManager
                BoxManager boxManager = new BoxManager();

                boxManager.Ip = c.ClientAddress.ToString();

                boxManager.Region = "None";
                boxManager.InstanceID = "None";
                boxManager.ThirdParty = true;

                // Get a list of servers
                Runabove.getServers((serverList) =>
                {
                    // Finds the box manager in the list of servers by matching the IPs
                    serverList.forEach((server, i) =>
                    {
                        string serverIP = server.Ip;
                        string boxIP = c.ClientAddress.ToString().Split(':')[0];

                        Helpers.log("ServerIP: " + serverIP);
                        Helpers.log("Box IP  " + boxIP);

                        if (serverIP == boxIP || serverIP.Split(':')[0] == "127.0.0.1")
                        {
                            boxManager.ThirdParty = false;

                            // Sets the subID so it can be destroyed later
                            boxManager.InstanceID = server.InstanceID;

                            // Sets the region so we know where it is hosted
                            boxManager.Region = server.Region;

                            return true;
                        }
                        return false;
                    });


                    // Send BoxManager object to server so it knows its place
                    c.Send(Helpers.packArguments("box", boxManager.toString()));
                });

                // Add the box manager to the list
                boxManagers.addBoxManager(boxManager);

                modGUI(boxesList, () =>
                {
                    if (boxesList.SelectedItem != null && boxesList.SelectedItem.ToString() == boxManager.Ip)
                    {
                        setBoxStatsGUI(boxManager);
                    }
                });

            });
            #endregion

            // Receives the system status from the box manager, loops
            #region wsServer.addhook("system");
            wsServer.addHook("system", (c, x) =>
            {

                if (!boxManagers.containsKey(c.ClientAddress.ToString()))
                {
                    return;
                }

                // Create pointer to box manager
                BoxManager boxManager = boxManagers.getBoxManager(c.ClientAddress.ToString());

                boxManager = new BoxManager(KV.parse(x[1]));

                boxManagers.addBoxManager(boxManager);

                BoxManager k = boxManagers.getBoxManager(c.ClientAddress.ToString());

                // Request GUI-safe thread
                modGUI(boxesList, () =>
                {
                    // Add BoxManager to the GUI List if it's not there
                    if (!boxesList.Items.Contains(c.ClientAddress.ToString()))
                    {
                        boxesList.Items.Add(c.ClientAddress.ToString());
                    }

                    // If the box manager is in the list and is selected, update the stats
                    if (boxesList.SelectedItem != null && boxesList.SelectedItem.ToString() == c.ClientAddress.ToString())
                    {
                        setBoxStatsGUI(boxManager);
                    }
                });

                // Set timeout to request the system info again
                Timers.setTimeout(1, Timers.SECONDS, () => { c.Send("system"); });
            });
            #endregion

            // Adds disconnect hook, removes box manager from list, refreshes server browser listbox
            #region wsServer.addHook(WebSocketServer.DISCONNECTED);
            wsServer.addHook(WebSocketServer.DISCONNECTED, (c) =>
            {

                // Remove boxmanager from the list
                boxManagers.removeBoxManager(c.ClientAddress.ToString());

                // Update listbox
                modGUI(boxesList, () => { boxesList.Items.Remove(c.ClientAddress.ToString()); });

            });
            #endregion

            // Receive game server request from webserver
            #region wsServer.addHook("createGameServer");
            wsServer.addHook("createGameServer", (c, x) =>
            {
                Helpers.log("Received createGameServer from lobby");
                Lobby lobby = new Lobby(KV.parse(x[1]));

                BoxManager boxManager = findBoxManager(lobby);

                if (boxManager != null)
                {
                    Helpers.log("Found game server");
                    GameServer gameServer = createGameServer(boxManager, lobby);

                    /*if (gameServer != null)
                    {
                        c.Send(Helpers.packArguments("gameServerInfo", "success", gameServer.toString()));
                    }
                    else
                    {
                        Helpers.log("Could not find server");
                    }*/
                }
                else
                {
                    c.Send(Helpers.packArguments("gameServerInfo", "failed", lobby.toString()));
                }
            });
            #endregion

            // Receive confirmation of createGameServer from BoxManager
            #region wsServer.addHook("gameServerInfo");
            wsServer.addHook("gameServerInfo", (c, x) =>
            {
                if (x[1] == "success")
                {
                    GameServer gameServer = new GameServer(KV.parse(x[2]));

                    Helpers.log("GAME SERVER: " + gameServer.toString());

                    foreach (Team team in gameServer.Lobby.Teams.getTeams())
                    {
                        foreach (Player player in team.Players.getPlayers())
                        {
                            lobbyManager.Send(Helpers.packArguments("gameServerInfo", "success", gameServer.toString()));
                        }
                    }
                }
            });
            #endregion

            // A game server has exited
            #region wsServer.addHook("gameServerExit");
            wsServer.addHook("gameServerExit", (c, x) =>
            {
                if (x[2] == "good")
                {
                    //GameServer gameServer = new GameServer(KV.parse(x[3]));

                    // The game server has exited properly, match was good
                }
                else if (x[2] == "error")
                {
                    // The game serer had an error, doh!
                }
            });
            #endregion


            #region wsServer.addHook("lobbyManager");
            wsServer.addHook("lobbyManager", (c, x) =>
            {
                if (c.ClientAddress.ToString().Split(':')[0] == "127.0.0.1")
                {
                    lobbyManager = c;
                }
            });
            #endregion

        }

        // All BoxManager/Gameserver related events here
        #region BoxManager/GameServer related events

        // Create a new box instance using snapshot
        private static void addBoxManager(string region)
        {
            Runabove.getServers((serverList) =>
            {
                if (serverList.Count >= SERVER_HARD_CAP)
                {
                    Helpers.log("SERVER HARD CAP REACHED: SERVER NOT CREATED");
                }
                else if (serverList.Count >= serverSoftCap)
                {
                    Helpers.log("SERVER SOFT CAP REACHED: SERVER NOT CREATED");
                }
                else
                {
                    Runabove.createServer(region);
                }
            });
        }

        // Destroy box instance
        private void removeBoxManager(BoxManager boxManager, bool now = false)
        {
            string ip = boxManager.Ip;
            if (!Convert.ToBoolean(boxManager.ThirdParty))
            {
                if (now)
                {
                    // Sends instant destroy message
                    wsServer.send("destroy|hard", ip);
                }
                else
                {
                    // Waits for current games to finish, polls every minute
                    wsServer.send("destroy", ip);
                }
            }

            // Remove boxmanager from list
            boxManagers.removeBoxManager(ip);

            // Update the listbox
            modGUI(boxesList, () => { boxesList.Items.Remove(ip); });
        }

        // Finds a server to host the gamemode selected, in the region selected
        private BoxManager findBoxManager(Lobby lobby)
        {
            int totalRam = 0;
            int totalCpu = 0;
            foreach (Addon addon in lobby.Addons.getAddons())
            {
                totalRam += addonRequirements[addon.Id].Ram;
                totalCpu += addonRequirements[addon.Id].Cpu;
            }
            foreach (BoxManager boxManager in boxManagers.getBoxManagers())
            {
                if (boxManager.Region == lobby.Region)
                {
                    Helpers.log("region OK");
                    Helpers.log("Total req RAM: " + totalRam);
                    Helpers.log("Available RAM: " + boxManager.RamAvailable);
                    Helpers.log("Total req CPU: " + totalCpu);
                    Helpers.log("Available CPU: " + (100 - boxManager.Cpu).ToString());

                    if (boxManager.RamAvailable > totalRam && 100 - boxManager.Cpu > totalCpu)
                    {
                        Helpers.log("stats OK");
                        return boxManager;
                    }
                }
            }
            return null;
        }

        // Creates a game server
        private GameServer createGameServer(BoxManager boxManager, Lobby lobby)
        {
            Helpers.log("createGameServer");

            List<ushort> ports = new List<ushort>();
            foreach (GameServer gs in boxManager.GameServers.getGameServers())
            {
                ports.Add(gs.Port);
            }
            GameServer gameServer = null;
            for (ushort i = 27015; i < 27025; ++i)
            {
                if (!ports.Contains(i))
                {
                    Helpers.log("Port Found");

                    gameServer = new GameServer();
                    gameServer.Port = i;
                    gameServer.Lobby = lobby;
                    gameServer.Ip = boxManager.Ip;

                    Helpers.log("sent create gameserver to boxmanager");
                    wsServer.send(Helpers.packArguments("create", gameServer.toString()), boxManager.Ip);

                    break;
                }
            }
            return gameServer;
        }

        // Reboots the selected box
        private void restartBox(BoxManager boxManager)
        {
            // TODO: Add box restart code here
        }

        #endregion

        // All form-related events go here
        #region FORM EVENTS

        // When the box managers list box selection changes
        private void boxesList_SelectedIndexChanged(object sender, EventArgs e)
        {
            modGUI(boxGameServerList, () =>
            {
                boxGameServerList.Items.Clear();
                boxGameServerList.Enabled = false;
            });

            // Stores selected item
            object selectedItem = boxesList.SelectedItem;

            // If the selected item is actually something
            if (selectedItem != null && selectedItem.ToString() != "")
            {
                // Set the current visible stats to those of the box manager
                string boxIP = boxesList.SelectedItem.ToString();
                setBoxStatsGUI(boxManagers.getBoxManager(boxIP));
                wsServer.send("gameServers", boxIP);
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
                    setBoxDefaultGUI();
                }
            }
        }

        // Form 1 load
        private void Form1_Load(object sender, EventArgs e)
        {
            setBoxDefaultGUI();

            updateCurrentBoxGameServers();
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
            addBoxManager(Runabove.CANADA);
        }

        // Temporary button, destroys the selected server
        private void button2_Click(object sender, EventArgs e)
        {
            removeBoxManager(boxManagers.getBoxManager(boxesList.SelectedItem.ToString()));
        }

        #endregion

        // All pure GUI updating code goes here
        #region UPDATE GUI

        // Set the default values for the elements of the GUI
        private void setBoxDefaultGUI()
        {
            setBoxNameGUI("None");
            setBoxStatusGUI(Runabove.BOX_INACTIVE);
            setBoxRAMGUI(0, 0);
            setBoxCPUGUI(0);
            setBoxNetworkGUI(0, 0);
            setBoxVerifiedGUI(false);
            modGUI(boxGameServerList, () => { boxGameServerList.Enabled = false; });
        }

        // Wrapper to find thread-safe thread to change GUI elements
        private void modGUI(Control o, Action a)
        {
            // Begins a function invoke using a thread safe for object o
            o.BeginInvoke(new MethodInvoker(delegate { a(); }));
        }

        // Sets all the stats of the GUI to that of the given boxmanager
        private void setBoxStatsGUI(BoxManager boxManager)
        {
            setBoxNameGUI(boxManager);
            setBoxStatusGUI(boxManager);
            setBoxCPUGUI(boxManager);
            setBoxRAMGUI(boxManager);
            setBoxNetworkGUI(boxManager);
            setBoxVerifiedGUI(boxManager);
            setBoxRegionGUI(boxManager);
            updateGameServerListGUI(boxManager);
        }

        // Sets the name label to that of the name of the given name
        private void setBoxNameGUI(BoxManager boxManager)
        {
            setBoxNameGUI(boxManager.Ip);
        }
        private void setBoxNameGUI(string name)
        {
            modGUI(boxNameLabel, () => { boxNameLabel.Text = name; });
        }

        // Sets the value and color of the status label to that of the status of the given status
        private void setBoxStatusGUI(BoxManager boxManager)
        {
            setBoxStatusGUI(boxManager.Status);
        }
        private void setBoxStatusGUI(byte status)
        {
            modGUI(boxStatusLabel, () =>
            {
                switch (status)
                {
                    case Runabove.BOX_ACTIVE:
                        boxStatusLabel.Text = "Active";
                        boxStatusLabel.ForeColor = success;
                        break;
                    case Runabove.BOX_MIA:
                        boxStatusLabel.Text = "MIA";
                        boxStatusLabel.ForeColor = warning;
                        break;
                    case Runabove.BOX_IDLE:
                        boxStatusLabel.Text = "Idle";
                        boxStatusLabel.ForeColor = success;
                        break;
                    case Runabove.BOX_INACTIVE:
                        boxStatusLabel.Text = "Inactive";
                        boxStatusLabel.ForeColor = danger;
                        break;
                    case Runabove.BOX_DEACTIVATED:
                        boxStatusLabel.Text = "Deactivated";
                        boxStatusLabel.ForeColor = warning;
                        break;
                }
            });
        }

        // Sets ram labels color and value to that of the given ram levels
        private void setBoxRAMGUI(BoxManager boxManager)
        {
            ushort[] ram = new ushort[2];
            ram[0] = boxManager.RamAvailable;
            ram[1] = boxManager.RamTotal;
            setBoxRAMGUI(ram[0], ram[1]);
        }
        private void setBoxRAMGUI(ushort remaining, ushort total)
        {
            short current = (short)(total - remaining);
            modGUI(boxRAMBar, () =>
            {
                boxRAMBar.Maximum = total;
                boxRAMBar.Value = current;
            });
            modGUI(boxRAMLabel, () =>
            {
                boxRAMLabel.Text = current + " / " + total;
                float percent = (float)current / (float)total;
                if (percent < 0.75)
                {
                    boxRAMLabel.ForeColor = success;
                }
                else if (percent < 0.9)
                {
                    boxRAMLabel.ForeColor = warning;
                }
                else
                {
                    boxRAMLabel.ForeColor = danger;
                }
            });
        }

        // Sets cpu label and bar to that of the % cpu usage given
        private void setBoxCPUGUI(BoxManager boxManager)
        {
            setBoxCPUGUI(boxManager.Cpu);
        }
        private void setBoxCPUGUI(int percent)
        {
            modGUI(boxCPUBar, () => { boxCPUBar.Value = percent; });
            modGUI(boxCPULabel, () =>
            {
                boxCPULabel.Text = percent + "%";
                if (percent < 75)
                {
                    boxCPULabel.ForeColor = success;
                }
                else if (percent < 90)
                {
                    boxCPULabel.ForeColor = warning;
                }
                else
                {
                    boxCPULabel.ForeColor = danger;
                }
            });
        }

        // Sets the network labels
        private void setBoxNetworkGUI(BoxManager boxManager)
        {
            uint[] network = new uint[2];
            network[0] = boxManager.Upload;
            network[1] = boxManager.Download;
            setBoxNetworkGUI(network[0], network[1]);
        }
        private void setBoxNetworkGUI(uint upload, uint download)
        {
            modGUI(boxUploadLabel, () =>
            {
                boxUploadLabel.Text = (upload * 8 / 1000) + " kb/s";
                if (upload < 7500000)
                {
                    boxUploadLabel.ForeColor = success;
                }
                else if (upload < 9000000)
                {
                    boxUploadLabel.ForeColor = warning;
                }
                else
                {
                    boxUploadLabel.ForeColor = danger;
                }
            });
            modGUI(boxDownloadLabel, () =>
            {
                boxDownloadLabel.Text = (download * 8 / 1000) + " kb/s";
                if (download < 7500000)
                {
                    boxDownloadLabel.ForeColor = success;
                }
                else if (upload < 9000000)
                {
                    boxDownloadLabel.ForeColor = warning;
                }
                else
                {
                    boxDownloadLabel.ForeColor = danger;
                }
            });

        }

        // Sets the network labels
        private void setBoxVerifiedGUI(BoxManager boxManager)
        {
            bool verified = !Convert.ToBoolean(boxManager.ThirdParty);
            setBoxVerifiedGUI(verified);
        }
        private void setBoxVerifiedGUI(bool verified)
        {
            modGUI(boxVerifiedLabel, () =>
            {
                boxVerifiedLabel.Text = verified.ToString();
                if (verified)
                {
                    boxVerifiedLabel.ForeColor = success;
                }
                else
                {
                    boxVerifiedLabel.ForeColor = danger;
                }
            });
        }

        // Update region label
        private void setBoxRegionGUI(BoxManager boxManager)
        {
            setBoxRegionGUI(boxManager.Region);
        }
        private void setBoxRegionGUI(string region)
        {
            modGUI(boxRegionLabel, () =>
            {
                if (Runabove.REGION_ID_TO_NAME.ContainsKey(region))
                {
                    boxRegionLabel.Text = Runabove.REGION_ID_TO_NAME[region];
                    boxRegionLabel.ForeColor = success;
                }
                else
                {
                    boxRegionLabel.Text = "None";
                    boxRegionLabel.ForeColor = Color.Black;
                }
            });
        }

        // Refreshes the GameServer list
        private void updateGameServerListGUI(BoxManager boxManager)
        {
            if (boxManager.GameServers != null && boxManager.GameServers.getKeys() != null)
            {
                modGUI(boxGameServerList, () =>
                {
                    boxGameServerList.Items.Clear();
                    foreach (KeyValuePair<string, KV> kvp in boxManager.GameServers.getKeys())
                    {
                        GameServer gameServer = new GameServer(kvp.Value);
                        boxGameServerList.Items.Add(gameServer.Ip);
                    }
                });
            }
        }

        #endregion

        private void boxUpdateButton_Click(object sender, EventArgs e)
        {
            wsServer.send("updateServer", boxesList.SelectedItem.ToString());
        }


    }
}
