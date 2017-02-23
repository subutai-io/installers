using System;
using System.Windows.Forms;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using NLog;


namespace uninstall_clean
{
    /// <summary>
    /// Uninstalling Subutai
    /// </summary>
    /// <seealso cref="System.Windows.Forms.Form" />
    public partial class clean : Form
    {
        private static NLog.Logger logger = LogManager.GetCurrentClassLogger();
        //private static NLog.Logger logger1 = LogManager.GetLogger("logger1");
        private int curWidth = 0;
        private int curHeight = 0;
        private int minWidth = 565;
        private int minHeight = 95;
        private int maxWidth = 600;
        private int maxHeight = 200;
        private Panel curPanel;


        /// <summary>
        /// The system drive
        /// </summary>
        public static string sysDrive = "";
        /// <summary>
        /// The subutai dir
        /// </summary>
        public static string  SubutaiDir = "";
        /// <summary>
        /// If uninstall is silent
        /// </summary>
        public static bool isSilent = false;
        /// <summary>
        /// If all components should be removed
        /// </summary>
        public static bool removeAll = true;
        /// <summary>
        /// The command line arguments
        /// </summary>
        public static string[] cmd_args = Environment.GetCommandLineArgs();
        /// <summary>
        /// If TAP software should be installed
        /// </summary>
        public static bool bTAP = true;
        /// <summary>
        /// If installation folder should be removed 
        /// </summary>
        public static bool bFolder = true;
        /// <summary>
        /// if Oracle VirtualBox should be removed
        /// </summary>
        public static bool bVBox = false;
        /// <summary>
        /// if Google Chrome should be removed
        /// </summary>
        public static bool bChrome = false;
        /// <summary>
        /// if run with logging
        /// </summary>
        public static bool toLog = false;

        public clean()
        {
            InitializeComponent();

            if (cmd_args.Length > 1)
                isSilent = defineSilent(cmd_args[1]);
            if (cmd_args.Length > 2)
                toLog = defineSilent(cmd_args[2]);
            if (cmd_args.Length > 3)
                removeAll = defineDeleteAll(cmd_args[3]);
            if (isSilent)
            {
                curPanel = this.panel1;
                panelChange();
            } 
        }

        private void clean_Load(object sender, EventArgs e)
        {
            string sysPath = Environment.GetFolderPath(Environment.SpecialFolder.System);
            sysDrive = Path.GetPathRoot(sysPath);
            SubutaiDir = AP.get_env_var("Subutai");
            logger.Info("Uninstalling: date = {0}", $"{ DateTime.Now.ToString("yyyyMMddhhmm")}");
            if (toLog)
            {
                logger.Info("Starting to uninstall Subutai with parameters: sysDrive: {0}; SubutaiDir: {1}; isSilent: {2}; removeAll: {3}", sysDrive, SubutaiDir, isSilent, removeAll);
                MessageBox.Show($"sysDrive: {sysDrive}; SubutaiDir: {SubutaiDir}; isSilent: {isSilent}; removeAll: {removeAll}; toLog: {toLog}", "OnLoad", MessageBoxButtons.OK);
            }
            if (isSilent)
                runCleaning();
         }

        private void panelChange()
        {
            if (curPanel == this.panel1)
            {
                curPanel = this.panel2;
                curPanel.Location = new Point(5, 5);

                this.Width = minWidth;
                this.Height = minHeight;
                curWidth = this.Width;
                curHeight = this.Height;

                this.panel2.Enabled = true;
                this.panel2.Visible = true;
                   
                this.panel1.Enabled = false;
                this.panel1.Visible = false;
            }
            else
            {
                curPanel = this.panel1;
                curPanel.Location = new Point(5, 5);

                this.Width = maxWidth;
                this.Height = maxHeight;
                curWidth = this.Width;
                curHeight = this.Height;

                this.panel1.Enabled = true;
                this.panel1.Visible = true;

                this.panel2.Enabled = false;
                this.panel2.Visible = false;
            }
        }

        /// <summary>
        /// Reportin Uninstall stage
        /// </summary>
        /// <param name="stageName">Name of the stage.</param>
        /// <param name="subStageName">Name of the sub stage.</param>
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

        /// <summary>
        /// Updates Uninstall progress
        /// </summary>
        /// <param name="progress">The progress percent</param>
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

        /// <summary>
        /// Sets the progress bar into indeterminate state.
        /// </summary>
        /// <param name="isIndeterminate">if set to <c>true</c> [is indeterminate].</param>
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

        /// <summary>
        /// Task factory actually perfoeming cleaning
        /// </summary>
        private void runCleaning()
        {
            string mess = "";
            SetIndeterminate(false);
            UpdateProgress(0);
            logger.Info("sysDrive: {0}; SubutaiDir: {1}; isSilent: {2}; removeAll: {3}", sysDrive, SubutaiDir, isSilent, removeAll);
            Task.Factory.StartNew(() =>
            {
                StageReporter("", "Starting uninstall");
                if (toLog)
                {
                    logger.Info("sysDrive: {0}; SubutaiDir: {1}; isSilent: {2}; removeAll: {3}", sysDrive, SubutaiDir, isSilent, removeAll);
                    logger.Info("Will delete: TAP software: {0}; Chrome: {1}; VBox: {2}; Subutai Directory: {3}", bTAP, bChrome, bVBox, bFolder);
                    mess = string.Format("Will delete: TAP software: {0}; Chrome: {1}; VBox: {2}; Subutai Directory: {3}", bTAP, bChrome, bVBox, bFolder);
                    MessageBox.Show(mess, "Start", MessageBoxButtons.OK);
                    mess = "";
                }
                SetIndeterminate(true);
            })

              .ContinueWith((prevTask) =>
              {
                  Exception ex = prevTask.Exception;
                  if (prevTask.IsFaulted)
                  {
                      while (ex is AggregateException && ex.InnerException != null)
                      {
                          ex = ex.InnerException;
                          if (toLog)
                          {
                              logger.Info("Exception on start: {0}", ex.Message);
                          }
                          MessageBox.Show(ex.Message, "Start", MessageBoxButtons.OK);
                      }
                  }
                  StageReporter("", "Removing firewall rules");
                  if (toLog)
                  {
                      logger.Info("Removing firewall rules");
                      MessageBox.Show("Removing firewall rules", "Removing", MessageBoxButtons.OK);
                  }
                      SetIndeterminate(true);
                  SCP.remove_fw_rules(SubutaiDir);
                  //UpdateProgress(10);
              }, TaskContinuationOptions.OnlyOnRanToCompletion)

                 .ContinueWith((prevTask) =>
                 {
                     Exception ex = prevTask.Exception;
                     if (prevTask.IsFaulted)
                     {
                         while (ex is AggregateException && ex.InnerException != null)
                         {
                             ex = ex.InnerException;
                             if (toLog)
                                 logger.Info("Exception on Firewall rules: {0}", ex.Message);
                             MessageBox.Show(ex.Message, "Firewall rules", MessageBoxButtons.OK);
                         }
                     }
                     StageReporter("", "Removing Subutai Social P2P service");
                     if (toLog)
                     {
                         logger.Info("Removing Subutai Social P2P service");
                         MessageBox.Show("Removing Subutai Social P2P service", "Removing", MessageBoxButtons.OK);
                     }
                     mess = SCP.stop_process("p2p");
                     mess = "";
                     mess = SCP.stop_service("Subutai Social P2P", 5000);
                     mess = "";
                     mess = SCP.remove_service("Subutai Social P2P");
                     //UpdateProgress(20);
                     StageReporter("", "Stopping SubutaiTray processes");
                     if (toLog)
                     {
                         logger.Info("Stopping SubutaiTray processes");
                         MessageBox.Show("Stopping SubutaiTray processes", "Removing", MessageBoxButtons.OK);
                     }
                     mess = SCP.stop_process("SubutaiTray");
                     //UpdateProgress(30);

                 }, TaskContinuationOptions.OnlyOnRanToCompletion)

                 .ContinueWith((prevTask) =>
                 {
                     Exception ex = prevTask.Exception;
                     if (prevTask.IsFaulted)
                     {
                         while (ex is AggregateException && ex.InnerException != null)
                         {
                             ex = ex.InnerException;
                             if (toLog)
                                 logger.Info("Exception on Removing Subutai Social P2P service and tray: {0}", ex.Message);
                             MessageBox.Show(ex.Message, "Subutai P2P service and SubutaiTray", MessageBoxButtons.OK);
                         }
                         
                     }
                     StageReporter("", "Removing /home directory link");
                     if (toLog)
                     {
                         logger.Info("Removing //home directory link");
                         MessageBox.Show("Removing //home directory link", "Removing", MessageBoxButtons.OK);
                     }
                     //Remove /home shortcut
                     if (SubutaiDir != "")
                     {
                         mess = FD.remove_from_home(SubutaiDir);
                         mess = FD.remove_home(SubutaiDir);
                     }
                     //Remove Subutai dirs from Path
                     //UpdateProgress(55);

                     StageReporter("", "Removing Subutai dirs from %Path%");
                     if (toLog)
                     {
                         logger.Info("Removing Subutai dirs from %Path%");
                         MessageBox.Show("Removing Subutai dirs from %Path%", "Removing", MessageBoxButtons.OK);
                     }
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
                     Exception ex = prevTask.Exception;
                     if (prevTask.IsFaulted)
                     {
                         while (ex is AggregateException && ex.InnerException != null)
                         {
                             ex = ex.InnerException;
                             if (toLog)
                                 logger.Info("Exception on Removing link, Subutai dirs from %Path% and envs: {0}", ex.Message);
                             MessageBox.Show(ex.Message, "Subutai dirs", MessageBoxButtons.OK);
                         }
                     }
                     StageReporter("", "Cleaning Registry");
                     if (toLog)
                     {
                         logger.Info("Cleaning Registry");
                         MessageBox.Show("Cleaning Registry", "Removing", MessageBoxButtons.OK);
                     }
                     //Clean registry
                     RG.delete_from_reg();
                     //UpdateProgress(70);
                 }, TaskContinuationOptions.OnlyOnRanToCompletion)

                 .ContinueWith((prevTask) =>
                 {
                     Exception ex = prevTask.Exception;
                     if (prevTask.IsFaulted)
                     {
                         while (ex is AggregateException && ex.InnerException != null)
                         {
                             ex = ex.InnerException;
                             if (toLog)
                                 logger.Info("Exception on Cleaning Registry: {0}", ex.Message);
                             MessageBox.Show(ex.Message, "Cleaning Registry", MessageBoxButtons.OK);
                         }
                     }
                     StageReporter("", "Removing TAP interfaces");
                     if (toLog)
                     {
                         logger.Info("Removing TAP interfaces");
                         MessageBox.Show("Removing TAP interfaces", "Removing", MessageBoxButtons.OK);
                     }
                     AP.del_TAP();
                     //UpdateProgress(80);
                 }, TaskContinuationOptions.OnlyOnRanToCompletion)

                .ContinueWith((prevTask) =>
                {
                    Exception ex = prevTask.Exception;
                    if (prevTask.IsFaulted)
                    {
                        while (ex is AggregateException && ex.InnerException != null)
                        {
                            ex = ex.InnerException;
                            if (toLog)
                                logger.Info("Exception on Removing TAP interfaces: {0}", ex.Message);
                            MessageBox.Show(ex.Message, "Removing TAP interfaces", MessageBoxButtons.OK);
                        }
                    }
                    StageReporter("", "Removing old logs");
                    if (toLog)
                    {
                        logger.Info("Removing old logs");
                        MessageBox.Show("Removing old logs", "Removing", MessageBoxButtons.OK);
                    }
                    //Remove log dir
                    FD.remove_log_dir();

                }, TaskContinuationOptions.OnlyOnRanToCompletion)

                .ContinueWith((prevTask) =>
                {
                    Exception ex = prevTask.Exception;
                    if (prevTask.IsFaulted)
                    {
                        while (ex is AggregateException && ex.InnerException != null)
                        {
                            ex = ex.InnerException;
                            if (toLog)
                                logger.Info("Exception on Removing old logs: {0}", ex.Message);
                            MessageBox.Show(ex.Message, "Removing old logs", MessageBoxButtons.OK);
                        }
                    }
                    StageReporter("", "Removing Subutai Virtual Machines");
                    if (toLog)
                    {
                        logger.Info("Removing Subutai Virtual Machines");
                        MessageBox.Show("Removing Subutai Virtual Machines", "Removing", MessageBoxButtons.OK);
                    }
                    //Remove snappy and subutai machines
                    VBx.remove_vm();
                    //Remove Oracle VirtualBox
                    StageReporter("", "Removing Oracle Virtual Box software");
                    if (toLog)
                    {
                        logger.Info("Removing Subutai Virtual  Box software");
                        MessageBox.Show("Removing Subutai Virtual  Box software", "Removing", MessageBoxButtons.OK);
                    }
                    if (!isSilent && bVBox)
                    {
                        if (toLog)
                        {
                            logger.Info("Removing Subutai Virtual Box software - yes");
                            MessageBox.Show("Removing Subutai Virtual Box software - yes", "Removing", MessageBoxButtons.OK);
                        }
                       VBx.remove_app_vbox_short("Oracle VirtualBox");
                    }
                }, TaskContinuationOptions.OnlyOnRanToCompletion)

                .ContinueWith((prevTask) =>
                 {
                     Exception ex = prevTask.Exception;
                     if (prevTask.IsFaulted)
                     {
                         while (ex is AggregateException && ex.InnerException != null)
                         {
                             ex = ex.InnerException;
                             if (toLog)
                                 logger.Info("Exception on Removing virtual machines: {0}", ex.Message);
                             MessageBox.Show(ex.Message, "VirtualBox", MessageBoxButtons.OK);
                         }
                     }
                     StageReporter("", "Removing Google Chrome");
                     if (toLog)
                     {
                         logger.Info("Removing Google Chrome");
                         MessageBox.Show("Removing Google Chrome", "Removing", MessageBoxButtons.OK);
                     }
                     if (!isSilent && bChrome)
                     {
                         if (toLog)
                         {
                             logger.Info("Removing Google Chrome - yes");
                             MessageBox.Show("Removing Google Chrome - yes", "Removing", MessageBoxButtons.OK);
                         }
                         AP.remove_chrome();
                     }
                 }, TaskContinuationOptions.OnlyOnRanToCompletion)



                .ContinueWith((prevTask) =>
                {
                    Exception ex = prevTask.Exception;
                    if (prevTask.IsFaulted)
                    {
                        while (ex is AggregateException && ex.InnerException != null)
                        {
                            ex = ex.InnerException;
                            if (toLog)
                                logger.Info("Exception on Removing Google Chrome:{0}", ex.Message);
                            MessageBox.Show(ex.Message, "Chrome", MessageBoxButtons.OK);
                        }
                    }
                    //Remove service if was installed during cancelling
                    if (toLog)
                    {
                        logger.Info("Removing Subutai P2P service if was installed after uninstall started");
                        MessageBox.Show("Removing Subutai P2P service if was installed after uninstall started", "Removing", MessageBoxButtons.OK);
                    }
                    mess = SCP.stop_process("p2p");
                    mess = "";
                    mess = SCP.stop_service("Subutai Social P2P", 5000);
                    mess = "";
                    mess = SCP.remove_service("Subutai Social P2P");

                }, TaskContinuationOptions.OnlyOnRanToCompletion)

                .ContinueWith((prevTask) =>
                {
                    Exception ex = prevTask.Exception;
                    if (prevTask.IsFaulted)
                    {
                        while (ex is AggregateException && ex.InnerException != null)
                        {
                            ex = ex.InnerException;
                            if (toLog)
                                logger.Info("Exception on Removing Subutai P2P Service: {0}", ex.Message);
                            MessageBox.Show(ex.Message, "Subutai P2P Service", MessageBoxButtons.OK);
                        }
                     }
                    StageReporter("", "Removing Subutai shortcuts");
                    if (toLog)
                    {
                        logger.Info("Removing Subutai shortcuts");
                        MessageBox.Show("Removing Subutai shortcuts", "Removing", MessageBoxButtons.OK);
                    }
                    FD.delete_Shortcuts("Subutai");
                    //UpdateProgress(80);
                    StageReporter("", "Removing Subutai directories");
                    if (toLog)
                    {
                        logger.Info("Removing Subutai directories");
                        MessageBox.Show("Removing Subutai directories", "Removing", MessageBoxButtons.OK);
                    }
                        mess = "";
                    string mesg = "";
                    if (SubutaiDir != "" && SubutaiDir != null && SubutaiDir != "C:\\" && SubutaiDir != "D:\\" && SubutaiDir != "E:\\" && !(SubutaiDir.Length < 4))
                    {
                        if (!isSilent)
                        {
                            if (bFolder)
                            {
                                if (toLog)
                                {
                                    logger.Info("Not silent, Removing Subutai directories completely");
                                    MessageBox.Show("Not silent, Removing Subutai directories completely", "Removing", MessageBoxButtons.OK);
                                }
                                    //checked, need to remove
                                    mess = FD.delete_dir(SubutaiDir);
                            }
                            else
                            {
                                if (toLog)
                                {
                                    logger.Info("Not silent, Removing Subutai bin directory");
                                    MessageBox.Show("Not silent, Removing Subutai bin directory", "Removing", MessageBoxButtons.OK);
                                }
                                    
                                mess = FD.delete_dir_bin(SubutaiDir);
                            }
                        }
                        else //silent
                        {
                            if (removeAll)
                            {
                                if (toLog)
                                {
                                    logger.Info("Silent, Removing Subutai directories completely");
                                    MessageBox.Show("Silent, Removing Subutai directories completely", "Removing", MessageBoxButtons.OK);
                                }
                                mess = FD.delete_dir(SubutaiDir);
                            }
                            else
                            {
                                if (toLog)
                                {
                                    logger.Info("Silent, Removing Subutai bin directory");
                                    MessageBox.Show("Silent, Removing Subutai bin directory", "Removing", MessageBoxButtons.OK);
                                }
                                mess = FD.delete_dir_bin(SubutaiDir);
                            }
                        }

                        if (mess.Contains("Can not"))
                        {
                            mesg = string.Format("Folder {0}\\bin can not be removed.\n\n Please close running applications that can lock files (ssh sessions, file manager windows, stop p2p service if running etc) and delete it manually", SubutaiDir);
                            if (toLog)
                            {
                                logger.Info("Failed to remove Subutai directory");
                                MessageBox.Show("Failed to remove Subutai directory", "Removing", MessageBoxButtons.OK);
                            }
                            MessageBox.Show(mesg, "Removing Subutai folder", MessageBoxButtons.OK);
                        }
                    }

                    //Remove Subutai dir from ApplicationData
                    string appUserDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    //MessageBox.Show($"AppData: {appUserDir}", "AppData", MessageBoxButtons.OK);
                    appUserDir = Path.Combine(appUserDir, "Subutai Social");
                    if (Directory.Exists(appUserDir))
                    {                    
                        try
                        {
                            Directory.Delete(appUserDir, true);
                        }
                        catch (Exception exx)
                        {
                            mesg = exx.Message;
                        }
                    }
                    SetIndeterminate(false);
                    UpdateProgress(100);
                    StageReporter("", "Finished");
                    string mes = "Subutai Social uninstalled. \n";
                    string mes1 = "\nPlease delete ";
                    string mes2 = "";
                    string mes3 = " software manually from Control Panel if You are not going to use it";
                    if (AP.app_installed("Clients\\StartMenuInternet\\Google Chrome") == 1)
                    {
                        mes1 += "Google Chrome ";
                        mes += $"{mes1}{mes2}";
                    }
                    if (AP.app_installed("Oracle\\VirtualBox") == 1)
                    {
                        if (mes.Contains("Chrome"))
                        {
                            mes += " and Oracle VirtualBox ";
                        } else
                        {
                            mes += "\nPlease delete Oracle VirtualBox ";
                        }
                    }

                    if (mes.Contains("Chrome") || mes.Contains("VirtualBox"))
                    {
                        mes += mes3;
                    }
                    mesg = string.Format($"{mes}");
                    if (toLog)
                    {
                        logger.Info("Subutai Social uninstalled");
                    }
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

            if (arg.ToLower().Contains("true"))
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

        /// <summary>
        /// Handles the Click event of the btnUninstall control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void btnUninstall_Click(object sender, EventArgs e)
        {
            this.Height += 50;
            panel2.Location = new Point(5,135);
            panel2.Visible = true;
            SetIndeterminate(false);

            btnUninstall.Enabled = false;
            panel1.Enabled = false;
            panel2.Enabled = true;
            runCleaning();
            //progressBar1.
        }

        /// <summary>
        /// Handles the CheckedChanged event of the cbxTAP control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void cbxTAP_CheckedChanged(object sender, EventArgs e)
        {
            if (cbxTAP.Checked == true)
            {
                bTAP = true;
            } else
            {
                bTAP = false;
            }
        }

        /// <summary>
        /// Handles the CheckedChanged event of the cbxFolder control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void cbxFolder_CheckedChanged(object sender, EventArgs e)
        {
            if (cbxFolder.Checked == true)
            {
                bFolder = true;
            }
            else
            {
                bFolder = false;
            }
        }

        /// <summary>
        /// Handles the CheckedChanged event of the cbxVBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void cbxVBox_CheckedChanged(object sender, EventArgs e)
        {
            if (cbxVBox.Checked == true)
            {
                bVBox = true;
            }
            else
            {
                bVBox = false;
            }
         }

        /// <summary>
        /// Handles the CheckedChanged event of the cbxChrome control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void cbxChrome_CheckedChanged(object sender, EventArgs e)
        {
            if (cbxChrome.Checked == true)
            {
                bChrome = true;
            }
            else
            {
                bChrome = false;
            }
        }
    }
}
