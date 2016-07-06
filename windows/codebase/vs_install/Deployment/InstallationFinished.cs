using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Deployment
{
    public partial class InstallationFinished : DevExpress.XtraEditors.XtraForm
    {
        bool opacityChanging = false;
        bool need2clean = false;
        string dir2clean = "";
       
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

        

        private void clean(string appdir)
        {
            string res = "";
            //MessageBox.Show("Cleaning failed installation", "Cleaning",MessageBoxButtons.OK);
            res = Deploy.LaunchCommandLineApp($"{appdir}bin\\uninstall-clean", "");
            //logger.Info("uninstall-clean: {0}", res);
            res = Deploy.LaunchCommandLineApp("regedit.exe", "/s c:\\temp\\subutai-clean-registry.reg");
            //logger.Info("Cleaning registry: {0}", res);

            //try
            //{
            //    Directory.Delete($"{_arguments["appDir"]}", true);
            //    logger.Info("Deleting dir");
            //}
            //catch (Exception ex)
            //{
            //    logger.Error(ex.Message, "Deleting directory");
            //}
        }


        private void InstallationFinished_Load(object sender, EventArgs e)
        {
 
        }

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
                //Program.ShowError("Installation was interrupted, please wait while removing partially installed Subutai Social", "Installation cancelled");
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

            //Application.Exit();    
            //Environment.Exit(0);
 
        }
     

        private void InstallationFinished_Click(object sender, EventArgs e)
        {
            if (!opacityChanging)
            {
                new Task(ChangeOpacity).Start();
                opacityChanging = true;
            }
        }

        private void label2_Click(object sender, EventArgs e)
        {
            if (!opacityChanging)
            {
                new Task(ChangeOpacity).Start();
                opacityChanging = true;
            }
       }

        private void InstallationFinished_FormClosing(object sender, FormClosingEventArgs e)
        {
           if (!opacityChanging)
            {
                new Task(ChangeOpacity).Start();
                opacityChanging = true;
            }
        }

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