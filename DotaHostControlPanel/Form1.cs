using DotaHostLibrary;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace DotaHostControlPanel
{
    public partial class Form1 : Form
    {
        private static Color success = Color.Green;
        private static Color warning = Color.Orange;
        private static Color error = Color.Red;

        private static WebSocketClient wsClient = new WebSocketClient("ws://" + Global.SERVER_MANAGER_IP + ":" + Global.SERVER_MANAGER_PORT + "/");

        public Form1()
        {
            InitializeComponent();
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

        private static void connectTo
        
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
