using System;
using System.Windows.Forms;
using System.Diagnostics;
using System.Threading;
using NLog;

namespace Deployment
{
    /// <summary>
    /// Starts installation application - shows installation parameters confirmation form
    /// After installation parametrs defined starts installation 
    /// Parameters string, part of parameters goes from command line, part from confirmation form
    /// Before parameters were defined in installer, noe only command line arguments are defined in third-party installer
    /// </summary>
    static class Program
    {
        public static f_confirm form_; //Parameters confirmation form
        public static f_install form1; //Installation form
        public static InstallationFinished form2; //Installation finished form
        public static string st_fail_reason = "";

        /// <summary>
        /// Command line arguments: Installation type (prod, dev, master, Repo descriptor file name and "Install"
        /// </summary>
        public static string[] cmd_args = Environment.GetCommandLineArgs();
        public static string inst_args = "";
        public static string inst_type = "";
        public static string inst_Dir = "";
        public static ulong vmCPUs = 0;
        public static ulong vmRAM = 0;
        

        public static bool stRun = false; 

        private static System.Timers.Timer mTimer = new System.Timers.Timer();
        private static System.Timers.ElapsedEventHandler mTimerHandler = null;
       
        const int WM_CLOSE = 0x0010;
        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam);

        private static NLog.Logger logger = LogManager.GetCurrentClassLogger();

        //[STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            AppDomain current_domain = AppDomain.CurrentDomain;
            Application.ThreadException += new ThreadExceptionEventHandler(Application_ThreadException);
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

            //Add installation type command line parameter to parameter string
            inst_args = $"params=deploy-redist,prepare-vbox,prepare-rh,deploy-p2p network-installation=true kurjunUrl=https://cdn.subut.ai:8338";
            inst_type = InstType(cmd_args[1]);
            string repo_desc = cmd_args[2];
            
            if (inst_type != "" && inst_type != null && inst_type != "prod")
            {
                inst_args = $"params=deploy-redist,prepare-vbox,prepare-rh,deploy-p2p,{inst_type} network-installation=true kurjunUrl=https://cdn.subut.ai:8338 repo_descriptor={repo_desc}";
            } else
            {
                inst_args = $"params=deploy-redist,prepare-vbox,prepare-rh,deploy-p2p network-installation=true kurjunUrl=https://cdn.subut.ai:8338 repo_descriptor={repo_desc}";
            }
            logger.Info("Argument string: {0}", inst_args);
            //Check if_installer_run - if "Installer", will run application in new process - to close installer
            string if_installer_run = cmd_args[3];
            if (if_installer_run.Equals("Installer"))
            {
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.UseShellExecute = true;
                startInfo.WorkingDirectory = Environment.CurrentDirectory;
                startInfo.FileName = Application.ExecutablePath;
                startInfo.Arguments = $"{cmd_args[1]} {cmd_args[2]} Run";
                startInfo.Verb = "runas";
                try
                {
                    Thread.Sleep(3000);
                    Process p = Process.Start(startInfo);
                    Environment.Exit(0);
                    //Application.Exit();
                }
                catch (System.ComponentModel.Win32Exception)
                {
                    MessageBox.Show("This utility requires elevated priviledges to complete correctly.", "Error: UAC Authorisation Required", MessageBoxButtons.OK);
                    return;
                }
            }
            inst_Dir = Inst.subutai_path();
            form_ = new f_confirm();
            form_.ShowDialog();
            if (stRun)
            {
                form1 = new f_install(inst_args);
                form2 = new InstallationFinished("complete", "");

                Application.Run(form1);
            } else
            {
                if (inst_Dir.Equals("") || inst_Dir.Equals("\"\""))
                {
                    inst_Dir = Inst.subutai_path();
                }
               
                string cmd = $"{inst_Dir}bin\\uninstall-clean.exe";
                cmd = FD.path_with_commas(cmd);
                var startInfo = new ProcessStartInfo
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    FileName =  cmd,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    Arguments = "Silent NoAll"
                };
                var exeProcess = Process.Start(startInfo);
                Environment.Exit(1);
            }
        }

        /// <summary>
        /// public static void ShowError(string Text, string Caption)
        /// This method is called when exception preventing further installation or error catched 
        /// Shows error message and exits application
        /// </summary>
        /// <param name="Text">Text for message box</param>
        /// <param name="Caption">Caption for MessageBox</param>
        public static void ShowError(string Text, string Caption)
        {
           mTimerHandler = ((sender, args)
                =>
                {
                    mTimer.Elapsed -= mTimerHandler;
                    mTimer.Enabled = false;
                    //mTimer.Close();
                    //mTimer.Dispose();
                    MsgBoxClose(Caption);
                });

            mTimer.Elapsed += mTimerHandler;
            mTimer.Interval = 60000;
            mTimer.AutoReset = false;
            mTimer.Enabled = true;
            try
            {
                Program.form1.Invoke((MethodInvoker)delegate
                {
                    st_fail_reason = Text;
                    mTimer.Enabled = true;
                    mTimer.Start();
                    var result = MessageBox.Show(Text, Caption, MessageBoxButtons.OK);
                    if (result == DialogResult.OK)
                    {
                        if (Program.form1.Visible == true)
                            Program.form1.Hide();
                        Program.form1.Close();
                    }
                });
            }
            catch (Exception ex)
            {
                string tmp = ex.Message;
            }
        }

        /// <summary>
        /// static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        /// Event handler for all thread exceptions 
        /// This method is called when exception preventing further installation or error from thread catched 
        /// Shows error message and exits application
        /// </summary>
        static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            st_fail_reason = e.Exception.Message;
            logger.Error(e.Exception.Message, "Application Thread Exception");
            ShowError(e.Exception.Message, "Unhandled Thread Exception");
        }

        /// <summary>
        /// static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        /// Event handler for all unhandled  exceptions 
        /// This method is called when exception preventing further installation or error from thread catched 
        /// Shows error message and exits application
        /// </summary>
        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            st_fail_reason = (e.ExceptionObject as Exception).Message;
            logger.Error((e.ExceptionObject as Exception).Message, "Application Exception");
            ShowError((e.ExceptionObject as Exception).Message, "Unhandled UI Exception");
        }

        /// <summary> 
        /// static string InstType(string inStr)
        /// Define installtion type from command line parameters 
        /// </summary>
        /// <param name="inStr">Text for message box</param>
        static string InstType(string inStr)
        {
            //string outStr = "";
            // inStr.Substring(inStr.LastIndexOf('-'), inStr.Length - inStr.LastIndexOf('.') + 1);
            //if (!outStr.Equals("dev") && !outStr.Equals("master"))
            //{
            //    return "";
            //}
            if (inStr.Contains("dev"))
            {
                return "dev";
            }
            if (inStr.Contains("master"))
            {
                return "master";

            }
            return "prod";
        }

        /// <summary>
        /// MessageBox close.
        /// </summary>
        static void MsgBoxClose(string sCaption)
        {
            IntPtr mbWnd = FindWindow(null, sCaption);
            if (mbWnd != IntPtr.Zero)
                SendMessage(mbWnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
        }
    }
}
