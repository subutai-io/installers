using System;
using System.Windows.Forms;
using DevExpress.UserSkins;
using DevExpress.Skins;
using DevExpress.LookAndFeel;
using DevExpress.XtraEditors;

namespace Deployment
{
    static class Program
    {
        public static Form1 form1;
        public static InstallationFinished form2;

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

            form1 = new Form1();
            form2 = new InstallationFinished();

            Application.Run(form1);
        }

        public static void ShowError(string Text, string Caption)
        {
            Program.form1.Hide();

            var result = XtraMessageBox.Show(Text, Caption, MessageBoxButtons.OK);
            if (result == DialogResult.OK)
            {
                Application.Exit();
            }
        }
    }
}
