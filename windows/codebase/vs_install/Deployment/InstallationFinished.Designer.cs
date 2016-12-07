namespace Deployment
{
    partial class InstallationFinished
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(InstallationFinished));
            this.lbSS = new System.Windows.Forms.Label();
            this.lbInstallation = new System.Windows.Forms.Label();
            this.lbFinished = new System.Windows.Forms.Label();
            this.lbPlease = new System.Windows.Forms.Label();
            this.pbLogo = new System.Windows.Forms.PictureBox();
            this.lbReason = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.pbLogo)).BeginInit();
            this.SuspendLayout();
            // 
            // lbSS
            // 
            this.lbSS.AutoSize = true;
            this.lbSS.Font = new System.Drawing.Font("Tahoma", 12F);
            this.lbSS.ForeColor = System.Drawing.Color.WhiteSmoke;
            this.lbSS.Location = new System.Drawing.Point(277, 7);
            this.lbSS.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lbSS.Name = "lbSS";
            this.lbSS.Size = new System.Drawing.Size(108, 19);
            this.lbSS.TabIndex = 3;
            this.lbSS.Text = "Subutai Social";
            // 
            // lbInstallation
            // 
            this.lbInstallation.AutoSize = true;
            this.lbInstallation.Font = new System.Drawing.Font("Tahoma", 20F);
            this.lbInstallation.ForeColor = System.Drawing.Color.WhiteSmoke;
            this.lbInstallation.Location = new System.Drawing.Point(121, 47);
            this.lbInstallation.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lbInstallation.Name = "lbInstallation";
            this.lbInstallation.Size = new System.Drawing.Size(146, 33);
            this.lbInstallation.TabIndex = 4;
            this.lbInstallation.Text = "Installation";
            this.lbInstallation.Click += new System.EventHandler(this.label2_Click);
            // 
            // lbFinished
            // 
            this.lbFinished.AutoSize = true;
            this.lbFinished.Font = new System.Drawing.Font("Tahoma", 20F);
            this.lbFinished.ForeColor = System.Drawing.Color.WhiteSmoke;
            this.lbFinished.Location = new System.Drawing.Point(265, 47);
            this.lbFinished.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lbFinished.Name = "lbFinished";
            this.lbFinished.Size = new System.Drawing.Size(107, 33);
            this.lbFinished.TabIndex = 5;
            this.lbFinished.Text = "finished";
            // 
            // lbPlease
            // 
            this.lbPlease.AutoSize = true;
            this.lbPlease.Font = new System.Drawing.Font("Tahoma", 11F);
            this.lbPlease.ForeColor = System.Drawing.Color.WhiteSmoke;
            this.lbPlease.Location = new System.Drawing.Point(126, 100);
            this.lbPlease.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lbPlease.Name = "lbPlease";
            this.lbPlease.Size = new System.Drawing.Size(144, 18);
            this.lbPlease.TabIndex = 6;
            this.lbPlease.Text = "Please try again later";
            this.lbPlease.Visible = false;
            // 
            // pbLogo
            // 
            this.pbLogo.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.pbLogo.Enabled = false;
            this.pbLogo.Image = global::Deployment.Properties.Resources.Subutai_logo_4_Dark;
            this.pbLogo.Location = new System.Drawing.Point(11, 10);
            this.pbLogo.Margin = new System.Windows.Forms.Padding(2);
            this.pbLogo.Name = "pbLogo";
            this.pbLogo.Size = new System.Drawing.Size(106, 108);
            this.pbLogo.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pbLogo.TabIndex = 7;
            this.pbLogo.TabStop = false;
            // 
            // lbReason
            // 
            this.lbReason.AutoSize = true;
            this.lbReason.ForeColor = System.Drawing.Color.WhiteSmoke;
            this.lbReason.Location = new System.Drawing.Point(126, 80);
            this.lbReason.MaximumSize = new System.Drawing.Size(290, 0);
            this.lbReason.Name = "lbReason";
            this.lbReason.Size = new System.Drawing.Size(72, 13);
            this.lbReason.TabIndex = 8;
            this.lbReason.Text = "Reason of fail";
            this.lbReason.Visible = false;
            // 
            // InstallationFinished
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(88)))), ((int)(((byte)(88)))), ((int)(((byte)(88)))));
            this.ClientSize = new System.Drawing.Size(404, 126);
            this.Controls.Add(this.lbReason);
            this.Controls.Add(this.pbLogo);
            this.Controls.Add(this.lbPlease);
            this.Controls.Add(this.lbFinished);
            this.Controls.Add(this.lbInstallation);
            this.Controls.Add(this.lbSS);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            this.Name = "InstallationFinished";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "InstallationFinished";
            this.TopMost = true;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.InstallationFinished_FormClosing);
            this.Load += new System.EventHandler(this.InstallationFinished_Load);
            this.Shown += new System.EventHandler(this.InstallationFinished_Shown);
            this.Click += new System.EventHandler(this.InstallationFinished_Click);
            this.MouseHover += new System.EventHandler(this.InstallationFinished_MouseHover);
            ((System.ComponentModel.ISupportInitialize)(this.pbLogo)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        //private DevExpress.XtraEditors.Controls.ImageSlider imageSlider1;
        private System.Windows.Forms.Label lbSS;
        private System.Windows.Forms.Label lbInstallation;
        private System.Windows.Forms.Label lbFinished;
        private System.Windows.Forms.Label lbPlease;
        private System.Windows.Forms.PictureBox pbLogo;
        private System.Windows.Forms.Label lbReason;
    }
}