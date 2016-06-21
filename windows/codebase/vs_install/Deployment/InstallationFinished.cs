using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Deployment
{
    public partial class InstallationFinished : DevExpress.XtraEditors.XtraForm
    {
        bool opacityChanging = false;

        public InstallationFinished(string status)
        {
            InitializeComponent();
            label3.Text = status;
            if (status.Contains("failed"))
            {
                label4.Visible = true;
                //Deploy.LaunchCommandLineApp("", "");
            }
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

            Program.form1.Invoke((MethodInvoker) delegate
            {
                Program.form1.Close();
            });
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