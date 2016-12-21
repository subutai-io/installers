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
        public static bool isSilent = false;
        public static bool removeAll = true;
        public static string[] cmd_args = Environment.GetCommandLineArgs();
        public clean()
        {
            InitializeComponent();
            progressBar1.Visible = true;
            label2.Visible = true;
            label1.Text = "Removing Subutai Social";
            if (cmd_args.Length > 1)
                isSilent = defineSilent(cmd_args[1]);
            if (cmd_args.Length > 2)
                removeAll = defineDeleteAll(cmd_args[2]);
        }

        private void clean_Load(object sender, EventArgs e)
        {
            if (!isSilent)
            {
                 DialogResult drs = MessageBox.Show($"Uninstall Subutai Social, are You sure?", "Subutai Social Uninstall",
                 MessageBoxButtons.YesNo,
                 MessageBoxIcon.Question,
                 MessageBoxDefaultButton.Button1);

                if (drs != DialogResult.Yes)
                {
                    Environment.Exit(0);
                }
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

                     StageReporter("", "Removing /home directory link");
                     //Remove /home shortcut
                     mess = FD.remove_from_home(SubutaiDir);
                     mess = FD.remove_home(SubutaiDir);

                     //Remove Subutai dirs from Path
                     //UpdateProgress(55);

                     StageReporter("", "Removing Subutai dirs from %Path%");
                     //Remove Subutai dirs from Path
                     mess = FD.remove_from_Path("Subutai");
                     //Remove %Subutai%
                     Environment.SetEnvironmentVariable("Subutai", "", EnvironmentVariableTarget.Machine);
                     Environment.SetEnvironmentVariable("Subutai", "", EnvironmentVariableTarget.User);
                     Environment.SetEnvironmentVariable("Subutai", "", EnvironmentVariableTarget.Process);
                     //UpdateProgress(60);
                 }, TaskContinuationOptions.OnlyOnRanToCompletion)

                 .ContinueWith((prevTask) =>
                 {
                     StageReporter("", "Cleaning Registry");
                     //Clean registry
                     RG.delete_from_reg();
                     //UpdateProgress(70);
                 }, TaskContinuationOptions.OnlyOnRanToCompletion)

                 .ContinueWith((prevTask) =>
                 {
                     StageReporter("", "Removing TAP interfaces");
                     AP.del_TAP();
                     //UpdateProgress(80);
                 }, TaskContinuationOptions.OnlyOnRanToCompletion)

                .ContinueWith((prevTask) =>
                {
                    StageReporter("", "Removing old logs");
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
                    if (!isSilent) {
                        VBx.remove_app_vbox_short("Oracle VirtualBox");
                    }
                }, TaskContinuationOptions.OnlyOnRanToCompletion)

                .ContinueWith((prevTask) =>
                {
                    //Remove service if was installed during cancelling
                    mess = SCP.stop_process("p2p");
                    mess = "";
                    mess = SCP.stop_service("Subutai Social P2P", 5000);
                    mess = "";
                    mess = SCP.remove_service("Subutai Social P2P");

                }, TaskContinuationOptions.OnlyOnRanToCompletion)

                .ContinueWith((prevTask) =>
                {
                    StageReporter("", "Removing Subutai shortcuts");
                    FD.delete_Shortcuts("Subutai");
                    //UpdateProgress(80);
                    StageReporter("", "Removing Subutai directories");
                    mess = "";
                    string mesg = "";
                    if (SubutaiDir != "" && SubutaiDir != null && SubutaiDir != "C:\\" && SubutaiDir != "D:\\" && SubutaiDir != "E:\\" && !(SubutaiDir.Length < 4))
                    {
                        if (!isSilent)
                        {
                            DialogResult drs = MessageBox.Show($"Remove folder {SubutaiDir}? (Do not remove if going to install again)", "Subutai uninstall",
                                            MessageBoxButtons.YesNo,
                                            MessageBoxIcon.Question,
                                            MessageBoxDefaultButton.Button1);

                            if (drs == DialogResult.Yes)
                            {
                                mess = FD.delete_dir(SubutaiDir);
                            } else
                            {
                                //bin should be deleted in any case
                                mess = FD.delete_dir_bin(SubutaiDir);
                            }
                        }
                        else
                        {
                            if (removeAll)
                            {
                                mess = FD.delete_dir(SubutaiDir);
                            }
                            else
                            {
                                mess = FD.delete_dir_bin(SubutaiDir);
                            }
                        }
                        if (mess.Contains("Can not"))
                        {
                            mesg = string.Format("Folder {0}\\bin can not be removed.\n\n Please close running applications that can lock files (ssh sessions, file manager windows, stop p2p service if running etc) and delete it manually", SubutaiDir);
                            MessageBox.Show(mesg, "Removing Subutai folder", MessageBoxButtons.OK);
                        }

                        //Remove Subutai dir from ApplicationData
                        string appUserDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                        //MessageBox.Show($"AppData: {appUserDir}", "AppData", MessageBoxButtons.OK);
                        appUserDir = Path.Combine(appUserDir, "Subutai Social");
                        if (Directory.Exists(appUserDir))
                        {
                            Directory.Delete(appUserDir, true);
                        }
                    }

                    SetIndeterminate(false);
                    UpdateProgress(100);
                    StageReporter("", "Finished");
                    mesg = string.Format("Subutai Social uninstalled. \n\nPlease delete Oracle VirtualBox and Google Chrome software manually from Control Panel if You are not going to use it");
                    MessageBox.Show(mesg, "Uninstall Subutai Social", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    Environment.Exit(0);
                });
        }

        /// <summary>
        /// Defines if uninstall should be silent.
        /// </summary>
        /// <param name="arg">The arg1.</param>
        /// <returns></returns>
        private bool defineSilent(string arg)
        {
            if (arg.ToLower().Contains("silent"))
            {
                return true;
            }

            if (arg.ToLower().Contains("noall"))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Defines if uninstall should delete all from installation directory.
        /// </summary>
        /// <param name="arg">The arg1.</param>
        /// <returns></returns>
        private bool defineDeleteAll(string arg)
        {
            if (arg.ToLower().Contains("no"))
            {
                return false;
            }
            return true;
        }
    }
}
