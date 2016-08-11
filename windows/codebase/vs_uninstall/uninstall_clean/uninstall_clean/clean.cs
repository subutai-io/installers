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

            var SubutaiDir = get_env_var("Subutai");
            
            remove_fw_rules(SubutaiDir);
            string mess = stop_process("p2p");
            mess = "";
            mess = stop_service("Subutai Social P2P", 5000);
            mess = "";
            mess = remove_service("Subutai Social P2P");
            mess = "";
            mess = stop_process("SubutaiTray");
            mess = stop_process("SubutaiTray");
            mess = "";
            delete_Shortcuts("Subutai");
            //remove_vm();
            
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
                
                if (mess.Contains("Can not"))
                {
                    MessageBox.Show($"Folder {SubutaiDir} can not be removed. Please delete it manually",
                        "Removing Subutai folder", MessageBoxButtons.OK);
                }
            }

            mess = remove_home(SubutaiDir);

            mess = remove_env();
 
            Environment.SetEnvironmentVariable("Subutai", "", EnvironmentVariableTarget.Machine);
            Environment.SetEnvironmentVariable("Subutai", "", EnvironmentVariableTarget.User);
            Environment.SetEnvironmentVariable("Subutai", "", EnvironmentVariableTarget.Process);

            delete_from_reg();
            progressBar1.Visible = false;

            //Find if registry cleaner exists 

            string rclean_path = logDir();
            
            del_TAP();
            remove_vm();
            MessageBox.Show("Subutai Social uninstalled", "Information", MessageBoxButtons.OK);

            if (rclean_path != "")
            {
                rclean_path = Path.Combine(rclean_path, "subutai-clean-registry.reg");

                var startInfo = new ProcessStartInfo
                {
                    CreateNoWindow = false,
                    UseShellExecute = true,
                    FileName = "regedit.exe",
                    WindowStyle = ProcessWindowStyle.Normal,
                    Arguments = $" /s {rclean_path}"
                };

                Process.Start(startInfo);
                //LaunchCommandLineApp("regedit.exe", $"/s {rclean_path}");
            }
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
            string mess = "";

            if (ctl == null)
            {
                mess = "Not installed";
            } else
            {
                mess = LaunchCommandLineApp("nssm", $"remove \"Subutai Social P2P\" confirm");
            }
        return (mess);
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
            if (outputVms.Contains("Error"))
            {
                return;
            }
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
                                string res1 = LaunchCommandLineApp("vboxmanage", $"controlvm {vmName} poweroff ");
                                Thread.Sleep(5000);
                                string res2 = LaunchCommandLineApp("vboxmanage", $"unregistervm  --delete {vmName}");
                                Thread.Sleep(5000);
                            }
                        }
                    }
                }
            }
        }

        private string LaunchCommandLineApp(string filename, string arguments )
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
                //LaunchCommandLineApp(filename, arguments);
            }
            return ($"1|{filename} was not executed|Error");
        }

        private void delete_from_reg()
        {
            //user environment
            string SubutaiProdID = @"CF66AAA126027D4479D5BB7808A6CDA7";
            string InstallerProdID = @"{1AAA66FC-2062-44D7-975D-BB87806ADC7A}";
            label1.Text = "Cleanig Registry";
            string subkey; 
            RegistryKey rk;

            //HKEY_CLASSES_ROOT\Installer\Products\CF66AAA126027D4479D5BB7808A6CDA7
            subkey = "Installer\\Products";
            DeleteSubKeyTree(SubutaiProdID, subkey, RegistryHive.ClassesRoot);

            //HKEY_LOCAL_MACHINE\SOFTWARE\Classes\Installer\Products\CF66AAA126027D4479D5BB7808A6CDA7
            subkey = "SOFTWARE\\Classes\\Installer\\Products";
            DeleteSubKeyTree(SubutaiProdID, subkey, RegistryHive.LocalMachine);

            //HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\Folders
            subkey = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Installer\\Folders";
            //DeleteValueFound(subkey, "Subutai", RegistryHive.LocalMachine);

            //HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\UserData\S - 1 - 5 - 18\Components\001B050B63BD23B49988FFEB639D2F61
            //Components
            subkey = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Installer\\UserData\\S-1-5-18\\Components";
            //DeleteSubKeyFound(subkey, "Subutai", RegistryHive.LocalMachine);

            //********************************************************
            //HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\UserData\S-1-5-18\Products\CF66AAA126027D4479D5BB7808A6CDA7
            //HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\UserData\S-1-5-18\Products\CF66AAA126027D4479D5BB7808A6CDA7

            //var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            subkey = @"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Installer\\UserData\\S-1-5-18\\Products";
            DeleteSubKeyTree(SubutaiProdID, subkey, RegistryHive.LocalMachine);

             //HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{ 1AAA66FC - 2062 - 44D7 - 975D - BB87806ADC7A}
            subkey = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall";
            DeleteSubKeyTree(SubutaiProdID, subkey, RegistryHive.LocalMachine);

            //HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Subutai 4.0.07
            //Uninstalling version
            //HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Subutai 4.0.2

            subkey = "SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Uninstall";
            //DeleteSubKeyFound(subkey, "Subutai", RegistryHive.LocalMachine);

            //HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Caphyon\Advanced Installer\LZMA\{ 1AAA66FC - 2062 - 44D7 - 975D - BB87806ADC7A}
            subkey = "SOFTWARE\\Wow6432Node\\Caphyon\\Advanced Installer\\LZMA";
            DeleteSubKeyTree(SubutaiProdID, subkey, RegistryHive.LocalMachine);

            //HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Subutai Social
            subkey = "SOFTWARE\\Wow6432Node";
            DeleteSubKeyTree("Subutai Social", subkey, RegistryHive.LocalMachine);

            //HKEY_LOCAL_MACHINE\SYSTEM\ControlSet001\Control\Session Manager\Environment
            //Path, Subutai
            subkey = "SYSTEM\\ControlSet001\\Control\\Session Manager\\Environment";
            DeleteValueFound(subkey,"Subutai", RegistryHive.LocalMachine);
 
            //HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Environment
            subkey = "SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Environment";
            DeleteSubKeyTree("Subutai", subkey, RegistryHive.LocalMachine);
 
            //HKEY_LOCAL_MACHINE\SYSTEM\VritualRoot\MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\UserData\S - 1 - 5 - 18\Components\336320F1CCE3E3F45A57FD0D4E46AB34
            subkey = "SYSTEM\\VritualRoot\\MACHINE\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Installer\\UserData\\S-1-5-18\\Components";
            DeleteSubKeyFound(subkey, "Subutai", RegistryHive.LocalMachine);

            //HKEY_LOCAL_MACHINE\SYSTEM\VritualRoot\MACHINE\SOFTWARE\Wow6432Node\Caphyon\Advanced Installer\LZMA
            subkey = "SYSTEM\\VritualRoot\\MACHINE\\SOFTWARE\\Wow6432Node\\Caphyon\\Advanced Installer\\LZMA";
            DeleteSubKeyTree(InstallerProdID, subkey, RegistryHive.LocalMachine);
 
            subkey = "SYSTEM\\VritualRoot\\MACHINE\\SOFTWARE\\Wow6432Node";
            DeleteSubKeyTree("Subutai Social", subkey, RegistryHive.LocalMachine);
 
            subkey = "SOFTWARE\\Optimal-dynamics";
            DeleteSubKeyTree("SS_Tray", subkey, RegistryHive.LocalMachine);

            subkey = "SOFTWARE";
            rk = Registry.CurrentUser.OpenSubKey(subkey, true);
            DeleteSubKeyTree("Optimal-dynamics", subkey, RegistryHive.LocalMachine);

            subkey = "Environment";
            DeleteSubKeyTree("Subutai", subkey, RegistryHive.LocalMachine);

        }

        public bool DeleteKey(string KeyName, ref RegistryKey baseKey)
        {
            try
            {
                // Setting
                RegistryKey sk1 = baseKey.OpenSubKey(KeyName);
                // If the RegistrySubKey doesn't exists -> (true)
                if (baseKey == null)
                {
                    return true;
                }
                else
                {
                    baseKey.DeleteValue(KeyName);
                    baseKey.Close();
                    return true;
                }
            }
            catch (Exception e)
            {
                string res = e.Message;
                return false;
            }
        }

        public bool DeleteSubKeyTree(string KeyName, string KeyPath, RegistryHive rh)
        {
            var  baseKey = RegistryKey.OpenBaseKey(rh, RegistryView.Registry64);
            RegistryKey rk = baseKey.OpenSubKey(KeyPath, true);
            if (rk != null)
            {
               try
                {
                    rk.DeleteSubKeyTree(KeyName);
                    rk.Close();
                    baseKey.Close();
                    return true;
                }
                catch (Exception e)
                {
                    string res = e.Message;
                    rk.Close();
                    baseKey.Close();
                    return false;
                }
            }
           
            baseKey.Close();
            return false;
        }

        public bool DeleteSubKeyTree(string KeyName, ref RegistryKey rk)
        {
            //var baseKey = RegistryKey.OpenBaseKey(rh, RegistryView.Registry64);
            //RegistryKey rk = baseKey.OpenSubKey(KeyPath, true);
            if (rk != null)
            {
                try
                {
                    rk.DeleteSubKeyTree(KeyName);
                    rk.Close();
                    return true;
                }
                catch (Exception e)
                {
                    string res = e.Message;
                    rk.Close();
                    return false;
                }
            }
            return false;
        }

        public void DeleteSubKeyFound(string subkey, string str_2_find, RegistryHive rh)
        {
            var baseKey = RegistryKey.OpenBaseKey(rh, RegistryView.Registry64);
            RegistryKey rk = baseKey.OpenSubKey(subkey, true);
            //RegistryKey rk = Registry.LocalMachine.OpenSubKey(subkey, true);//Components
            
            if (rk != null)
            {
                foreach (var rsk in rk.GetSubKeyNames()) //Product
                {
                    RegistryKey productKey = rk.OpenSubKey(rsk, true);
                    string rsk_path = Path.Combine(subkey, rsk);
                    if (productKey != null)
                    {
                        foreach (var vname in productKey.GetValueNames())
                        {
                            string kvalue = Convert.ToString(productKey.GetValue(vname));
                            if (kvalue.Contains(str_2_find))
                            {
                                DeleteSubKeyTree(rsk, ref productKey);
                                //DeleteSubKeyTree(rsk, rsk_pat, rh);
                                //logger.Info("Delete subkey {0}", subkey);
                            }
                        }
                    }
                    productKey.Close();
                }
            }
           
            baseKey.Close();
        }

        public void DeleteValueFound(string subkey, string str_2_find, RegistryHive rh)
        {
            var baseKey = RegistryKey.OpenBaseKey(rh, RegistryView.Registry64);
            RegistryKey rk = baseKey.OpenSubKey(subkey, true);//Components
            if (rk != null)
            {
                foreach (var vname in rk.GetValueNames())
                {
                    string kvalue = Convert.ToString(rk.GetValue(vname));
                    if (kvalue.Contains(str_2_find) && !vname.Contains("Path"))
                    {
                        rk.DeleteValue(vname);
                    }
                }
                rk.Close();
            }
            baseKey.Close();
        }

        public static string remove_env()
        {
            //logger.Info("Remove env");

            string strPath = get_env_var("Path");
            //Environment.GetEnvironmentVariable("Path");
            string strSubutai = get_env_var("Subutai");
            //Environment.GetEnvironmentVariable("Subutai");

            if (strPath == null || strPath == "")
                return "Path Empty";

            if (strSubutai == null || strSubutai == "")
                return "Subutai Empty";

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
            Environment.SetEnvironmentVariable("Path", newPath, EnvironmentVariableTarget.Process);

            return newPath;
        }

        public void remove_fw_rules(string appdir)
        {
            LaunchCommandLineApp("netsh", " advfirewall firewall delete rule name=all service=\"Subutai Social P2P\"");
            LaunchCommandLineApp("netsh", $" advfirewall firewall delete rule name=all program=\"{appdir}bin\\p2p.exe\"");
            LaunchCommandLineApp("netsh", $" advfirewall firewall delete rule name=all program=\"{appdir}bin\\tray\\SubutaiTray.exe\"");

            //LaunchCommandLineApp("netsh", $" advfirewall firewall delete rule name=\"vboxheadless_in\"");
            //LaunchCommandLineApp("netsh", $" advfirewall firewall delete rule name=\"vboxheadless_out\"");

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
            string binPath = Path.Combine(sysDrive, "Program Files",
            "TAP-Windows", "bin", "tapinstall.exe");
            string res = "";
            if (File.Exists(binPath))
            {
                res = LaunchCommandLineApp(binPath, "remove tap0901");
                Thread.Sleep(20000);
            }
        }

        public static string remove_home(string instDir)
        {
            if (instDir == "")
            {
                return "Not exists";
            }
            string path_l = Path.Combine(instDir, "home");
            
            if (Directory.Exists(path_l))
            {
                try
                {
                    Directory.Delete(path_l);
                    return "OK";
                }
                catch (Exception ex)
                {
                    return ("Error: " + ex.Message);
                }
            }
            return path_l;
        }

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
    }
}
