namespace uninstall_clean
{
    partial class clean
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(clean));
            this.label1 = new System.Windows.Forms.Label();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.label2 = new System.Windows.Forms.Label();
            this.panel1 = new System.Windows.Forms.Panel();
            this.label6 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.lbTAP = new System.Windows.Forms.Label();
            this.btnUninstall = new System.Windows.Forms.Button();
            this.cbxFolder = new System.Windows.Forms.CheckBox();
            this.label3 = new System.Windows.Forms.Label();
            this.cbxChrome = new System.Windows.Forms.CheckBox();
            this.cbxTAP = new System.Windows.Forms.CheckBox();
            this.cbxVBox = new System.Windows.Forms.CheckBox();
            this.panel2 = new System.Windows.Forms.Panel();
            this.panel1.SuspendLayout();
            this.panel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(5, 6);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(102, 15);
            this.label1.TabIndex = 0;
            this.label1.Text = "Uninstall Subutai";
            // 
            // progressBar1
            // 
            this.progressBar1.ForeColor = System.Drawing.SystemColors.HotTrack;
            this.progressBar1.Location = new System.Drawing.Point(7, 24);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(241, 22);
            this.progressBar1.TabIndex = 1;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(254, 28);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(100, 15);
            this.label2.TabIndex = 2;
            this.label2.Text = "Sub-stage name";
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.label6);
            this.panel1.Controls.Add(this.label5);
            this.panel1.Controls.Add(this.label4);
            this.panel1.Controls.Add(this.lbTAP);
            this.panel1.Controls.Add(this.btnUninstall);
            this.panel1.Controls.Add(this.cbxFolder);
            this.panel1.Controls.Add(this.label3);
            this.panel1.Controls.Add(this.cbxChrome);
            this.panel1.Controls.Add(this.cbxTAP);
            this.panel1.Controls.Add(this.cbxVBox);
            this.panel1.Location = new System.Drawing.Point(7, 2);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(572, 140);
            this.panel1.TabIndex = 11;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Font = new System.Drawing.Font("Arial", 8.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.label6.Location = new System.Drawing.Point(262, 47);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(221, 15);
            this.label6.TabIndex = 13;
            this.label6.Text = "It\'s better to delete it from Control Panel";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("Arial", 8.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.label5.Location = new System.Drawing.Point(263, 90);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(288, 15);
            this.label5.TabIndex = 12;
            this.label5.Text = "Remove Chrome if it is not present in Control Panel";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Arial", 8.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.label4.Location = new System.Drawing.Point(23, 91);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(219, 15);
            this.label4.TabIndex = 11;
            this.label4.Text = "Do not remove  if  going to install again";
            // 
            // lbTAP
            // 
            this.lbTAP.AutoSize = true;
            this.lbTAP.Font = new System.Drawing.Font("Arial", 8.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.lbTAP.Location = new System.Drawing.Point(23, 47);
            this.lbTAP.Name = "lbTAP";
            this.lbTAP.Size = new System.Drawing.Size(193, 15);
            this.lbTAP.TabIndex = 10;
            this.lbTAP.Text = "Do not remove  if  using OpenVPN";
            // 
            // btnUninstall
            // 
            this.btnUninstall.Location = new System.Drawing.Point(447, 110);
            this.btnUninstall.Name = "btnUninstall";
            this.btnUninstall.Size = new System.Drawing.Size(116, 23);
            this.btnUninstall.TabIndex = 9;
            this.btnUninstall.Text = "Uninstall";
            this.btnUninstall.UseVisualStyleBackColor = true;
            this.btnUninstall.Click += new System.EventHandler(this.btnUninstall_Click);
            // 
            // cbxFolder
            // 
            this.cbxFolder.AutoSize = true;
            this.cbxFolder.Checked = true;
            this.cbxFolder.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbxFolder.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.cbxFolder.Location = new System.Drawing.Point(6, 74);
            this.cbxFolder.Name = "cbxFolder";
            this.cbxFolder.Size = new System.Drawing.Size(170, 19);
            this.cbxFolder.TabIndex = 4;
            this.cbxFolder.Text = "Subutai Installation folder";
            this.cbxFolder.UseVisualStyleBackColor = true;
            this.cbxFolder.CheckedChanged += new System.EventHandler(this.cbxFolder_CheckedChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Arial", 11F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.label3.Location = new System.Drawing.Point(3, 7);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(238, 18);
            this.label3.TabIndex = 0;
            this.label3.Text = "Choose components to remove: ";
            // 
            // cbxChrome
            // 
            this.cbxChrome.AutoSize = true;
            this.cbxChrome.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.cbxChrome.Location = new System.Drawing.Point(246, 74);
            this.cbxChrome.Name = "cbxChrome";
            this.cbxChrome.Size = new System.Drawing.Size(168, 19);
            this.cbxChrome.TabIndex = 3;
            this.cbxChrome.Text = "Google Chrome Software";
            this.cbxChrome.UseVisualStyleBackColor = true;
            this.cbxChrome.CheckedChanged += new System.EventHandler(this.cbxChrome_CheckedChanged);
            // 
            // cbxTAP
            // 
            this.cbxTAP.AutoSize = true;
            this.cbxTAP.Checked = true;
            this.cbxTAP.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbxTAP.FlatAppearance.CheckedBackColor = System.Drawing.Color.AliceBlue;
            this.cbxTAP.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.cbxTAP.Location = new System.Drawing.Point(6, 33);
            this.cbxTAP.Name = "cbxTAP";
            this.cbxTAP.Size = new System.Drawing.Size(158, 19);
            this.cbxTAP.TabIndex = 1;
            this.cbxTAP.Text = "TAP Windows software";
            this.cbxTAP.UseVisualStyleBackColor = true;
            this.cbxTAP.CheckedChanged += new System.EventHandler(this.cbxTAP_CheckedChanged);
            // 
            // cbxVBox
            // 
            this.cbxVBox.AutoSize = true;
            this.cbxVBox.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.cbxVBox.Location = new System.Drawing.Point(246, 33);
            this.cbxVBox.Name = "cbxVBox";
            this.cbxVBox.Size = new System.Drawing.Size(183, 19);
            this.cbxVBox.TabIndex = 2;
            this.cbxVBox.Text = "Oracle Virtual Box software";
            this.cbxVBox.UseVisualStyleBackColor = true;
            this.cbxVBox.CheckedChanged += new System.EventHandler(this.cbxVBox_CheckedChanged);
            // 
            // panel2
            // 
            this.panel2.BackColor = System.Drawing.SystemColors.Window;
            this.panel2.Controls.Add(this.progressBar1);
            this.panel2.Controls.Add(this.label2);
            this.panel2.Controls.Add(this.label1);
            this.panel2.Enabled = false;
            this.panel2.Location = new System.Drawing.Point(7, 78);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(572, 52);
            this.panel2.TabIndex = 12;
            this.panel2.Visible = false;
            // 
            // clean
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Window;
            this.ClientSize = new System.Drawing.Size(584, 146);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.panel1);
            this.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "clean";
            this.Text = "Subutai Social Uninstall";
            this.Load += new System.EventHandler(this.clean_Load);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.panel2.ResumeLayout(false);
            this.panel2.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ProgressBar progressBar1;
        private System.Windows.Forms.Timer timer1;
        internal System.Windows.Forms.Label label2;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button btnUninstall;
        private System.Windows.Forms.CheckBox cbxFolder;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.CheckBox cbxChrome;
        private System.Windows.Forms.CheckBox cbxTAP;
        private System.Windows.Forms.CheckBox cbxVBox;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Label lbTAP;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
    }
}