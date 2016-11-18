using System;
using System.Windows.Forms;
using System.IO;
using System.Threading.Tasks;


namespace uninstall_clean
{
    public partial class clean : Form
    {
        //private static NLog.logger logger = LogManager.GetCurrentClasslogger();
        public static string sysDrive = "";
        public static string  SubutaiDir = AP.get_env_var("Subutai");
        public clean()
        {
            InitializeComponent();
            progressBar1.Visible = true;
            label2.Visible = true;
            label1.Text = "Removing Subutai Social";
        }

        private void clean_Load(object sender, EventArgs e)
        {
            DialogResult drs = MessageBox.Show($"Uninstall Subutai Social, are You sure?", "Subutai Social Uninstall",
                 MessageBoxButtons.YesNo,
                 MessageBoxIcon.Question,
                 MessageBoxDefaultButton.Button1);

            if (drs != DialogResult.Yes)
            {
                Environment.Exit(0);
            }

            string sysPath = Environment.GetFolderPath(Environment.SpecialFolder.System);
            sysDrive = Path.GetPathRoot(sysPath);
            SubutaiDir = AP.get_env_var("Subutai");
            runCleaning();
            //clean_all();
         }

        void timer1_Tick(object sender, EventArgs e)
        {
            if (progressBar1.Value != 10)
            {
                progressBar1.Value++;
            }
            else
            {
                timer1.Stop();
            }
        }

        public static void StageReporter(string stageName, string subStageName)
        {
            Program.form1.Invoke((MethodInvoker)delegate
            {
                if (stageName != "")
                {
                    Program.form1.label1.Text = stageName;
                }
                if (subStageName != "")
                {
                    Program.form1.label2.Text = subStageName;
                }
            });
        }

        public static void UpdateProgress(int progress)
        {
            if (Program.form1.progressBar1.InvokeRequired)
            {
                Program.form1.progressBar1.BeginInvoke(
                    new Action(() =>
                    {
                        Program.form1.progressBar1.Value = progress;
                    }
                ));
            }
            else
            {
                Program.form1.progressBar1.Value = progress;
            }
        }

        public static void SetIndeterminate(bool isIndeterminate)
        {
            if (Program.form1.progressBar1.InvokeRequired)
            {
                Program.form1.progressBar1.BeginInvoke(
                    new Action(() =>
                    {
                        if (isIndeterminate)
                        {
                            Program.form1.progressBar1.Style = ProgressBarStyle.Marquee;
                        }
                        else
                        {
                            Program.form1.progressBar1.Style = ProgressBarStyle.Blocks;
                        }
                    }
                ));
            }
            else
            {
                if (isIndeterminate)
                {
                    Program.form1.progressBar1.Style = ProgressBarStyle.Marquee;
                }
                else
                {
                    Program.form1.progressBar1.Style = ProgressBarStyle.Blocks;
                }
            }
        }

        private void runCleaning()
        {
            string mess = "";
            SetIndeterminate(false);
            UpdateProgress(0);
            Task.Factory.StartNew(() =>
            {
                StageReporter("", "Starting uninstall");
                SetIndeterminate(true);
            })

              .ContinueWith((prevTask) =>
               {
                   Exception ex = prevTask.Exception;
                   if (prevTask.IsFaulted)
                   {
                       //prepare-vbox faulted with exception
                       while (ex is AggregateException && ex.InnerException != null)
                       {
                           ex = ex.InnerException;
                       }
                       MessageBox.Show(ex.Message, "Start", MessageBoxButtons.OK);
                       //throw new InvalidOperationException();
                   }
                   StageReporter("", "Removing firewall rules");
                   SetIndeterminate(true);
                   SCP.remove_fw_rules(SubutaiDir);
                   //UpdateProgress(10);
               })

                 .ContinueWith((prevTask) =>
                 {
                     StageReporter("", "Removing Subutai Social P2P service");
                     mess = SCP.stop_process("p2p");
                     mess = "";
                     mess = SCP.stop_service("Subutai Social P2P", 5000);
                     mess = "";
                     mess = SCP.remove_service("Subutai Social P2P");
                     //UpdateProgress(20);
                     StageReporter("", "Stopping SubutaiTray processes");
                     mess = SCP.stop_process("SubutaiTray");
                     //UpdateProgress(30);

                 }, TaskContinuationOptions.OnlyOnRanToCompletion)


                 .ContinueWith((prevTask) =>
                 {
                     StageReporter("", "Removing Subutai shortcuts");
                     FD.delete_Shortcuts("Subutai");
                     //UpdateProgress(40);
                     StageReporter("", "Removing Subutai directories");
                     if (SubutaiDir != "" && SubutaiDir != null && SubutaiDir != "C:\\" && SubutaiDir != "D:\\" && SubutaiDir != "E:\\")
                     {
                         DialogResult drs = MessageBox.Show($"Remove folder {SubutaiDir}? (Do not remove if going to install again)", "Subutai uninstall",
                                         MessageBoxButtons.YesNo,
                                         MessageBoxIcon.Question,
                                         MessageBoxDefaultButton.Button1);
                         mess = "";
                         if (drs == DialogResult.Yes)
                         {
                             mess = FD.delete_dir(SubutaiDir);
                         }

                         if (mess.Contains("Can not"))
                         {
                             MessageBox.Show($"Folder {SubutaiDir} can not be removed. Please delete it manually",
                                 "Removing Subutai folder", MessageBoxButtons.OK);
                         }
                     }

                     //Remove Subutai dir from ApplicationData
                     string appUserDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                     //MessageBox.Show($"AppData: {appUserDir}", "AppData", MessageBoxButtons.OK);
                     appUserDir = Path.Combine(appUserDir, "Subutai Social");
                     if (Directory.Exists(appUserDir))
                     {
                         Directory.Delete(appUserDir, true);
                     }
                     //UpdateProgress(50);
                 }, TaskContinuationOptions.OnlyOnRanToCompletion)

                 .ContinueWith((prevTask) =>
                 {

                     StageReporter("", "Removing /home directory link");
                     //Remove / home shortcut
                     mess = FD.remove_from_home(SubutaiDir);
                     mess = FD.remove_home(SubutaiDir);

                     //Remove Subutai dirs from Path
                     // UpdateProgress(60);

                     StageReporter("", "Removing Subutai dirs from %Path%");
                     //Remove Subutai dirs from Path
                     mess = FD.remove_from_Path("Subutai");
                     //Remove %Subutai%
                     Environment.SetEnvironmentVariable("Subutai", "", EnvironmentVariableTarget.Machine);
                     Environment.SetEnvironmentVariable("Subutai", "", EnvironmentVariableTarget.User);
                     Environment.SetEnvironmentVariable("Subutai", "", EnvironmentVariableTarget.Process);
                     //UpdateProgress(70);
                 }, TaskContinuationOptions.OnlyOnRanToCompletion)

                 .ContinueWith((prevTask) =>
                 {
                     StageReporter("", "Cleaning Registry");
                     //Clean registry
                     RG.delete_from_reg();
                     //UpdateProgress(80);
                 }, TaskContinuationOptions.OnlyOnRanToCompletion)

                 .ContinueWith((prevTask) =>
                 {
                     StageReporter("", "Removing TAP interfaces");
                     AP.del_TAP();
                     UpdateProgress(90);
                 }, TaskContinuationOptions.OnlyOnRanToCompletion)

              .ContinueWith((prevTask) =>
              {
                  StageReporter("","Removing old logs");
                  //Remove log dir
                  FD.remove_log_dir();

              }, TaskContinuationOptions.OnlyOnRanToCompletion)

              //.ContinueWith((prevTask) =>
              //{
              //     StageReporter("", "Removing Google Chrome");
              //     //Remove log dir
              //     AP.remove_chrome();

              // }, TaskContinuationOptions.OnlyOnRanToCompletion)

              .ContinueWith((prevTask) =>
              {
                  StageReporter("", "Removing Subutai Virtual Machines");
                  //Remove snappy and subutai machines
                  VBx.remove_vm();
                  //Remove Oracle VirtualBox
                  StageReporter("", "Removing Oracle Virtual Box software");
                  VBx.remove_app_vbox_short("Oracle VirtualBox");


                  SetIndeterminate(false);
                  UpdateProgress(100);
                  StageReporter("", "Finished");
                  MessageBox.Show("Subutai Social uninstalled", "Information", MessageBoxButtons.OK);
                  Environment.Exit(0);
               }, TaskContinuationOptions.OnlyOnRanToCompletion);
        }

 
    private void progressBar1_Click(object sender, EventArgs e)
        {

        }
    }
}
