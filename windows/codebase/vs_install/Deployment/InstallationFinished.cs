using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.Drawing;
     

namespace Deployment
{
    public partial class InstallationFinished : Form
    {
        bool opacityChanging = false;
        bool need2clean = false;
        string dir2clean = "";

        /// <summary>
        /// Initializes a new instance of the <see cref="InstallationFinished"/> class.
        /// </summary>
        /// <param name="status">The status (failed, cancelled, success).</param>
        /// <param name="appdir">The application directory.</param>
        public InstallationFinished(string status, string appdir)
        {
            InitializeComponent();
            lbFinished.Text = status;
            int formWidth = this.Size.Width;
            Point xLocation = pbLogo.Location;
            Point yLocation = pbLogo.Location;
            int dxWide = pbLogo.Size.Width;
            int dyHigh = pbLogo.Size.Height;
            int xX = xLocation.X;
            int yY = yLocation.Y;
            int allX = xX + dxWide + 10;
            int allY = yY + dyHigh;

            int delta = 0;

            lbPlease.Location = new System.Drawing.Point(allX + 5, allY - lbPlease.Size.Height);
            if (status.Contains("failed"))
            {
                delta = dyHigh - lbPlease.Size.Height - 15 - lbSS.Size.Height - lbInstallation.Size.Height - 5 - lbReason.Size.Height;
                lbInstallation.Location = new System.Drawing.Point(allX, yY + lbSS.Size.Height + delta);
                lbFinished.Location = new System.Drawing.Point(allX + lbInstallation.Size.Width + 5, yY + lbSS.Size.Height + delta);
                lbReason.Location = new System.Drawing.Point(allX + 5, allY - lbPlease.Size.Height - 15 - lbReason.Size.Height);

                lbReason.Text = Program.st_fail_reason;
                lbReason.Visible = true;
            }

            if (status.Contains("cancelled"))
            {
                delta = dyHigh - lbPlease.Size.Height - 10 - lbSS.Size.Height - lbInstallation.Size.Height;
                lbInstallation.Location = new System.Drawing.Point(allX, yY + lbSS.Size.Height + delta - delta / 3);
                lbFinished.Location = new System.Drawing.Point(allX + lbInstallation.Size.Width + 3, yY + lbSS.Size.Height + delta - delta/3);
            }

            if (status.Contains("failed") || status.Contains("cancelled"))
            {
                lbPlease.Visible = true;
                need2clean = true;
                dir2clean = appdir;
            } 
        }

        /// <summary>
        /// Runs uninstall application
        /// </summary>
        /// <param name="appdir">The appdir - installation folder</param>
        private void clean(string appdir)
        {
            
            Process.Start($"{FD.logDir()}\\{Deploy.SubutaiUninstallName}",  "Silent NoAll");
        }

        /// <summary>
        /// Handles the Load event of the InstallationFinished control.
        /// Will wait 20000 ms - if opacity changing did not started - will run opacity change
        /// to close form
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs" /> instance containing the event data.</param>
        private void InstallationFinished_Load(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// Handles the Shown event of the InstallationFinished control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void InstallationFinished_Shown(object sender, EventArgs e)
        {
            Application.DoEvents();
            Thread.Sleep(5000);
            Task ts =  Task.Run(() => WaitClose());
        }

        /// <summary>
        /// Waiting for closing - 20 seconds.
        /// If form not closed, will start opacity changing
        /// and close after that.
        /// </summary>
        private void WaitClose()
        {
            //Task ts =  Task.Run(() => waiting_ms());
            //ts.Start();
            Thread.Sleep(20000);
            
            if (!opacityChanging)
            {
                new Task(ChangeOpacity).Start();
                opacityChanging = true;
            }
        }

        /// <summary>
        /// Waits 20000 ms.
        /// </summary>
        private void waiting_ms()
        {
            Thread.Sleep(20000);
        }

        /// <summary>
        /// Changes the opacity of "finished" message.
        /// </summary>
        private void ChangeOpacity()
        {
            opacityChanging = true;
            for (var i = 1.0; i > 0; i -= 0.01)
            {
                this.Invoke((MethodInvoker) delegate
                {
                    this.Opacity = i;
                });
                Thread.Sleep(20);
            }

            if (need2clean && dir2clean!="")
            {
                MessageBox.Show("Installation was interrupted, removing partially installed Subutai Social", "Installation failed", MessageBoxButtons.OK);
                clean(dir2clean);
                Program.form1.Invoke((MethodInvoker)delegate
                {
                    Program.form1.Close();
                });
            }
            if (!need2clean)
            {
                Program.form1.Invoke((MethodInvoker)delegate
                {
                    Program.form1.Close();
                });
            }
        }


        /// <summary>
        /// Handles the Click event of the InstallationFinished control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void InstallationFinished_Click(object sender, EventArgs e)
        {
            if (!opacityChanging)
            {
                new Task(ChangeOpacity).Start();
                opacityChanging = true;
            }
        }

        /// <summary>
        /// Handles the Click event of the label2 control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void label2_Click(object sender, EventArgs e)
        {
            if (!opacityChanging)
            {
                new Task(ChangeOpacity).Start();
                opacityChanging = true;
            }
       }

        /// <summary>
        /// Handles the FormClosing event of the InstallationFinished control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="FormClosingEventArgs"/> instance containing the event data.</param>
        private void InstallationFinished_FormClosing(object sender, FormClosingEventArgs e)
        {
           if (!opacityChanging)
            {
                new Task(ChangeOpacity).Start();
                opacityChanging = true;
            }
        }

        /// <summary>
        /// Handles the MouseHover event of the InstallationFinished control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void InstallationFinished_MouseHover(object sender, EventArgs e)
        {
            if (!opacityChanging)
            {
                new Task(ChangeOpacity).Start();
                opacityChanging = true;
            }
        }
    }
}