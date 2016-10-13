using System;
using System.Windows.Forms;
using System.Threading;
using NLog;

namespace Deployment
{
    static class Program
    {
        public static f_confirm form_;
        public static f_install form1;
        public static InstallationFinished form2;

        //public static string[] cmd_args = Environment.GetCommandLineArgs();
        public static string inst_args = "params=deploy-redist,prepare-vbox,dev,prepare-rh,deploy-p2p network-installation=true kurjunUrl=https://cdn.subut.ai:8338 repo_descriptor=repomd5-dev";

        public static bool stRun = false; 
        private static NLog.Logger logger = LogManager.GetCurrentClassLogger();
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            AppDomain current_domain = AppDomain.CurrentDomain;
            Application.ThreadException += new ThreadExceptionEventHandler(Application_ThreadException);
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

            PreventIfInstalled();
            form_ = new f_confirm();
            form_.ShowDialog();
            if (stRun)
            {
                form1 = new f_install(inst_args);
                form2 = new InstallationFinished("complete", "");

                Application.Run(form1);
            } else
            {
                Environment.Exit(1);
            }
        }

        public static void ShowError(string Text, string Caption)
        {
            if (Program.form1.Visible == true)
                Program.form1.Hide();
            var result = MessageBox.Show(Text, Caption, MessageBoxButtons.OK);
            if (result == DialogResult.OK)
                  Program.form1.Close();
        
        }

        static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            MessageBox.Show(e.Exception.Message, "Unhandled Thread Exception");
            logger.Error(e.Exception.Message, "Thread Exception");
            form1.Close();
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            MessageBox.Show((e.ExceptionObject as Exception).Message, "Unhandled UI Exception");
            logger.Error((e.ExceptionObject as Exception).Message, "Application Exception");
            form1.Close();
        }

        static void PreventIfInstalled()
        {
            string path = Inst.subutai_path();
            if (!path.Equals("NA"))
            {
                var result = MessageBox.Show($"Subutai is already installed in {path}. Please uninstall before new installation" , "Subutai Social", MessageBoxButtons.OK);
                if (result == DialogResult.OK)
                    Environment.Exit(1);
            }
        }
    }
}
