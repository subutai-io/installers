using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Forms;
using System.ServiceProcess;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Text.RegularExpressions;
using Microsoft.Win32;


namespace uninstall_clean
{
    public partial class clean : Form
    {
        //private static NLog.logger logger = LogManager.GetCurrentClasslogger();
        private static string sysDrive = "";
        public clean()
        {
            InitializeComponent();
            progressBar1.Visible = true;
            label2.Visible = false;
            label1.Text = "Removing Subutai Social";
        }

        private void clean_Load(object sender, EventArgs e)
        {
              clean_all();
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
        private void clean_all()
        {
           
            var SubutaiDir = Environment.GetEnvironmentVariable("Subutai") ?? "";

            remove_fw_rules(SubutaiDir);
            string mess = stop_process("p2p");
            //logger.Info("Stopping p2p process: {0}", mess);
            mess = "";
            mess = stop_service("Subutai Social P2P", 5000);
            mess = "";
            mess = remove_service("Subutai Social P2P");
            mess = "";
            //logger.Info("Removing p2p service: {0}", mess);
            //MessageBox.Show(mess + " Service was not running.", "Stopping P2P service", MessageBoxButtons.OK);

            mess = stop_process("SubutaiTray");
            //logger.Info("Stopping SubutaiTray process: {0}", mess);
            mess = stop_process("SubutaiTray");
            //logger.Info("Stopping SubutaiTray process: {0}", mess);
            //MessageBox.Show(mess + " Application was not running.", "Stopping SubutaiTray", MessageBoxButtons.OK);
            mess = "";
           
            delete_Shortcuts("Subutai");
            remove_vm();

            if (SubutaiDir != "" && SubutaiDir != null && SubutaiDir != "C:\\" && SubutaiDir != "D:\\" && SubutaiDir != "E:\\")
            {
                DialogResult drs = MessageBox.Show($"Remove folder {SubutaiDir}? (Do not remove if going to install again)", "Subutai Virtual Machines",
                                MessageBoxButtons.YesNo,
                                MessageBoxIcon.Question,
                                MessageBoxDefaultButton.Button1);
                mess = "";
                if (drs == DialogResult.Yes)
                {
                    mess = delete_dir(SubutaiDir);
                }
                //logger.Info("Deleting Subutai directory: {0}", mess);
                if (mess.Contains("Can not"))
                {
                    MessageBox.Show($"Folder {SubutaiDir} can not be removed. Please delete it manually",
                        "Removing Subutai folder", MessageBoxButtons.OK);
                }
            }
            remove_env();
            SubutaiDir = Environment.GetEnvironmentVariable("Subutai") ?? "";

            Environment.SetEnvironmentVariable("Subutai", "", EnvironmentVariableTarget.Machine);
            Environment.SetEnvironmentVariable("Subutai", "", EnvironmentVariableTarget.User);
            Environment.SetEnvironmentVariable("Subutai", "", EnvironmentVariableTarget.Process);

            delete_from_reg();
            progressBar1.Visible = false;

            //Find if registry cleaner exists 

            string rclean_path = logDir();
            if (rclean_path != "")
            {
                rclean_path = Path.Combine(rclean_path, "subutai-clean-registry.reg");
                LaunchCommandLineApp("regedit.exe", $"/s {rclean_path}");
            }

            del_TAP();
            MessageBox.Show("Subutai Social uninstalled", "Information", MessageBoxButtons.OK);
            Environment.Exit(0);
        }
        private string stop_service(string serviceName, int timeoutMilliseconds)
        {
            ServiceController service = new ServiceController(serviceName);
            try
            {
                TimeSpan timeout = TimeSpan.FromMilliseconds(timeoutMilliseconds);
                label1.Text = "Stopping " + serviceName + " service";
                service.Stop();
                service.WaitForStatus(ServiceControllerStatus.Stopped, timeout);
                Thread.Sleep(2000);
                return "0";
            }
            catch (Exception ex)
            {
                label1.Text = "Can not stop service " + serviceName + "  " + ex.Message.ToString();
                //logger.Error(ex.Message, "Stopping Subutai Social P2P service");
                return ex.Message.ToString();
            }

        }

        private string remove_service(string serviceName)
        {
            ServiceController ctl = ServiceController.GetServices().FirstOrDefault(s => s.ServiceName == serviceName);
            string mess;

            if (ctl == null)
            {
                return ("Not installed");
            } else
            {
                mess = LaunchCommandLineApp("nssm", $"remove \"Subutai Social P2P\" confirm");
                return (mess);
            }
        }

        private string stop_process(string procName)
        {
            try
            {
                label1.Text = "Stopping " + procName + " process";
                Process[] proc = Process.GetProcessesByName(procName);
                proc[0].Kill();
                Thread.Sleep(3000);
                return "0";
            }
            catch (Exception ex)
            {
                label1.Text = "Can not stop process " + procName + ". " + ex.Message.ToString();
                //logger.Error(ex.Message, "Stopping process");
                return "Can not stop process " + procName + ". " + ex.Message.ToString();
            }

        }

        private string delete_dir(string dirName)
        {
            try
            {
 
                label1.Text = "Deleting " + dirName + " folder";
                if (Directory.Exists(dirName))
                    Directory.Delete(dirName, true);

                Thread.Sleep(5000);
                return "0";
            }
            catch (Exception ex)
            {
 
                label1.Text = "Can not delete folder " + dirName + ". " + ex.Message.ToString();
                return "Can not delete folder " + dirName + ". " + ex.Message.ToString();
            }
        }//

        private void deleteDirectory(string path, bool recursive)
        {
            // Delete all files and sub-folders?
            if (recursive)
            {
                // Yep... Let's do this
                var subfolders = Directory.GetDirectories(path);
                foreach (var s in subfolders)
                {
                    deleteDirectory(s, recursive);
                }
            }

            // Get all files of the folder
            var files = Directory.GetFiles(path);
            foreach (var f in files)
            {
                // Get the attributes of the file
                var attr = File.GetAttributes(f);

                // Is this file marked as 'read-only'?
                if ((attr & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                {
                    // Yes... Remove the 'read-only' attribute, then
                    File.SetAttributes(f, attr ^ FileAttributes.ReadOnly);
                }

                // Delete the file
                File.Delete(f);
            }

            // When we get here, all the files of the folder were
            // already deleted, so we just delete the empty folder
            Directory.Delete(path);
        }

        private void delete_Shortcut(string shPath, string aName, Boolean isDir)
        {
            
            var app = "";
            if (isDir)
            {
                app = aName;
            }
            else
            {
                app = aName + ".lnk";
            }

            string fullname = Path.Combine(shPath, app);
            string str = fullname;

            Boolean if_Exists;
            if (isDir)
            {
                if_Exists = Directory.Exists(fullname);
            }
            else
            {
                if_Exists = File.Exists(fullname);
            }

            if (if_Exists && !isDir)
            {
                try
                {
                    File.Delete(fullname);
                    str = "File " + fullname + " deleted"; ;
                }
                catch (Exception ex)
                {
                    str = ex.ToString();
                }
                finally
                {
                    //MessageBox.Show(str, fullname, MessageBoxButtons.OK);
                }
            }

            if (if_Exists && isDir)
            {
                try
                {
                    Directory.Delete(fullname);
                    str = fullname + " deleted";
                }
                catch (Exception ex)
                {
                    str = ex.ToString();
                }
                finally
                {
                    //MessageBox.Show(str, fullname + " Folder", MessageBoxButtons.OK);
                }
            }
        }
        private void delete_Shortcuts(string appName)
        {
            label1.Text = "Deleting shortcuts";
            var shcutPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory);
            //    Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            //delete_Shortcut(shcutPath, appName);
            delete_Shortcut(shcutPath, appName, false);

            //shcutPath = Environment.GetFolderPath(Environment.SpecialFolder.StartMenu);
            //delete_Shortcut(shcutPath, appName);

            var shcutStartPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu);
            //Folder files
            shcutPath = Path.Combine(shcutStartPath, "Programs", appName);
            //Uninstall.lnk
            delete_Shortcut(shcutPath, "Uninstall", false);
            //Subutai.lnk
            delete_Shortcut(shcutPath, appName, false);

            //Start Menu/Programs/Subutai.lnk
            shcutPath = Path.Combine(shcutStartPath, "Programs");
            delete_Shortcut(shcutPath, appName, false);
            //Start Menu/Programs/Subutai
            delete_Shortcut(shcutPath, appName, true);
        }

        void remove_vm()
        {
            label1.Text = "Virtual Machines";
            string outputVms = LaunchCommandLineApp("vboxmanage", $"list vms");
            //string outputVmsRunning = LaunchCommandLineApp("vboxmanage", $"list runningvms");
            string[] rows = Regex.Split(outputVms, "\n");
            foreach (string row in rows)
            {
                if (row.Contains("subutai") || row.Contains("snappy"))
                {
                    string[] wrds = row.Split(' ');
                    foreach (string wrd in wrds)
                    {
                        if (wrd.Contains("subutai") || wrd.Contains("snappy"))
                        {
                            string vmName = wrd.Replace("\"","");
                            DialogResult drs = MessageBox.Show($"Remove virtual machine {wrd}?", "Subutai Virtual Machines",
                                MessageBoxButtons.YesNo,
                                MessageBoxIcon.Question,
                                MessageBoxDefaultButton.Button1);
                            if (drs == DialogResult.Yes)
                            {
                                //if (outputVmsRunning.Contains(wrd))
                                //{
                                string res1 = LaunchCommandLineApp("vboxmanage", $"controlvm {vmName} poweroff ");
                                Thread.Sleep(5000);
                                //}
                                string res2 = LaunchCommandLineApp("vboxmanage", $"unregistervm  --delete {vmName}");
                                Thread.Sleep(5000);
                            }
                        }
                    }
                }
            }
        }

        private string LaunchCommandLineApp(string filename, string arguments)
        {
            // Use ProcessStartInfo class
            var startInfo = new ProcessStartInfo
            {
                CreateNoWindow = true,
                UseShellExecute = false,
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
                LaunchCommandLineApp(filename, arguments);
            }
            return (filename + " was not executed");
        }

        private void delete_from_reg()
        {
            //user environment
            label1.Text = "Cleanig Registry";
            string subkey; 
            RegistryKey rk;

            //HKEY_CLASSES_ROOT\Installer\Products\CF66AAA126027D4479D5BB7808A6CDA7
            subkey = "Installer\\Products";
            rk = Registry.ClassesRoot.OpenSubKey(subkey, true);
            if (rk != null)
            {
                DeleteSubKeyTree("CF66AAA126027D4479D5BB7808A6CDA7", rk);
                //logger.Info("{0} deleted", subkey);
            }

            //HKEY_LOCAL_MACHINE\SOFTWARE\Classes\Installer\Products\CF66AAA126027D4479D5BB7808A6CDA7
            subkey = "SOFTWARE\\Classes\\Installer\\Products";
            rk = Registry.ClassesRoot.OpenSubKey(subkey, true);
            if (rk != null)
            {
                DeleteSubKeyTree("CF66AAA126027D4479D5BB7808A6CDA7", rk);
                //logger.Info("{0} deleted", subkey);
            }
            //HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\Folders
            subkey = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Installer\\Folders";
            DeleteValueFound(subkey, "Subutai");

            //HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\UserData\S - 1 - 5 - 18\Components\001B050B63BD23B49988FFEB639D2F61
            //Components
            subkey = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Installer\\UserData\\S-1-5-18\\Components";
            DeleteSubKeyFound(subkey, "Subutai");

            //********************************************************
            //HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\UserData\S - 1 - 5 - 18\Products\CF66AAA126027D4479D5BB7808A6CDA7
            subkey = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Installer\\UserData\\S-1-5-18\\Products";
            rk = Registry.LocalMachine.OpenSubKey(subkey, true);
            if (rk != null)
            {
                DeleteSubKeyTree("CF66AAA126027D4479D5BB7808A6CDA7", rk);//Main!!!
                //logger.Info("{0} deleted", subkey);
            }
            //HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{ 1AAA66FC - 2062 - 44D7 - 975D - BB87806ADC7A}
            subkey = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall";
            rk = Registry.LocalMachine.OpenSubKey(subkey, true);
            if (rk != null)
            {
                DeleteSubKeyTree("{1AAA66FC-2062-44D7-975D-BB87806ADC7A}", rk);
                //logger.Info("{0} deleted", subkey);
            }
            
            //HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Subutai 4.0.07
            subkey = "SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Uninstall";
            DeleteSubKeyFound(subkey, "Subutai");

            //HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Caphyon\Advanced Installer\LZMA\{ 1AAA66FC - 2062 - 44D7 - 975D - BB87806ADC7A}
            subkey = "SOFTWARE\\Wow6432Node\\Caphyon\\Advanced Installer\\LZMA";
            rk = Registry.LocalMachine.OpenSubKey(subkey, true);
            if (rk != null)
            {
                DeleteSubKeyTree("{1AAA66FC-2062-44D7-975D-BB87806ADC7A}", rk);
                //logger.Info("{0} deleted", subkey);
            }

            //HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Subutai Social
            subkey = "SOFTWARE\\Wow6432Node";
            rk = Registry.LocalMachine.OpenSubKey(subkey, true);
            if (rk != null)
            {
                DeleteSubKeyTree("Subutai Social", rk);
                //logger.Info("{0} deleted", subkey);
            }
            //HKEY_LOCAL_MACHINE\SYSTEM\ControlSet001\Control\Session Manager\Environment
            //Path, Subutai
            subkey = "SYSTEM\\ControlSet001\\Control\\Session Manager\\Environment";
            rk = Registry.LocalMachine.OpenSubKey(subkey, true);
            if (rk != null)
            {
                DeleteValueFound(subkey,"Subutai");
                //logger.Info("{0} deleted", subkey);
            }

            //HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Environment
            subkey = "SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Environment";
            rk = Registry.LocalMachine.OpenSubKey(subkey, true);
            if (rk != null)
            {
                //DeleteSubKeyTree("Subutai", rk);
                DeleteValueFound(subkey, "Subutai");
                //logger.Info("{0} deleted", subkey);
            }

            //HKEY_LOCAL_MACHINE\SYSTEM\VritualRoot\MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\UserData\S - 1 - 5 - 18\Components\336320F1CCE3E3F45A57FD0D4E46AB34
            subkey = "SYSTEM\\VritualRoot\\MACHINE\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Installer\\UserData\\S-1-5-18\\Components";
            DeleteSubKeyFound(subkey, "Subutai");

            //HKEY_LOCAL_MACHINE\SYSTEM\VritualRoot\MACHINE\SOFTWARE\Wow6432Node\Caphyon\Advanced Installer\LZMA
            subkey = "SYSTEM\\VritualRoot\\MACHINE\\SOFTWARE\\Wow6432Node\\Caphyon\\Advanced Installer\\LZMA";
            rk = Registry.LocalMachine.OpenSubKey(subkey, true);
            if (rk != null)
            {
                DeleteSubKeyTree("{1AAA66FC-2062-44D7-975D-BB87806ADC7A}", rk);
                //logger.Info("{0} deleted", subkey);
            }
            subkey = "SYSTEM\\VritualRoot\\MACHINE\\SOFTWARE\\Wow6432Node";
            rk = Registry.LocalMachine.OpenSubKey(subkey, true);
            if (rk != null)
            {
                DeleteSubKeyTree("Subutai Social", rk);
                //logger.Info("{0} deleted", subkey);
            }

            subkey = "SOFTWARE\\Optimal-dynamics";
            rk = Registry.CurrentUser.OpenSubKey(subkey, true);
            if (rk != null)
            {
                DeleteSubKeyTree("SS_Tray", rk);
                //logger.Info("{0} deleted", subkey);
            }

            subkey = "SOFTWARE";
            rk = Registry.CurrentUser.OpenSubKey(subkey, true);
            if (rk != null)
            {
                DeleteSubKeyTree("Optimal-dynamics", rk);
                //logger.Info("{0} deleted", subkey);
            }

            subkey = "Environment";
            rk = Registry.CurrentUser.OpenSubKey(subkey, true);
            if (rk != null)
            {
                DeleteSubKeyTree("Subutai", rk);
                //logger.Info("{0} deleted", subkey);
            }
        }

        public bool DeleteKey(string KeyName, RegistryKey baseKey)
        {
            try
            {
                // Setting
                RegistryKey sk1 = baseKey.OpenSubKey(KeyName);
                // If the RegistrySubKey doesn't exists -> (true)
                if (baseKey == null)
                    return true;
                else
                    baseKey.DeleteValue(KeyName);
                return true;
            }
            catch (Exception e)
            {
                //MessageBox.Show("Deleting SubKey " + KeyName + " exception: " + e, "Error", MessageBoxButtons.OK);
                //logger.Error(e.Message, " Delete key {0}", KeyName);
                return false;
            }
        }

        public bool DeleteSubKeyTree(string KeyName, RegistryKey baseKey)
        {
            try
            {
                if (baseKey != null)
                    baseKey.DeleteSubKeyTree(KeyName);
                return true;
            }
            catch (Exception e)
            {
                //Show("Deleting SubKeyTree " + KeyName + " exception: " + e, "Error", MessageBoxButtons.OK);
                return false;
            }
        }

        public void DeleteSubKeyFound(string subkey, string str_2_find)
        {
            RegistryKey rk = Registry.LocalMachine.OpenSubKey(subkey, true);//Components
            
            if (rk != null)
            {
                foreach (var rsk in rk.GetSubKeyNames()) //Product
                {
                   RegistryKey productKey = rk.OpenSubKey(rsk);
                    if (productKey != null)
                    {
                        foreach (var vname in productKey.GetValueNames())
                        {
                            string kvalue = Convert.ToString(productKey.GetValue(vname));
                            if (kvalue.Contains(str_2_find))
                            {
                                DeleteSubKeyTree(rsk, productKey);
                                //logger.Info("Delete subkey {0}", subkey);
                            }
                        }
                    }
                }
            }
        }

        public void DeleteValueFound(string subkey, string str_2_find)
        {
            RegistryKey rk = Registry.LocalMachine.OpenSubKey(subkey, true);//Components
            if (rk != null)
            {
                foreach (var vname in rk.GetValueNames())
                {
                    string kvalue = Convert.ToString(rk.GetValue(vname));
                    if (kvalue.Contains(str_2_find) && !vname.Contains("Path"))
                    {
                        //MessageBox.Show("value: " + kvalue, rk + "\\" + vname, MessageBoxButtons.OK);
                        rk.DeleteValue(vname);
                        //logger.Info("Delete value {0}", kvalue);
                    }
                }
            }
        }


        public void remove_env()
        {
            //logger.Info("Remove env");
            
            string strPath = Environment.GetEnvironmentVariable("Path");
            string strSubutai = Environment.GetEnvironmentVariable("Subutai");

            if (strPath == null || strPath == "")
                return;

            if (strSubutai == null || strSubutai == "")
                return;

            string[] strP = strPath.Split(';');
            List<string> lPath = new List<string>();

            foreach (string sP in strP)
            {
                if (!lPath.Contains(sP) && !sP.Contains(strSubutai))
                {
                    lPath.Add(sP);
                }
            }

            string[] slPath = lPath.ToArray();
            string newPath = string.Join(";", slPath);

            Environment.SetEnvironmentVariable("Path", newPath, EnvironmentVariableTarget.Machine);
          
        }

        public void remove_fw_rules(string appdir)
        {
            LaunchCommandLineApp("netsh", " advfirewall firewall delete rule name=all service=\"Subutai Social P2P\"");
            LaunchCommandLineApp("netsh", $" advfirewall firewall delete rule name=all program=\"{appdir}bin\\p2p.exe\"");
            LaunchCommandLineApp("netsh", $" advfirewall firewall delete rule name=all program=\"{appdir}bin\\tray\\SubutaiTray.exe\"");

            LaunchCommandLineApp("netsh", $" advfirewall firewall delete rule name=\"vboxheadless_in\"");
            LaunchCommandLineApp("netsh", $" advfirewall firewall delete rule name=\"vboxheadless_out\"");

            LaunchCommandLineApp("netsh", $" advfirewall firewall delete rule name=\"virtualbox_in\"");
            LaunchCommandLineApp("netsh", $" advfirewall firewall delete rule name=\"virtualbox_out\"");
        }

        public static string logDir()
        {
            string sysPath = Environment.GetFolderPath(Environment.SpecialFolder.System);
            sysDrive = Path.GetPathRoot(sysPath);
            string logPath = Path.Combine(sysDrive, "temp");
            if (!Directory.Exists(logPath))
            {
                Directory.CreateDirectory(logPath);
            }
            logPath = Path.Combine(logPath, "Subutai_Log");
            if (!Directory.Exists(logPath))
            {
                return "";
            }
            return logPath;
        }

        public void del_TAP()
        {
            //string binPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), 
            string binPath = Path.Combine(sysDrive, "Program Files",
            "TAP-Windows", "bin", "tapinstall.exe");
            string res = "";
            if (File.Exists(binPath))
            {
                res = LaunchCommandLineApp(binPath, "remove tap0901");
                Thread.Sleep(20000);
            }
        }
    }

    }
