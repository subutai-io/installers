using System;
using System.Windows.Forms;
using System.Threading;
using DevExpress.UserSkins;
using DevExpress.Skins;
using DevExpress.LookAndFeel;
using DevExpress.XtraEditors;
using NLog;

namespace Deployment
{
    static class Program
    {
        public static Form1 form1;
        public static InstallationFinished form2;

        private static NLog.Logger logger = LogManager.GetCurrentClassLogger();
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            BonusSkins.Register();
            SkinManager.EnableFormSkins();
            UserLookAndFeel.Default.SetSkinStyle("DevExpress Style");

            AppDomain current_domain = AppDomain.CurrentDomain;
            Application.ThreadException += new ThreadExceptionEventHandler(Application_ThreadException);
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

            form1 = new Form1();
            form2 = new InstallationFinished("complete","");

            Application.Run(form1);
        }

        public static void ShowError(string Text, string Caption)
        {
            if (Program.form1.Visible == true)
                Program.form1.Hide();
            //Program.form1.Visible = false;
            var result = XtraMessageBox.Show(Text, Caption, MessageBoxButtons.OK);
            if (result == DialogResult.OK)
                  Program.form1.Close();
            //Application.Exit();
           
        }

        static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            //ShowError(e.Exception.Message + " Installation failed. Please uninstall with Start->All Applications->Subutai(folder)->Uninstall and try to install later", "Thread Exception");
            MessageBox.Show(e.Exception.Message, "Unhandled Thread Exception");
            logger.Error(e.Exception.Message, "Thread Exception");
            //Application.Exit();
            form1.Close();
            //form1.Visible = false;
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            //ShowError((e.ExceptionObject as Exception).Message + " Installation failed. Please uninstall with Start->All Applications->Subutai(folder)->Uninstall and try to install later", 
            //    "Application Exception");
            MessageBox.Show((e.ExceptionObject as Exception).Message, "Unhandled UI Exception");
            logger.Error((e.ExceptionObject as Exception).Message, "Application Exception");
            form1.Close();
            //form1.Visible = false;
        }
    }
}
