using DotaHostLibrary;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net;
using System.Windows.Forms;

namespace DotaHostServerManager
{
    public partial class Form1 : Form
    {
        private static Color success = Color.Green;
        private static Color warning = Color.Orange;
        private static Color danger = Color.Red;

        // Initialize boxManagers dictionary
        private static Dictionary<string, BoxManager> boxManagers = new Dictionary<string, BoxManager>();

        // Create WebSocketServer
        private static WebSocketServer wsServer = new WebSocketServer(IPAddress.Parse(Global.SERVER_MANAGER_IP), Global.SERVER_MANAGER_PORT);

        public Form1()
        {
            InitializeComponent();

            HTTPRequestManager.startRequest("https://api.vultr.com/v1/regions/list", "GET", (r) =>
            {
                Console.WriteLine(r["6"]["name"]);
            });

            Console.ReadLine();

            // Hook socket events
            hookWSocketServerEvents();

            // Start the websocket server, wait for incomming connections
            wsServer.start();
        }

         // Socket hooks go here
        private void hookWSocketServerEvents()
        {
            // Print received messages to console for debugging
            #region wsServer.addHook
            wsServer.addHook(WebSocketClient.RECEIVE, (c) =>
            {
                // Helpers.log(c.DataFrame.ToString());
            });
            #endregion

            // When a server is started, it sends box function, so this tells the servermanager "Hey, there's a new box in town" and the server manager does it's things to accomodate
            #region wsServer.addHook("box");
            wsServer.addHook("box", (c, x) => { 

                // Create the BoxManager object and append it to the BoxManagers list
                BoxManager boxManager = new BoxManager();
                boxManager.setIP(c.ClientAddress.ToString());
                boxManagers.Add(c.ClientAddress.ToString(), boxManager);

                // Add BoxManager to the GUI List
                modGUI(boxesList, () => { boxesList.Items.Add(c.ClientAddress.ToString()); });
                
                c.Send("system");
            });
            #endregion


            #region wsServer.addhook("system");
            // Receives the system status from the box manager, loops
            wsServer.addHook("system", (c, x) =>
            {
                BoxManager boxManager = boxManagers[c.ClientAddress.ToString()];
                boxManager.setStatus(Convert.ToByte(x[1]));
                boxManager.setCpuPercent(Convert.ToByte(x[2]));
                boxManager.setRam(new short[] { Convert.ToInt16(x[3]), Convert.ToInt16(x[4]) });
                boxManager.setNetwork(new int[] { Convert.ToInt32(x[5]), Convert.ToInt32(x[6]), Convert.ToInt32(x[7]) });

                modGUI(boxesList, () =>
                {
                    if (boxesList.SelectedItem != null && boxesList.SelectedItem.ToString() == c.ClientAddress.ToString())
                    {
                        setCurrentBoxStatsGUI(boxManager);
                    }
                });

                DotaHostLibrary.Timer.newTimer(1, DotaHostLibrary.Timer.SECONDS, () => { c.Send("system"); });
            });
            #endregion


            #region wsServer.addHook(WebSocketServer.DISCONNECTED);
            wsServer.addHook(WebSocketServer.DISCONNECTED, (c) =>
            {
                boxManagers.Remove(c.ClientAddress.ToString());
                modGUI(boxesList, () => { boxesList.Items.Remove(c.ClientAddress.ToString()); });

            });
            #endregion


        }

        // Create a new box instance using snapshot
        private void addBoxManager()
        {
            // TODO: Code to start up new box, box will then contact this server once it's started.
            System.Diagnostics.Process.Start("DotaHostBoxManager.exe");
        }

        // Destroy box instance
        private void removeBoxManager(BoxManager boxManager)
        {
            // TODO: Code to destroy box server
            boxManagers.Remove(boxManager.getIP());
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

        private void boxesList_SelectedIndexChanged(object sender, EventArgs e)
        {
            object selectedItem = boxesList.SelectedItem;
            if(selectedItem != null && selectedItem.ToString() != "")
            {
                string boxIP = boxesList.SelectedItem.ToString();
                setCurrentBoxStatsGUI(boxManagers[boxIP]);
            }
            else
            {
                if (boxesList.Items.Count > 0)
                {
                    boxesList.SelectedIndex = 0;
                }
                else
                {
                    setBoxDefaultGUI();
                }
            }
        }
        
        private void Form1_Load(object sender, EventArgs e)
        {
            setBoxDefaultGUI();
        }

        private void Form1_Shown(object sender, EventArgs e)
        {

        }

        private void setBoxDefaultGUI()
        {
            setBoxNameGUI("None");
            setBoxStatusGUI(BoxManager.INACTIVE);
            setBoxRAMGUI(0, 0);
            setBoxCPUGUI(0);
            setBoxNetworkGUI(0, 0, 0);
        }

        private void modGUI(Control o, Action a)
        {
            o.BeginInvoke(new MethodInvoker(delegate { a(); }));
        }

        private void setCurrentBoxStatsGUI(BoxManager boxManager)
        {
            setCurrentBoxNameGUI(boxManager);
            setCurrentBoxStatusGUI(boxManager);
            setCurrentBoxCPUGUI(boxManager);
            setCurrentBoxRAMGUI(boxManager);
            setCurrentBoxNetworkGUI(boxManager);
        }

        private void setCurrentBoxNameGUI(BoxManager boxManager)
        {
            setBoxNameGUI(boxManager.getIP());
        }

        private void setBoxNameGUI(string name)
        {
            modGUI(boxNameLabel, () => { boxNameLabel.Text = name; });
        }

        private void setCurrentBoxStatusGUI(BoxManager boxManager)
        {
            setBoxStatusGUI(boxManager.getStatus());
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

        private void setCurrentBoxRAMGUI(BoxManager boxManager)
        {
            short[] ram = boxManager.getRam();
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

        private void setCurrentBoxCPUGUI(BoxManager boxManager)
        {
            setBoxCPUGUI(boxManager.getCpuPercent());
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

        private void setCurrentBoxNetworkGUI(BoxManager boxManager)
        {
            int[] network = boxManager.getNetwork();
            setBoxNetworkGUI(network[0], network[1], network[2]);
        }

        private void setBoxNetworkGUI(int bandwidth, int upload, int download)
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

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            Environment.Exit(0);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            addBoxManager();
        }




    }
}
