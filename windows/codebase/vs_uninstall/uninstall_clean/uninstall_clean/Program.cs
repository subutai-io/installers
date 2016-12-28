using System;
using System.Windows.Forms;
using System.Diagnostics;
using System.Threading;
using System.Security.Principal;

namespace uninstall_clean
{
    static class Program
    {
        public static clean form1;
        /// <summary>
        /// Mutex to ensure singletone 
        /// </summary>
        public static Mutex mt_single = new Mutex(false, "uninstall-clean");
        
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            WindowsPrincipal principal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
            bool hasAdministrativeRight = principal.IsInRole(WindowsBuiltInRole.Administrator);

            // waiting 2 seconds in case that the instance is just 
            // shutting down
            if (!mt_single.WaitOne(TimeSpan.FromSeconds(0), false))
            {
                //MessageBox.Show("Subutai uninstall already started!", "", MessageBoxButtons.OK);
                return;
            }

            if (!hasAdministrativeRight)
            {
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.UseShellExecute = true;
                startInfo.WorkingDirectory = Environment.CurrentDirectory;
                startInfo.FileName = Application.ExecutablePath;
                startInfo.Verb = "runas";
                try
                {
                    Process p = Process.Start(startInfo);
                    Application.Exit();
                }
                catch (System.ComponentModel.Win32Exception ex)
                {
                    MessageBox.Show("This utility requires elevated priviledges to complete correctly.", "Error: UAC Authorisation Required", MessageBoxButtons.OK);
                    //Debug.Print(ex.Message);
                    mt_single.ReleaseMutex();
                    return;
                }
            }
            else
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                form1 = new clean();
                Application.Run(form1);
            }
        }
    }
}
