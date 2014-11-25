using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DotaHostControlPanel
{
    public partial class Form1 : Form
    {
        Color success = Color.Green;
        Color warning = Color.Orange;
        Color error = Color.Red;

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
            Console.WriteLine(percent);
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
