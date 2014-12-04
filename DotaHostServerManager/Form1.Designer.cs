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
            this.boxGameServerList = new System.Windows.Forms.ListBox();
            this.label5 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.boxUploadLabel = new System.Windows.Forms.Label();
            this.boxDownloadLabel = new System.Windows.Forms.Label();
            this.button1 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.boxVerifiedLabel = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.boxRegionLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // boxesList
            // 
            this.boxesList.FormattingEnabled = true;
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
            this.boxNameLabel.Size = new System.Drawing.Size(136, 26);
            this.boxNameLabel.TabIndex = 2;
            this.boxNameLabel.Text = "333.333.333:33333";
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
            this.boxStatusLabel.Size = new System.Drawing.Size(136, 26);
            this.boxStatusLabel.TabIndex = 6;
            this.boxStatusLabel.Text = "Active!";
            this.boxStatusLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label2
            // 
            this.label2.Font = new System.Drawing.Font("Arial", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(533, 49);
            this.label2.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(90, 26);
            this.label2.TabIndex = 7;
            this.label2.Text = "RAM:";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // boxRAMBar
            // 
            this.boxRAMBar.Enabled = false;
            this.boxRAMBar.Location = new System.Drawing.Point(628, 49);
            this.boxRAMBar.Name = "boxRAMBar";
            this.boxRAMBar.Size = new System.Drawing.Size(108, 26);
            this.boxRAMBar.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            this.boxRAMBar.TabIndex = 9;
            // 
            // boxRAMLabel
            // 
            this.boxRAMLabel.Font = new System.Drawing.Font("Arial", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.boxRAMLabel.Location = new System.Drawing.Point(628, 78);
            this.boxRAMLabel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.boxRAMLabel.Name = "boxRAMLabel";
            this.boxRAMLabel.Size = new System.Drawing.Size(108, 26);
            this.boxRAMLabel.TabIndex = 10;
            this.boxRAMLabel.Text = "38 / 3090";
            this.boxRAMLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // boxCPULabel
            // 
            this.boxCPULabel.Font = new System.Drawing.Font("Arial", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.boxCPULabel.Location = new System.Drawing.Point(628, 136);
            this.boxCPULabel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.boxCPULabel.Name = "boxCPULabel";
            this.boxCPULabel.Size = new System.Drawing.Size(108, 26);
            this.boxCPULabel.TabIndex = 13;
            this.boxCPULabel.Text = "40%";
            this.boxCPULabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // boxCPUBar
            // 
            this.boxCPUBar.Location = new System.Drawing.Point(628, 107);
            this.boxCPUBar.Name = "boxCPUBar";
            this.boxCPUBar.Size = new System.Drawing.Size(108, 26);
            this.boxCPUBar.TabIndex = 12;
            // 
            // label6
            // 
            this.label6.Font = new System.Drawing.Font("Arial", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label6.Location = new System.Drawing.Point(533, 107);
            this.label6.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(90, 26);
            this.label6.TabIndex = 11;
            this.label6.Text = "CPU:";
            this.label6.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // boxGameServerList
            // 
            this.boxGameServerList.BackColor = System.Drawing.SystemColors.Window;
            this.boxGameServerList.FormattingEnabled = true;
            this.boxGameServerList.Location = new System.Drawing.Point(842, 75);
            this.boxGameServerList.Name = "boxGameServerList";
            this.boxGameServerList.Size = new System.Drawing.Size(108, 277);
            this.boxGameServerList.TabIndex = 14;
            // 
            // label5
            // 
            this.label5.Font = new System.Drawing.Font("Arial", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label5.Location = new System.Drawing.Point(839, 36);
            this.label5.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(108, 26);
            this.label5.TabIndex = 15;
            this.label5.Text = "Game Servers";
            this.label5.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label7
            // 
            this.label7.Font = new System.Drawing.Font("Arial", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label7.Location = new System.Drawing.Point(533, 192);
            this.label7.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(90, 26);
            this.label7.TabIndex = 16;
            this.label7.Text = "Upload:";
            this.label7.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label8
            // 
            this.label8.Font = new System.Drawing.Font("Arial", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label8.Location = new System.Drawing.Point(533, 218);
            this.label8.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(90, 26);
            this.label8.TabIndex = 17;
            this.label8.Text = "Download:";
            this.label8.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // boxUploadLabel
            // 
            this.boxUploadLabel.Font = new System.Drawing.Font("Arial", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.boxUploadLabel.Location = new System.Drawing.Point(628, 192);
            this.boxUploadLabel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.boxUploadLabel.Name = "boxUploadLabel";
            this.boxUploadLabel.Size = new System.Drawing.Size(108, 26);
            this.boxUploadLabel.TabIndex = 18;
            this.boxUploadLabel.Text = "1823 kb/s";
            this.boxUploadLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // boxDownloadLabel
            // 
            this.boxDownloadLabel.Font = new System.Drawing.Font("Arial", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.boxDownloadLabel.Location = new System.Drawing.Point(628, 218);
            this.boxDownloadLabel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.boxDownloadLabel.Name = "boxDownloadLabel";
            this.boxDownloadLabel.Size = new System.Drawing.Size(108, 26);
            this.boxDownloadLabel.TabIndex = 19;
            this.boxDownloadLabel.Text = "1823 kb/s";
            this.boxDownloadLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(142, 282);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(112, 52);
            this.button1.TabIndex = 20;
            this.button1.Text = "Create BoxManager";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(157, 177);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(112, 75);
            this.button2.TabIndex = 21;
            this.button2.Text = "Delete Selected Box Manager";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // boxVerifiedLabel
            // 
            this.boxVerifiedLabel.Font = new System.Drawing.Font("Arial", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.boxVerifiedLabel.Location = new System.Drawing.Point(231, 88);
            this.boxVerifiedLabel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.boxVerifiedLabel.Name = "boxVerifiedLabel";
            this.boxVerifiedLabel.Size = new System.Drawing.Size(136, 26);
            this.boxVerifiedLabel.TabIndex = 23;
            this.boxVerifiedLabel.Text = "False";
            this.boxVerifiedLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label10
            // 
            this.label10.Font = new System.Drawing.Font("Arial", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label10.Location = new System.Drawing.Point(121, 88);
            this.label10.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(106, 26);
            this.label10.TabIndex = 22;
            this.label10.Text = "Verified:";
            this.label10.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label9
            // 
            this.label9.Font = new System.Drawing.Font("Arial", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label9.Location = new System.Drawing.Point(121, 114);
            this.label9.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(106, 26);
            this.label9.TabIndex = 24;
            this.label9.Text = "Region:";
            this.label9.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // boxRegionLabel
            // 
            this.boxRegionLabel.Font = new System.Drawing.Font("Arial", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.boxRegionLabel.Location = new System.Drawing.Point(231, 114);
            this.boxRegionLabel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.boxRegionLabel.Name = "boxRegionLabel";
            this.boxRegionLabel.Size = new System.Drawing.Size(136, 26);
            this.boxRegionLabel.TabIndex = 25;
            this.boxRegionLabel.Text = "None";
            this.boxRegionLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1016, 565);
            this.Controls.Add(this.boxRegionLabel);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.boxVerifiedLabel);
            this.Controls.Add(this.label10);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.boxDownloadLabel);
            this.Controls.Add(this.boxUploadLabel);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.boxGameServerList);
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
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.Form1_FormClosed);
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
        private System.Windows.Forms.ListBox boxGameServerList;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label boxUploadLabel;
        private System.Windows.Forms.Label boxDownloadLabel;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Label boxVerifiedLabel;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label boxRegionLabel;
    }
}

