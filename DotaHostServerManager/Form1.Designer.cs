namespace DotaHostServerManager
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.boxesList = new System.Windows.Forms.ListBox();
            this.label1 = new System.Windows.Forms.Label();
            this.boxNameLabel = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.boxStatusLabel = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.boxRAMBar = new System.Windows.Forms.ProgressBar();
            this.boxRAMLabel = new System.Windows.Forms.Label();
            this.boxCPULabel = new System.Windows.Forms.Label();
            this.boxCPUBar = new System.Windows.Forms.ProgressBar();
            this.label6 = new System.Windows.Forms.Label();
            this.listBox1 = new System.Windows.Forms.ListBox();
            this.label5 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // boxesList
            // 
            this.boxesList.FormattingEnabled = true;
            this.boxesList.Items.AddRange(new object[] {
            "box1",
            "box2"});
            this.boxesList.Location = new System.Drawing.Point(9, 36);
            this.boxesList.Margin = new System.Windows.Forms.Padding(2);
            this.boxesList.Name = "boxesList";
            this.boxesList.Size = new System.Drawing.Size(108, 316);
            this.boxesList.TabIndex = 0;
            this.boxesList.SelectedIndexChanged += new System.EventHandler(this.boxesList_SelectedIndexChanged);
            // 
            // label1
            // 
            this.label1.Font = new System.Drawing.Font("Arial", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(9, 7);
            this.label1.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(106, 26);
            this.label1.TabIndex = 1;
            this.label1.Text = "Boxes";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // boxNameLabel
            // 
            this.boxNameLabel.Font = new System.Drawing.Font("Arial", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.boxNameLabel.Location = new System.Drawing.Point(231, 36);
            this.boxNameLabel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.boxNameLabel.Name = "boxNameLabel";
            this.boxNameLabel.Size = new System.Drawing.Size(53, 26);
            this.boxNameLabel.TabIndex = 2;
            this.boxNameLabel.Text = "box1";
            this.boxNameLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label3
            // 
            this.label3.Font = new System.Drawing.Font("Arial", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(121, 36);
            this.label3.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(106, 26);
            this.label3.TabIndex = 4;
            this.label3.Text = "Selected Box:";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label4
            // 
            this.label4.Font = new System.Drawing.Font("Arial", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.Location = new System.Drawing.Point(121, 62);
            this.label4.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(106, 26);
            this.label4.TabIndex = 5;
            this.label4.Text = "Status:";
            this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // boxStatusLabel
            // 
            this.boxStatusLabel.Font = new System.Drawing.Font("Arial", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.boxStatusLabel.Location = new System.Drawing.Point(231, 62);
            this.boxStatusLabel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.boxStatusLabel.Name = "boxStatusLabel";
            this.boxStatusLabel.Size = new System.Drawing.Size(53, 26);
            this.boxStatusLabel.TabIndex = 6;
            this.boxStatusLabel.Text = "Active!";
            this.boxStatusLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label2
            // 
            this.label2.Font = new System.Drawing.Font("Arial", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(340, 113);
            this.label2.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(63, 26);
            this.label2.TabIndex = 7;
            this.label2.Text = "RAM:";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // boxRAMBar
            // 
            this.boxRAMBar.Enabled = false;
            this.boxRAMBar.Location = new System.Drawing.Point(408, 113);
            this.boxRAMBar.Name = "boxRAMBar";
            this.boxRAMBar.Size = new System.Drawing.Size(111, 26);
            this.boxRAMBar.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            this.boxRAMBar.TabIndex = 9;
            // 
            // boxRAMLabel
            // 
            this.boxRAMLabel.Font = new System.Drawing.Font("Arial", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.boxRAMLabel.Location = new System.Drawing.Point(408, 142);
            this.boxRAMLabel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.boxRAMLabel.Name = "boxRAMLabel";
            this.boxRAMLabel.Size = new System.Drawing.Size(111, 26);
            this.boxRAMLabel.TabIndex = 10;
            this.boxRAMLabel.Text = "38 / 3090";
            this.boxRAMLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // boxCPULabel
            // 
            this.boxCPULabel.Font = new System.Drawing.Font("Arial", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.boxCPULabel.Location = new System.Drawing.Point(411, 200);
            this.boxCPULabel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.boxCPULabel.Name = "boxCPULabel";
            this.boxCPULabel.Size = new System.Drawing.Size(111, 26);
            this.boxCPULabel.TabIndex = 13;
            this.boxCPULabel.Text = "40%";
            this.boxCPULabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // boxCPUBar
            // 
            this.boxCPUBar.Location = new System.Drawing.Point(411, 171);
            this.boxCPUBar.Name = "boxCPUBar";
            this.boxCPUBar.Size = new System.Drawing.Size(111, 26);
            this.boxCPUBar.TabIndex = 12;
            // 
            // label6
            // 
            this.label6.Font = new System.Drawing.Font("Arial", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label6.Location = new System.Drawing.Point(343, 171);
            this.label6.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(63, 26);
            this.label6.TabIndex = 11;
            this.label6.Text = "CPU:";
            this.label6.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // listBox1
            // 
            this.listBox1.FormattingEnabled = true;
            this.listBox1.Location = new System.Drawing.Point(572, 75);
            this.listBox1.Name = "listBox1";
            this.listBox1.Size = new System.Drawing.Size(108, 277);
            this.listBox1.TabIndex = 14;
            // 
            // label5
            // 
            this.label5.Font = new System.Drawing.Font("Arial", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label5.Location = new System.Drawing.Point(569, 36);
            this.label5.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(108, 26);
            this.label5.TabIndex = 15;
            this.label5.Text = "Game Servers";
            this.label5.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(841, 369);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.listBox1);
            this.Controls.Add(this.boxCPULabel);
            this.Controls.Add(this.boxCPUBar);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.boxRAMLabel);
            this.Controls.Add(this.boxRAMBar);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.boxStatusLabel);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.boxNameLabel);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.boxesList);
            this.Margin = new System.Windows.Forms.Padding(2);
            this.Name = "Form1";
            this.Text = "DostHost Control Panel";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.Shown += new System.EventHandler(this.Form1_Shown);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ListBox boxesList;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label boxNameLabel;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label boxStatusLabel;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ProgressBar boxRAMBar;
        private System.Windows.Forms.Label boxRAMLabel;
        private System.Windows.Forms.Label boxCPULabel;
        private System.Windows.Forms.ProgressBar boxCPUBar;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.ListBox listBox1;
        private System.Windows.Forms.Label label5;
    }
}

