﻿using DotaHostClientLibrary;
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
        private static Dictionary<string, BoxManager> boxManagers = new Dictionary<string, BoxManager>();

        // Create WebSocketServer
        private static WebSocketServer wsServer = new WebSocketServer(IPAddress.Any, Vultr.SERVER_MANAGER_PORT);

        // Soft BoxManager limit
        private static byte serverSoftCap;

        // Hard BoxManager limit
        private const byte SERVER_HARD_CAP = 5;


        public Form1()
        {
            serverSoftCap = 1;

            InitializeComponent();

            // Hook socket events
            hookWSocketServerEvents();

            // Start the websocket server, wait for incomming connections
            wsServer.start();
        }

         // Socket hooks go here
        private void hookWSocketServerEvents()
        {
            // Print received messages to console for debugging
            #region wsServer.addHook(WebSocketServer.RECEIVE);
            wsServer.addHook(WebSocketServer.RECEIVE, (c) =>
            {
                //MessageBox.Show(c.DataFrame.ToString());
            });
            #endregion

            // When a server is started, it sends box function, so this tells the servermanager "Hey, there's a new box in town" and the server manager does it's things to accomodate
            #region wsServer.addHook("box");
            wsServer.addHook("box", (c, x) =>
            {
                // Get a list of servers
                Vultr.getServers((jsonObj) =>
                {
                    // Initialize the new BoxManager
                    BoxManager boxManager = new BoxManager();

                    // Used to contain the info generated from the API
                    VultrServerProperties serverInfo = new VultrServerProperties();

                    // Flag to check if server was found, if not SHIT IF WROOONG
                    bool found = false;

                    // Finds the box manager in the list of servers by matching the IPs
                    foreach (KeyValuePair<string, VultrServerProperties> kvp in jsonObj)
                    {
                        string serverIP = kvp.Value.main_ip;
                        string boxIP = c.ClientAddress.ToString().Split(':')[0];
                        if (serverIP == boxIP)
                        {
                            serverInfo = kvp.Value;
                            found = true;
                        }
                    }
                    if (!found)
                    {
                        Helpers.log("Server IP was not found.");
                        return;
                    }

                    // Sets the subID so it can be destroyed later
                    boxManager.SubID = Convert.ToInt32(serverInfo.SUBID);

                    // Sets the region so we know where it is hosted
                    boxManager.Region = Vultr.NAME_TO_REGION_ID[serverInfo.location];

                    // Sets the IP of the box manager
                    boxManager.Ip = c.ClientAddress.ToString();

                    // Add the box manager to the list
                    boxManagers.Add(c.ClientAddress.ToString(), boxManager);

                    // Add BoxManager to the GUI List
                    modGUI(boxesList, () => { boxesList.Items.Add(c.ClientAddress.ToString()); });

                    // Send SUBID to server so it knows its place
                    c.Send("subid;" + boxManager.SubID);

                    // Request system stats
                    c.Send("system");
                });
            });
            #endregion

            // Receives the system status from the box manager, loops
            #region wsServer.addhook("system");
            wsServer.addHook("system", (c, x) =>
            {
                if(!boxManagers.ContainsKey(c.ClientAddress.ToString()))
                {
                    return;
                }

                // Create pointer to box manager
                BoxManager boxManager = boxManagers[c.ClientAddress.ToString()];

                // Refresh all stats of server
                boxManager.Status = Convert.ToByte(x[1]);
                boxManager.CpuPercent = Convert.ToByte(x[2]);
                boxManager.Ram = new short[] { Convert.ToInt16(x[3]), Convert.ToInt16(x[4]) };
                boxManager.Network = new int[] { Convert.ToInt32(x[5]), Convert.ToInt32(x[6])};

                // Request GUI-safe thread
                modGUI(boxesList, () =>
                {
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
                boxManagers.Remove(c.ClientAddress.ToString());

                // Update listbox
                modGUI(boxesList, () => { boxesList.Items.Remove(c.ClientAddress.ToString()); });

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
        private void removeBoxManager(BoxManager boxManager, bool now = false)
        {
            if (now)
            {
                // Sends instant destroy message
                wsServer.send("destroy|hard", boxManager.Ip);
            }
            else
            {
                // Waits for current games to finish, polls every minute
                wsServer.send("destroy", boxManager.Ip);
            }

            // Remove boxmanager from list
            boxManagers.Remove(boxManager.Ip);

            // Update the listbox
            modGUI(boxesList, () => { boxesList.Items.Remove(boxManager.Ip); });
        }

        // Finds a server to host the gamemode selected, in the region selected
        private void findServer(byte region, string addonID)
        {
            // TODO: Add server finding algorithm
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
            // Stores selected item
            object selectedItem = boxesList.SelectedItem;

            // If the selected item is actually something
            if(selectedItem != null && selectedItem.ToString() != "")
            {
                // Set the current visible stats to those of the box manager
                string boxIP = boxesList.SelectedItem.ToString();
                setBoxStatsGUI(boxManagers[boxIP]);
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
            removeBoxManager(boxManagers[boxesList.SelectedItem.ToString()]);
        }

        #endregion

        // All pure GUI updating code goes here
        #region UPDATE GUI

        // Set the default values for the elements of the GUI
        private void setBoxDefaultGUI()
        {
            setBoxNameGUI("None");
            setBoxStatusGUI(BoxManager.INACTIVE);
            setBoxRAMGUI(0, 0);
            setBoxCPUGUI(0);
            setBoxNetworkGUI(0, 0);
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
            switch (status)
            {
                case BoxManager.ACTIVE:
                    modGUI(boxStatusLabel, () => { boxStatusLabel.Text = "Active"; });
                    boxStatusLabel.ForeColor = success;
                    break;
                case BoxManager.MIA:
                    modGUI(boxStatusLabel, () => { boxStatusLabel.Text = "MIA"; });
                    boxStatusLabel.ForeColor = warning;
                    break;
                case BoxManager.IDLE:
                    modGUI(boxStatusLabel, () => { boxStatusLabel.Text = "Idle"; });
                    boxStatusLabel.ForeColor = success;
                    break;
                case BoxManager.INACTIVE:
                    modGUI(boxStatusLabel, () => { boxStatusLabel.Text = "Inactive"; });
                    boxStatusLabel.ForeColor = danger;
                    break;
                case BoxManager.DEACTIVATED:
                    modGUI(boxStatusLabel, () => { boxStatusLabel.Text = "Deactivated"; });
                    boxStatusLabel.ForeColor = warning;
                    break;
            }
        }

        // Sets ram labels color and value to that of the given ram levels
        private void setBoxRAMGUI(BoxManager boxManager)
        {
            short[] ram = boxManager.Ram;
            setBoxRAMGUI(ram[0], ram[1]);
        }
        private void setBoxRAMGUI(short remaining, short total)
        {
            short current = (short)(total - remaining);
            modGUI(boxRAMBar, () => { boxRAMBar.Maximum = total; });
            modGUI(boxRAMBar, () => { boxRAMBar.Value = current; });
            modGUI(boxRAMLabel, () => { boxRAMLabel.Text = current + " / " + total; });
            float percent = (float)current / (float)total;
            if (percent < 0.75)
            {
                modGUI(boxRAMLabel, () => { boxRAMLabel.ForeColor = success; });
            }
            else if (percent < 0.9)
            {
                modGUI(boxRAMLabel, () => { boxRAMLabel.ForeColor = warning; });
            }
            else
            {
                modGUI(boxRAMLabel, () => { boxRAMLabel.ForeColor = danger; });
            }
        }

        // Sets cpu label and bar to that of the % cpu usage given
        private void setBoxCPUGUI(BoxManager boxManager)
        {
            setBoxCPUGUI(boxManager.CpuPercent);
        }
        private void setBoxCPUGUI(int percent)
        {
            modGUI(boxCPUBar, () => { boxCPUBar.Value = percent; });
            modGUI(boxCPULabel, () => { boxCPULabel.Text = percent + "%"; });
            if (percent < 75)
            {
                modGUI(boxCPULabel, () => { boxCPULabel.ForeColor = success; });
            }
            else if (percent < 90)
            {
                modGUI(boxCPULabel, () => { boxCPULabel.ForeColor = warning; });
            }
            else
            {
                modGUI(boxCPULabel, () => { boxCPULabel.ForeColor = danger; });
            }
        }

        // Sets the network labels
        private void setBoxNetworkGUI(BoxManager boxManager)
        {
            int[] network = boxManager.Network;
            setBoxNetworkGUI(network[0], network[1]);
        }
        private void setBoxNetworkGUI(int upload, int download)
        {
            modGUI(boxUploadLabel, () => { boxUploadLabel.Text = (upload * 8 / 1000) + " kb/s"; });
            modGUI(boxDownloadLabel, () => { boxDownloadLabel.Text = (download * 8 / 1000) + " kb/s"; });
            if (upload < 7500000)
            {
                modGUI(boxUploadLabel, () => { boxUploadLabel.ForeColor = success; });
            }
            else if (upload < 9000000)
            {
                modGUI(boxUploadLabel, () => { boxUploadLabel.ForeColor = warning; });
            }
            else
            {
                modGUI(boxUploadLabel, () => { boxUploadLabel.ForeColor = danger; });
            }
            if (download < 7500000)
            {
                modGUI(boxDownloadLabel, () => { boxDownloadLabel.ForeColor = success; });
            }
            else if (upload < 9000000)
            {
                modGUI(boxDownloadLabel, () => { boxDownloadLabel.ForeColor = warning; });
            }
            else
            {
                modGUI(boxDownloadLabel, () => { boxDownloadLabel.ForeColor = danger; });
            }
        }

        #endregion


    }
}
