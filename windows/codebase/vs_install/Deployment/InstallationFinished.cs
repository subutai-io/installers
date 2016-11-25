using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
     

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
            label3.Text = status;
            if (status.Contains("failed"))
            {
                label4.Visible = true;
            }

            if (status.Contains("failed") || status.Contains("cancelled"))
            {
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

        private void InstallationFinished_Load(object sender, EventArgs e)
        {
 
        }

        /// <summary>
        /// Changes the opacity of "finished" message.
        /// </summary>
        private void ChangeOpacity()
        {
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
                this.Close();
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