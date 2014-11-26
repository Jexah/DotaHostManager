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
        private static Color error = Color.Red;

        // Initialize boxManagers dictionary
        private static Dictionary<string, BoxManager> boxManagers = new Dictionary<string, BoxManager>();

        // Create WebSocketServer
        private static WebSocketServer wsServer = new WebSocketServer(IPAddress.Parse(Global.SERVER_MANAGER_IP), Global.SERVER_MANAGER_PORT);

        public Form1()
        {
            InitializeComponent();

            // Hook socket events
            hookWSocketServerEvents();

            // Start the websocket server, wait for incomming connections
            wsServer.start();
        }

         // Socket hooks go here
        private static void hookWSocketServerEvents()
        {
            // Print received messages to console for debugging
            wsServer.addHook(WebSocketClient.RECEIVE, (c) =>
            {
                Helpers.log(c.DataFrame.ToString());
            });

            // When a server is started, it sends box function, so this tells the servermanager "Hey, there's a new box in town" and the server manager does it's things to accomodate
            wsServer.addHook("box", (c, x) => { 
                BoxManager boxManager = new BoxManager();
                boxManager.setIP(c.ClientAddress.ToString());
                boxManagers.Add(c.ClientAddress.ToString(), boxManager);
                Helpers.log("IP: " + c.ClientAddress.ToString());
                c.Send("system");
            });

            // Receives the system status from the box manager, loops
            wsServer.addHook("system", (c, x) =>
            {
                Helpers.log("Update thingo");
                BoxManager boxManager = boxManagers[c.ClientAddress.ToString()];
                boxManager.setStatus(Convert.ToByte(x[1]));
                boxManager.setCpuPercent(Convert.ToByte(x[2]));
                boxManager.setRam(new short[] { Convert.ToInt16(x[3]), Convert.ToInt16(x[4]) });
                boxManager.setNetwork(new int[] { Convert.ToInt32(x[5]), Convert.ToInt32(x[6]), Convert.ToInt32(x[7]) });
                DotaHostLibrary.Timer.newTimer(10, DotaHostLibrary.Timer.SECONDS, () => { c.Send("system"); });
            });
        }

        // Create a new box instance using snapshot
        private static void addBoxManager()
        {
            // TODO: Code to start up new box, box will then contact this server once it's started.
        }

        // Destroy box instance
        private static void removeBoxManager(BoxManager boxManager)
        {
            // TODO: Code to destroy box server
            boxManagers.Remove(boxManager.getIP());
        }

        // Finds a server to host the gamemode selected, in the region selected
        private static void findServer(byte region, string addonID)
        {
            // TODO: Add server finding algorithm
        }

        // Reboots the selected box
        private static void restartBox(BoxManager boxManager)
        {
            // TODO: Add box restart code here
        }

        private void boxesList_SelectedIndexChanged(object sender, EventArgs e)
        {
            boxNameLabel.Text = boxesList.SelectedItem.ToString();
        }
        
        private void Form1_Load(object sender, EventArgs e)
        {


            boxesList.SelectedIndex = 0;
            setCurrentBoxAsActiveGUI();
            setCurrentBoxRAMGUI(2800, 3080);
            setCurrentBoxCPUGUI(78);
        }

        
        private void Form1_Shown(object sender, EventArgs e)
        {

        }



        private void setCurrentBoxAsActiveGUI()
        {
            boxStatusLabel.Text = "Active!";
            boxStatusLabel.ForeColor = success;
        }
        private void setCurrentBoxAsMIAGUI()
        {
            boxStatusLabel.Text = "MIA";
            boxStatusLabel.ForeColor = warning;
        }
        private void setCurrentBoxRAMGUI(int current, int total)
        {
            boxRAMBar.Maximum = total;
            boxRAMBar.Value = current;
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
                boxRAMLabel.ForeColor = error;
            }
        }
        private void setCurrentBoxCPUGUI(int percent)
        {
            boxCPUBar.Value = percent;
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
                boxCPULabel.ForeColor = error;
            }
        }




    }
}
