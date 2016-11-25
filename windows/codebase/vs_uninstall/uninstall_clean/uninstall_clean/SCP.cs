using System;
using System.ServiceProcess;
using System.Linq;
using System.Threading;
using System.Diagnostics;
using System.Windows.Forms;

namespace uninstall_clean
{
    /// <summary>
    ///  class SCP - working with ServiCes and Processes
    /// </summary>
    class SCP
    {
        public static  string stop_service(string serviceName, int timeoutMilliseconds)
        {
            ServiceController service = new ServiceController(serviceName);
            try
            {
                TimeSpan timeout = TimeSpan.FromMilliseconds(timeoutMilliseconds);
                //label1.Text = "Stopping " + serviceName + " service";
                service.Stop();
                service.WaitForStatus(ServiceControllerStatus.Stopped, timeout);
                Thread.Sleep(2000);
                return "0";
            }
            catch (Exception ex)
            {
                //label1.Text = "Can not stop service " + serviceName + "  " + ex.Message.ToString();
                //logger.Error(ex.Message, "Stopping Subutai Social P2P service");
                return ex.Message.ToString();
            }
        }

        public static string remove_service(string serviceName)
        {
            ServiceController ctl = ServiceController.GetServices().FirstOrDefault(s => s.ServiceName == serviceName);
            string mess = "";

            ////if (ctl == null)
            ////{
            ////    mess = "Not installed";
            ////}
            ////else
            ////{
            ////    mess = LaunchCommandLineApp("nssm", $"remove \"Subutai Social P2P\" confirm", true, false);
            ////}

            //mess = LaunchCommandLineApp("nssm", $"remove \"Subutai Social P2P\" confirm", true, false);
            mess = LaunchCommandLineApp("sc", $"delete \"Subutai Social P2P\"", true, false);
            return (mess);
        }

        public static string stop_process(string procName)
        {
            Process[] processes = Process.GetProcessesByName(procName);
            foreach (Process process in processes)
            {
                try
                {
                    //label1.Text = "Stopping " + procName + " process";
                    process.Kill();
                    //Thread.Sleep(3000);
                    return "0";
                }
                catch (Exception ex)
                {
                    return "Can not stop process " + procName + ". " + ex.Message.ToString();
                }
            }
            return "1";
        }

        public static void remove_fw_rules(string appdir)
        {
            string res = "";
            res = LaunchCommandLineApp("netsh", " advfirewall firewall delete rule name=all service=\"Subutai Social P2P\"", true, false);

            res = LaunchCommandLineApp("netsh", $" advfirewall firewall delete rule name=all program=\"{appdir}bin\\p2p.exe\"", true, false);
            res = LaunchCommandLineApp("netsh", $" advfirewall firewall delete rule name=all program=\"{appdir}bin\\tray\\SubutaiTray.exe\"", true, false);

            res = LaunchCommandLineApp("netsh", $" advfirewall firewall delete rule name=\"vboxheadless_in\"", true, false);
            res = LaunchCommandLineApp("netsh", $" advfirewall firewall delete rule name=\"vboxheadless_out\"", true, false);

            res = LaunchCommandLineApp("netsh", $" advfirewall firewall delete rule name=\"virtualbox_in\"", true, false);
            res = LaunchCommandLineApp("netsh", $" advfirewall firewall delete rule name=\"virtualbox_out\"", true, false);
        }

        public static string LaunchCommandLineApp(string filename, string arguments, bool bCrNoWin, bool bUseShExe)
        {
            // Use ProcessStartInfo class
            var startInfo = new ProcessStartInfo
            {
                CreateNoWindow = bCrNoWin,//true,
                UseShellExecute = bUseShExe,//false,
                FileName = filename,
                WindowStyle = ProcessWindowStyle.Hidden,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            string output;
            string err;

            try
            {
                // Start the process with the info we specified.
                // Call WaitForExit and then the using statement will close.
                using (var exeProcess = Process.Start(startInfo))
                {
                    output = exeProcess.StandardOutput.ReadToEnd();
                    err = exeProcess.StandardError.ReadToEnd();
                    exeProcess.WaitForExit();
                    return ("executing " + filename + " \nstdout: " + output + " \nstderr: " + err);
                }
            }
            catch (Exception)
            {
                Thread.Sleep(2000);
                //LaunchCommandLineApp(filename, arguments);
            }
            return ($"1|{filename} was not executed|Error");
        }
    }
}
