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
        public static string sysDrive = "";
        public clean()
        {
            InitializeComponent();
            progressBar1.Visible = true;
            label2.Visible = false;
            label1.Text = "Removing Subutai Social";
        }

        private void clean_Load(object sender, EventArgs e)
        {
            string sysPath = Environment.GetFolderPath(Environment.SpecialFolder.System);
            sysDrive = Path.GetPathRoot(sysPath);
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
            //Remove Subutai dir from ApplicationData
            string appUserDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            //MessageBox.Show($"AppData: {appUserDir}", "AppData", MessageBoxButtons.OK);
            appUserDir = Path.Combine(appUserDir, "Subutai Social");
            //MessageBox.Show($"Subutai Social: {appUserDir}", "Subutai Social", MessageBoxButtons.OK);
            mess = delete_dir(appUserDir);
            //Remove /home shortcut
            mess = remove_home(SubutaiDir);
            //Remove Subutai dirs from Path
            mess = remove_from_Path("Subutai");
            //Save Path
            Environment.SetEnvironmentVariable("Subutai", "", EnvironmentVariableTarget.Machine);
            Environment.SetEnvironmentVariable("Subutai", "", EnvironmentVariableTarget.User);
            Environment.SetEnvironmentVariable("Subutai", "", EnvironmentVariableTarget.Process);
            //Clean registry
            delete_from_reg();
            progressBar1.Visible = false;
       
            //Remove TAP interfaces and uninstall TAP
            del_TAP();
            //Remove snappy and subutai machines
            remove_vm();
            //Remove Oracle VirtualBox
//            remove_app_vbox("Oracle VirtualBox");
            //Remove log dir
            remove_log_dir();
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
            string mess = "";

            if (ctl == null)
            {
                mess = "Not installed";
            } else
            {
                mess = LaunchCommandLineApp("nssm", $"remove \"Subutai Social P2P\" confirm", true, false);
            }
        return (mess);
        }

        private string stop_process(string procName)
        {
            Process[] processes = Process.GetProcessesByName(procName);
            foreach (Process process in processes)
            {
                try
                {
                    label1.Text = "Stopping " + procName + " process";
                    process.Kill();
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
            return "1";
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

        public static void delete_Shortcut(string shPath, string aName, Boolean isDir)
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
                    Directory.Delete(fullname, true);
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
            string outputVms = LaunchCommandLineApp("vboxmanage", $"list vms", true, false);
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
                                string res1 = LaunchCommandLineApp("vboxmanage", $"controlvm {vmName} poweroff ", true, false);
                                Thread.Sleep(5000);
                                string res2 = LaunchCommandLineApp("vboxmanage", $"unregistervm  --delete {vmName}", true, false);
                                Thread.Sleep(5000);
                            }
                        }
                    }
                }
            }
        }

        private string LaunchCommandLineApp(string filename, string arguments, bool bCrNoWin, bool bUseShExe )
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
            DeleteValueFound(subkey, "Subutai", RegistryHive.LocalMachine);

            //HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\UserData\S - 1 - 5 - 18\Components\001B050B63BD23B49988FFEB639D2F61
            //Components
            subkey = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Installer\\UserData\\S-1-5-18\\Components";
            DeleteSubKeyFound(subkey, "Subutai", RegistryHive.LocalMachine);

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
            //Uninstalling version key is CLOSED
            //HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Subutai 4.0.2

            subkey = "SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Uninstall";
            DeleteSubKeyFound(subkey, "Subutai", RegistryHive.LocalMachine);

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

            //HKEY_CURRENT_USER\Software\Optimal-dynamics
            //HKEY_CURRENT_USER\Software\Optimal - dynamics\SS_Tray

            //subkey = "Software\\Optimal-dynamics";
            //DeleteSubKeyTree("SS_Tray", subkey, RegistryHive.CurrentUser);

            subkey = "Software";
            rk = Registry.CurrentUser.OpenSubKey(subkey, true);
            DeleteSubKeyTree("Optimal-dynamics", subkey, RegistryHive.CurrentUser);

            //HKEY_CURRENT_USER\Environment
            subkey = "Environment";
            DeleteSubKeyTree("Subutai", subkey, RegistryHive.CurrentUser);
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

        public static bool DeleteSubKeyTree(string KeyName, string KeyPath, RegistryHive rh)
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
            return false;
        }

        public static bool DeleteSubKeyTree(string KeyName, ref RegistryKey rk)
        {
            //var baseKey = RegistryKey.OpenBaseKey(rh, RegistryView.Registry64);
            //RegistryKey rk = baseKey.OpenSubKey(KeyPath, true);
            if (rk != null)
            {
                try
                {
                    rk.DeleteSubKeyTree(KeyName);
                    //rk.Close();
                    return true;
                }
                catch (Exception e)
                {
                    string res = e.Message;
                    //rk.Close();
                    return false;
                }
            }
            return false;
        }

        public static void DeleteSubKeyFound(string subkey, string str_2_find, RegistryHive rh)
        {
            var baseKey = RegistryKey.OpenBaseKey(rh, RegistryView.Registry64);
            RegistryKey rk = baseKey.OpenSubKey(subkey, true);
            //RegistryKey rk = Registry.LocalMachine.OpenSubKey(subkey, true);//Components
            
            if (rk != null)
            {
                foreach (var rsk in rk.GetSubKeyNames()) //Product
                {
                    RegistryKey productKey;
                    try
                    {
                        productKey = rk.OpenSubKey(rsk, true);
                    }
                    catch (Exception e)
                    {
                        string res = e.Message;
                        continue;
                    }
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
                rk.Close();
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
                        MessageBox.Show($"Vname: {vname} = {kvalue}", "DeleteValue", MessageBoxButtons.OK);
                        rk.DeleteValue(vname);
                    }
                }
                rk.Close();
            }
            baseKey.Close();
        }

        public static string remove_from_Path(string str2delete)
        {
            //logger.Info("Remove env");

            string strPath = get_env_var("Path");
            //Environment.GetEnvironmentVariable("Path");
            if (str2delete == "Subutai")
            {
                str2delete = get_env_var("Subutai");
                //Environment.GetEnvironmentVariable("Subutai");
            }
            
            if (strPath == null || strPath == "")
                return "Path Empty";

            if (str2delete == null || str2delete == "")
                return $"{str2delete} Empty";

            string[] strP = strPath.Split(';');
            List<string> lPath = new List<string>();

            foreach (string sP in strP)
            {
                if (!lPath.Contains(sP) && !sP.Contains(str2delete))
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
            LaunchCommandLineApp("netsh", " advfirewall firewall delete rule name=all service=\"Subutai Social P2P\"", true, false);
            LaunchCommandLineApp("netsh", $" advfirewall firewall delete rule name=all program=\"{appdir}bin\\p2p.exe\"", true, false);
            LaunchCommandLineApp("netsh", $" advfirewall firewall delete rule name=all program=\"{appdir}bin\\tray\\SubutaiTray.exe\"", true, false);

            LaunchCommandLineApp("netsh", $" advfirewall firewall delete rule name=\"vboxheadless_in\"", true, false);
            LaunchCommandLineApp("netsh", $" advfirewall firewall delete rule name=\"vboxheadless_out\"", true, false);

            LaunchCommandLineApp("netsh", $" advfirewall firewall delete rule name=\"virtualbox_in\"", true, false);
            LaunchCommandLineApp("netsh", $" advfirewall firewall delete rule name=\"virtualbox_out\"", true, false);
        }

        public static string logDir()
        {
            
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
            string sysPath = Environment.GetFolderPath(Environment.SpecialFolder.System);
            sysDrive = Path.GetPathRoot(sysPath);
            string binPath = Path.Combine(sysDrive, "Program Files", "TAP-Windows", "bin", "tapinstall.exe");
            string res = "";
            if (File.Exists(binPath))
            {
                res = LaunchCommandLineApp(binPath, "remove tap0901", true, false);
            }
            binPath = Path.Combine(sysDrive, "Program Files", "TAP-Windows", "Uninstall.exe");
            string pathPath = Path.Combine(sysDrive, "Program Files", "TAP-Windows", "bin"); 
            remove_app("TAP-Windows", binPath, "/S", "TAP-Windows");
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

        public void remove_app(string app_name, string cmd, string args, string app_path)
        {
            DialogResult drs = MessageBox.Show($"Remove {app_name}?", $"Removing {app_name}",
                               MessageBoxButtons.YesNo,
                               MessageBoxIcon.Question,
                               MessageBoxDefaultButton.Button1);

            if (drs == DialogResult.No)
                return;

            string mess = "";
            if (File.Exists(cmd))
            {
                string res = LaunchCommandLineApp(cmd, args, false, false);
                if (res.Contains("|Error"))
                {
                    mess = $"{app_name} was not removed, please uninstall manually";
                }
                else
                {
                    mess = $"{app_name} uninstalled";
                }
            } else
            {
                    mess = $"Probably {app_name} was not installed, please check and uninstall manually";
            }
            MessageBox.Show(mess, $"Uninstalling {app_name}", MessageBoxButtons.OK);
            if (app_path !="" || app_path != null)
            {
                remove_from_Path(app_path);
            }           
         }

 

        public void remove_log_dir()
        {
            //Find if log directory exists 
            string lDir = logDir();
            if (lDir == "")
                return;

            string today = $"{ DateTime.Now.ToString("yyyy-MM-dd")}";
            DirectoryInfo di = new DirectoryInfo(lDir);
            foreach (FileInfo fi in di.GetFiles())
            {
                if (!fi.Name.ToLower().Contains(today) && !fi.Name.ToLower().Contains(".reg"))
                {
                    //MessageBox.Show($"File: {fi.Name}", "Removing file from drivers", MessageBoxButtons.OK);
                    try
                    {
                        fi.Delete();
                    }
                    catch (Exception e)
                    {
                        string res = e.Message;
                    }
                }
            }
        }
    }
}
