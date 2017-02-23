using System;
using System.IO;
using System.Windows.Forms;
using Microsoft.Win32;


namespace uninstall_clean
{
    /// <summary>
    /// Applications
    /// </summary>
    class AP
    {
        /// <summary>
        /// Deleting TAP software.
        /// </summary>
        public static void del_TAP()
        {
            string binPath = Path.Combine(clean.sysDrive, "Program Files", "TAP-Windows", "bin", "tapinstall.exe");
            string res = "";
            if (File.Exists(binPath))
            {
                res = SCP.LaunchCommandLineApp(binPath, "remove tap0901", true, false, 480000);
            }
            binPath = Path.Combine(clean.sysDrive, "Program Files", "TAP-Windows", "Uninstall.exe");
            string pathPath = Path.Combine(clean.sysDrive, "Program Files", "TAP-Windows", "bin");
            if (clean.bTAP)
                remove_app("TAP-Windows", binPath, "/S", "TAP-Windows");
        }

        /// <summary>
        /// Gets the environment variable.
        /// </summary>
        /// <param name="var_name">Name of the variable.</param>
        /// <returns></returns>
        public static string get_env_var(string var_name)
        {
            var EnvVar = Environment.GetEnvironmentVariable(var_name) ?? "";

            if (EnvVar == "")
            {
                EnvVar = Environment.GetEnvironmentVariable(var_name, EnvironmentVariableTarget.Machine) ?? "";
                if (EnvVar == "")
                {
                    EnvVar = Environment.GetEnvironmentVariable(var_name, EnvironmentVariableTarget.Process) ?? "";

                }
            }
            return EnvVar;
        }

        /// <summary>
        /// Checks if applications is installed.
        /// </summary>
        /// <param name="appName">Name of the application.</param>
        /// <returns></returns>
        public static int app_installed(string appName)
        {
            string subkey = Path.Combine("SOFTWARE\\Wow6432Node", appName);
            string subkey86 = Path.Combine("SOFTWARE\\", appName);
            RegistryKey rk = Registry.LocalMachine.OpenSubKey(subkey);
            RegistryKey rk86 = Registry.LocalMachine.OpenSubKey(subkey86);
            if (rk == null && rk86 == null)
            {
                return 0;
            }
            return 1;
        }

        /// <summary>
        /// Removes the application.
        /// </summary>
        /// <param name="app_name">Name of the application.</param>
        /// <param name="cmd">The command.</param>
        /// <param name="args">The arguments.</param>
        /// <param name="app_path">The application path.</param>
        public static  void remove_app(string app_name, string cmd, string args, string app_path)
        {
            string mess = "";
            if (File.Exists(cmd))
            {
                string res = SCP.LaunchCommandLineApp(cmd, args, false, false, 480000);
                if (res.Contains("|Error") || res.ToLower().Contains("error"))
                {
                    mess = $"{app_name} was not removed, please uninstall manually";
                }
                else
                {
                    mess = $"{app_name} uninstalled";
                }
            }
            else
            {
                mess = $"Probably {app_name} was not installed, please check and uninstall manually";
            }
            //MessageBox.Show(mess, $"Uninstalling {app_name}", MessageBoxButtons.OK);
            if (app_path != "" || app_path != null)
            {
                FD.remove_from_Path(app_path);
            }
        }

        /// <summary>
        /// Removes Google Chrome usefull when Chrome is not visible in Control Panel
        /// </summary>
        public static void remove_chrome()
        {
            if (AP.app_installed("Clients\\StartMenuInternet\\Google Chrome") == 0)
            {
                MessageBox.Show("Google Chrome is not installed on Your machine", "Removing Google Chrome", MessageBoxButtons.OK);
                return;
            }

            //Check if Chrome is running
            SCP.stop_process("chrome.exe");
            SCP.stop_process("Google Chrome");
            SCP.stop_process("Google Chrome (32 bit)");
            //Unpin from taskbar
            if (clean.toLog)
                MessageBox.Show("Google Chrome processes stopped", "Removing Google Chrome", MessageBoxButtons.OK);
            RG.rg_clean_chrome();
            if (clean.toLog)
                MessageBox.Show("Google Chrome register cleaned", "Removing Google Chrome", MessageBoxButtons.OK);
            FD.fd_clean_chrome();
            if (clean.toLog)
                MessageBox.Show("Google Chrome files cleaned", "Removing Google Chrome", MessageBoxButtons.OK);
        }
    }
}
