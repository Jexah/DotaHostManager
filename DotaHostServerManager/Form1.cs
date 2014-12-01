using DotaHostClientLibrary;
using DotaHostLibrary;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Net;
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
        private static KV boxManagers = new KV();

        // Initialize addonRequirements dictionary
        private static Dictionary<string, AddonRequirements> addonRequirements = new Dictionary<string,AddonRequirements>();

        // Create WebSocketServer
        private static WebSocketServer wsServer = new WebSocketServer(IPAddress.Any, Vultr.SERVER_MANAGER_PORT);

        // Soft BoxManager limit
        private static byte serverSoftCap;

        // Hard BoxManager limit
        private const byte SERVER_HARD_CAP = 5;


        public Form1()
        {
            setAddonRequirements();

            serverSoftCap = 1;

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
                Helpers.log(c.DataFrame.ToString());
            });
            #endregion

            // When a server is started, it sends box function, so this tells the servermanager "Hey, there's a new box in town" and the server manager does it's things to accomodate
            #region wsServer.addHook("box");
            wsServer.addHook("box", (c, x) =>
            {
                // Initialize the new BoxManager
                KV boxManager = new KV();

                // Get a list of servers
                Vultr.getServers((jsonObj) =>
                {
                    // Used to contain the info generated from the API
                    VultrServerProperties serverInfo = new VultrServerProperties();

                    // Finds the box manager in the list of servers by matching the IPs
                    foreach (KeyValuePair<string, VultrServerProperties> kvp in jsonObj)
                    {
                        string serverIP = kvp.Value.main_ip;
                        string boxIP = c.ClientAddress.ToString().Split(':')[0];
                        if (serverIP == boxIP)
                        {
                            serverInfo = kvp.Value;
                            boxManager.addValue("thirdParty", false.ToString());
                        }
                    }
                    if (!Convert.ToBoolean(boxManager.getValue("thirdParty")))
                    {
                        // Sets the subID so it can be destroyed later
                        boxManager.addValue("subID", serverInfo.SUBID);

                        // Sets the region so we know where it is hosted
                        boxManager.addValue("region", Vultr.NAME_TO_REGION_ID[serverInfo.location].ToString());

                        // Send SUBID to server so it knows its place
                        c.Send("subid;" + boxManager.getValue("subID"));
                    }
                });

                // Sets the IP of the box manager
                boxManager.addValue("ip", c.ClientAddress.ToString());

                // Add the box manager to the list
                boxManagers.addKey(boxManager.getValue("ip"), boxManager);

                modGUI(boxesList, () =>
                {
                    if (boxesList.SelectedItem != null && boxesList.SelectedItem.ToString() == boxManager.getValue("ip"))
                    {
                        setBoxStatsGUI(boxManager);
                    }
                });
                

                // Request system stats
                c.Send("system");

            });
            #endregion

            // Receives the system status from the box manager, loops
            #region wsServer.addhook("system");
            wsServer.addHook("system", (c, x) =>
            {
                if(!boxManagers.containsKey(c.ClientAddress.ToString()))
                {
                    return;
                }

                // Create pointer to box manager
                KV boxManager = boxManagers.getKV(c.ClientAddress.ToString());

                // Refresh all stats of server
                boxManager = KV.parse(String.Join("", Helpers.RemoveIndex(x, 0)));

                // Request GUI-safe thread
                modGUI(boxesList, () =>
                {
                    // Add BoxManager to the GUI List if it's not there
                    if(!boxesList.Items.Contains(c.ClientAddress.ToString()))
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
                boxManagers.removeKey(c.ClientAddress.ToString());

                // Update listbox
                modGUI(boxesList, () => { boxesList.Items.Remove(c.ClientAddress.ToString()); });

            });
            #endregion

            // Receive game server request from webserver
            #region wsServer.addHook("create");
            wsServer.addHook("create", (c, x) =>
            {

                byte region = Convert.ToByte(x[1]);

                KV lobby = KV.parse(x[2]);

                KV boxManager = findBoxManager(region, lobby.getKV("addons"));

                if(boxManager != null)
                {
                    wsServer.send(String.Join(";", x), boxManager.getValue("ip"));
                }
                else
                {
                    Helpers.log("Could not find server");
                }
            });
            #endregion

            // Receive game server list  ======================= [WIP]
            #region wsServer.addHook("gameServers");
            wsServer.addHook("gameServers", (c, x) =>
            {
                string boxesListSelectedItem = "";
                modGUI(boxesList, () =>
                {
                    if(boxesList.SelectedItem != null)
                    {
                        boxesListSelectedItem = boxesList.SelectedItem.ToString();
                    }
                    if (boxesListSelectedItem == c.ClientAddress.ToString())
                    {
                        KV servers = new KV();
                        string[] serverStrings = Helpers.RemoveIndex(x, 0);
                        for (int i = 0; i < serverStrings.Length; ++i)
                        {
                            KV server = new KV();
                            server.addValue("name", serverStrings[i]);
                            modGUI(boxGameServerList, () =>
                            {
                                boxGameServerList.Enabled = true;
                                boxGameServerList.Items.Clear();
                                boxGameServerList.Items.Add(server.getValue("name"));
                            });
                        }
                    }
                });
                
            });
            #endregion

        }

        // All BoxManager/Gameserver related events here
        #region BoxManager/GameServer related events

        // Create a new box instance using snapshot
        private static void addBoxManager(byte region)
        {
            Vultr.getServers((jsonObj) =>
            {
                if (jsonObj.Count >= SERVER_HARD_CAP)
                {
                    Helpers.log("SERVER HARD CAP REACHED: SERVER NOT CREATED");
                }
                else if (jsonObj.Count >= serverSoftCap)
                {
                    Helpers.log("SERVER SOFT CAP REACHED: SERVER NOT CREATED");
                }
                else
                {
                    Vultr.createServer(region);
                }
            });
        }

        // Destroy box instance
        private void removeBoxManager(KV boxManager, bool now = false)
        {
            string ip = boxManager.getValue("ip");
            if (!Convert.ToBoolean(boxManager.getValue("thirdParty")))
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
            boxManagers.removeKey(ip);

            // Update the listbox
            modGUI(boxesList, () => { boxesList.Items.Remove(ip); });
        }

        // Finds a server to host the gamemode selected, in the region selected
        private KV findBoxManager(byte region, KV addons)
        {
            int totalRam = 0;
            int totalCpu = 0;
            foreach (KeyValuePair<string, KV> addon in addons.getKeys())
            {
                totalRam += addonRequirements[addon.Value.getValue("id")].Ram;
                totalCpu += addonRequirements[addon.Value.getValue("id")].Cpu;
            }
            foreach (KeyValuePair<string, KV> kvp in boxManagers.getKeys())
            {
                if (kvp.Value.getValue("region") == region.ToString())
                {
                    Helpers.log("region OK");
                    if (Convert.ToInt16(kvp.Value.getValue("ram").Split(' ')[0]) > totalRam && 100 - Convert.ToByte(kvp.Value.getValue("cpuPercent")) > totalCpu)
                    {
                        Helpers.log("stats OK");
                        return kvp.Value;
                    }
                }
            }
            return null;
        }

        // Reboots the selected box
        private void restartBox(KV boxManager)
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
            if(selectedItem != null && selectedItem.ToString() != "")
            {
                // Set the current visible stats to those of the box manager
                string boxIP = boxesList.SelectedItem.ToString();
                setBoxStatsGUI(boxManagers.getKV("boxIP"));
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
            addBoxManager(Vultr.AUSTRALIA);
        }

        // Temporary button, destroys the selected server
        private void button2_Click(object sender, EventArgs e)
        {
            removeBoxManager(boxManagers.getKV(boxesList.SelectedItem.ToString()));
        }

        #endregion

        // All pure GUI updating code goes here
        #region UPDATE GUI

        // Set the default values for the elements of the GUI
        private void setBoxDefaultGUI()
        {
            setBoxNameGUI("None");
            setBoxStatusGUI(Vultr.BOX_INACTIVE);
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
        private void setBoxStatsGUI(KV boxManager)
        {
            setBoxNameGUI(boxManager);
            setBoxStatusGUI(boxManager);
            setBoxCPUGUI(boxManager);
            setBoxRAMGUI(boxManager);
            setBoxNetworkGUI(boxManager);
            setBoxVerifiedGUI(boxManager);
        }

        // Sets the name label to that of the name of the given name
        private void setBoxNameGUI(KV boxManager)
        {
            setBoxNameGUI(boxManager.getValue("name"));
        }
        private void setBoxNameGUI(string name)
        {
            modGUI(boxNameLabel, () => { boxNameLabel.Text = name; });
        }

        // Sets the value and color of the status label to that of the status of the given status
        private void setBoxStatusGUI(KV boxManager)
        {
            setBoxStatusGUI(Convert.ToByte(boxManager.getValue("status")));
        }
        private void setBoxStatusGUI(byte status)
        {
            modGUI(boxStatusLabel, () =>
            {
                switch (status)
                {
                    case Vultr.BOX_ACTIVE:
                        boxStatusLabel.Text = "Active";
                        boxStatusLabel.ForeColor = success;
                        break;
                    case Vultr.BOX_MIA:
                        boxStatusLabel.Text = "MIA";
                        boxStatusLabel.ForeColor = warning;
                        break;
                    case Vultr.BOX_IDLE:
                        boxStatusLabel.Text = "Idle";
                        boxStatusLabel.ForeColor = success;
                        break;
                    case Vultr.BOX_INACTIVE:
                        boxStatusLabel.Text = "Inactive";
                        boxStatusLabel.ForeColor = danger;
                        break;
                    case Vultr.BOX_DEACTIVATED:
                        boxStatusLabel.Text = "Deactivated";
                        boxStatusLabel.ForeColor = warning;
                        break;
                }
            });
        }

        // Sets ram labels color and value to that of the given ram levels
        private void setBoxRAMGUI(KV boxManager)
        {
            string[] split = boxManager.getValue("ram").Split(' ');
            short[] ram = new short[2];
            ram[0] = Convert.ToInt16(split[0]);
            ram[1] = Convert.ToInt16(split[1]);
            setBoxRAMGUI(ram[0], ram[1]);
        }
        private void setBoxRAMGUI(short remaining, short total)
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
        private void setBoxCPUGUI(KV boxManager)
        {
            setBoxCPUGUI(Convert.ToByte(boxManager.getValue("cpuPercent")));
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
        private void setBoxNetworkGUI(KV boxManager)
        {
            string[] split = boxManager.getValue("network").Split(' ');
            int[] network = new int[2];
            network[0] = Convert.ToInt32(split[0]);
            network[1] = Convert.ToInt32(split[1]);
            setBoxNetworkGUI(network[0], network[1]);
        }
        private void setBoxNetworkGUI(int upload, int download)
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
        private void setBoxVerifiedGUI(KV boxManager)
        {
            bool verified = !Convert.ToBoolean(boxManager.getValue("thirdParty"));
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

        #endregion


    }
}
