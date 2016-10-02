namespace Deployment
{
    partial class f_install
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(f_install));
            this.label_Stage = new System.Windows.Forms.Label();
            this.prBar_ = new System.Windows.Forms.ProgressBar();
            this.label_SubStage = new System.Windows.Forms.Label();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.SuspendLayout();
            // 
            // label_Stage
            // 
            this.label_Stage.AutoSize = true;
            this.label_Stage.Font = new System.Drawing.Font("Arial", 10.2F);
            this.label_Stage.ForeColor = System.Drawing.Color.WhiteSmoke;
            this.label_Stage.Location = new System.Drawing.Point(14, 6);
            this.label_Stage.Name = "label_Stage";
            this.label_Stage.Size = new System.Drawing.Size(98, 19);
            this.label_Stage.TabIndex = 0;
            this.label_Stage.Text = "Stage Name";
            // 
            // prBar_
            // 
            this.prBar_.Location = new System.Drawing.Point(16, 31);
            this.prBar_.Name = "prBar_";
            this.prBar_.Size = new System.Drawing.Size(241, 17);
            this.prBar_.TabIndex = 1;
            // 
            // label_SubStage
            // 
            this.label_SubStage.AutoSize = true;
            this.label_SubStage.Font = new System.Drawing.Font("Arial", 9F);
            this.label_SubStage.ForeColor = System.Drawing.Color.WhiteSmoke;
            this.label_SubStage.Location = new System.Drawing.Point(263, 30);
            this.label_SubStage.Name = "label_SubStage";
            this.label_SubStage.Size = new System.Drawing.Size(119, 17);
            this.label_SubStage.TabIndex = 2;
            this.label_SubStage.Text = "Sub Stage Name";
            // 
            // f_install
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(88)))), ((int)(((byte)(88)))), ((int)(((byte)(88)))));
            this.ClientSize = new System.Drawing.Size(610, 56);
            this.Controls.Add(this.label_SubStage);
            this.Controls.Add(this.prBar_);
            this.Controls.Add(this.label_Stage);
            this.Font = new System.Drawing.Font("Arial", 7.8F, System.Drawing.FontStyle.Bold);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "f_install";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.Text = "Initialization of Subutai Social";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.f_install_FormClosing);
            this.Load += new System.EventHandler(this.f_install_Load);
            this.VisibleChanged += new System.EventHandler(this.f_install_VisibleChanged);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        public System.Windows.Forms.Label label_Stage;
        public System.Windows.Forms.ProgressBar prBar_;
        public System.Windows.Forms.Label label_SubStage;
        public System.Windows.Forms.Timer timer1;
    }
}