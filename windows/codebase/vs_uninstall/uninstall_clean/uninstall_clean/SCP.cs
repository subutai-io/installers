using System;
using System.ServiceProcess;
using System.Linq;
using System.Threading;
using System.Diagnostics;
using System.Text;
using System.Windows.Forms;

namespace uninstall_clean
{
    /// <summary>
    ///  class SCP - working with ServiCes and Processes
    /// </summary>
    class SCP
    {
        /// <summary>
        /// Stopping service.
        /// </summary>
        /// <param name="serviceName">Name of the service.</param>
        /// <param name="timeoutMilliseconds">The timeout milliseconds.</param>
        /// <returns></returns>
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

        /// <summary>
        /// Removing P2P Subutai Social service.
        /// </summary>
        /// <param name="serviceName">Name of the service.</param>
        /// <returns></returns>
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

            mess = LaunchCommandLineApp("sc", $"delete \"Subutai Social P2P\"", true, false, 300000);
            return (mess);
        }

        /// <summary>
        /// Stopping process.
        /// </summary>
        /// <param name="procName">Name of the process</param>
        /// <returns></returns>
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

        /// <summary>
        /// Removing firewall rules.
        /// </summary>
        /// <param name="appdir">Application installation directory</param>
        public static void remove_fw_rules(string appdir)
        {
            string res = "";
            res = LaunchCommandLineApp("netsh", " advfirewall firewall delete rule name=all service=\"Subutai Social P2P\"", true, false, 300000);

            res = LaunchCommandLineApp("netsh", $" advfirewall firewall delete rule name=all program=\"{appdir}bin\\p2p.exe\"", true, false, 300000);
            res = LaunchCommandLineApp("netsh", $" advfirewall firewall delete rule name=all program=\"{appdir}bin\\tray\\SubutaiTray.exe\"", true, false, 300000);

            res = LaunchCommandLineApp("netsh", $" advfirewall firewall delete rule name=\"vboxheadless_in\"", true, false, 300000);
            res = LaunchCommandLineApp("netsh", $" advfirewall firewall delete rule name=\"vboxheadless_out\"", true, false, 300000);

            res = LaunchCommandLineApp("netsh", $" advfirewall firewall delete rule name=\"virtualbox_in\"", true, false, 300000);
            res = LaunchCommandLineApp("netsh", $" advfirewall firewall delete rule name=\"virtualbox_out\"", true, false, 300000);
        }

        /// <summary>
        /// Launches the command line application.
        /// </summary>
        /// <param name="filename">The filename.</param>
        /// <param name="arguments">The arguments.</param>
        /// <param name="bCrNoWin">if set to <c>true</c> [b cr no win].</param>
        /// <param name="bUseShExe">if set to <c>true</c> [b use sh executable].</param>
        /// <returns></returns>

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

        /// <summary>
        /// Launches the command line application with timeout.
        /// </summary>
        /// <param name="filename">The filename.</param>
        /// <param name="arguments">The arguments.</param>
        /// <param name="timeout">The timeout for command in ms.</param>
        /// <returns></returns>
        public static string LaunchCommandLineApp(string filename, string arguments, bool bCrNoWin, bool bUseShExe, int timeout)
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
            //string output;
            //string err;
            Process process = new Process();
            process.StartInfo = startInfo;

            StringBuilder output = new StringBuilder();
            StringBuilder error = new StringBuilder();

            using (AutoResetEvent outputWaitHandle = new AutoResetEvent(false))
            using (AutoResetEvent errorWaitHandle = new AutoResetEvent(false))
            {
                process.OutputDataReceived += (sender, e) =>
                {
                    if (e.Data == null)
                    {
                        outputWaitHandle.Set();
                    }
                    else
                    {
                        output.AppendLine(e.Data);
                    }
                };
                process.ErrorDataReceived += (sender, e) =>
                {
                    if (e.Data == null)
                    {
                        errorWaitHandle.Set();
                    }
                    else
                    {
                        error.AppendLine(e.Data);
                    }
                };

                try
                {
                    // Start the process with the info we specified.
                    // Call WaitForExit and then the using statement will close.
                    process.Start();

                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    if (process.WaitForExit(timeout) &&
                        outputWaitHandle.WaitOne(timeout) &&
                        errorWaitHandle.WaitOne(timeout))
                    {
                        // Process completed. Check process.ExitCode here.
                        return ($"executing: \"{filename} {arguments}\"|{output}|{error}");
                    }
                    else
                    {
                        // Timed out.
                        return ($"1|{filename} was timed out|Error");
                    }
                }
                catch (Exception ex)
                {
                    //MessageBox.Show($"Can not run {filename}: {ex.Message}");
                    return ($"1|{filename} was not executed|Error");
                    //try to repeat, counting 
                    //uncomment if need repeated tries 
                    //LaunchCommandLineApp(filename, arguments, 0);//will try 3 times
                    //Thread.Sleep(10000); 
                }
            }
        }
    }
}
