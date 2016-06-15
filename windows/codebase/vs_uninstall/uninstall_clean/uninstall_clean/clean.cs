using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        public clean()
        {
            InitializeComponent();
            progressBar1.Visible = true;
            label2.Visible = false;
            label1.Text = "Removing Subutai Social";
        }

        private void clean_Load(object sender, EventArgs e)
        {
            //timer1.Enabled = true;
            //timer1.Start();
            //timer1.Interval = 1000;
            //progressBar1.Maximum = 10;

            //timer1.Tick += new EventHandler(timer1_Tick);
            clean_all();

            //remove_vm();
            //delete_from_reg();
            //remove_env();
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
            string mess = stop_service("Subutai Social P2P", 5000);
            //MessageBox.Show(mess + " Service was not running.", "Stopping P2P service", MessageBoxButtons.OK);

            mess = stop_process("SubutaiTray");
            mess = stop_process("SubutaiTray");
            //MessageBox.Show(mess + " Application was not running.", "Stopping SubutaiTray", MessageBoxButtons.OK);

            var SubutaiDir = Environment.GetEnvironmentVariable("Subutai");
            //var SubutaiDir = "c:\\4delete";
            //MessageBox.Show("Subutai Social P2P service stopped. Deleting " + SubutaiDir.ToString() + " folder", "Deleting Subutai folder", MessageBoxButtons.OK);
            if (SubutaiDir != "" && SubutaiDir != null && SubutaiDir != "C:\\" && SubutaiDir != "D:\\" && SubutaiDir != "E:\\")
            {
                mess = delete_dir(SubutaiDir);
                if (mess.Contains("Can not"))
                {
                    MessageBox.Show($"Folder {SubutaiDir} can not be removed. Please delete it manually", 
                        "Attention", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }
            }
            //Environment.SetEnvironmentVariable("Subutai", null);
            delete_from_reg();
            Environment.SetEnvironmentVariable("Subutai", "");
        
            delete_Shortcuts("Subutai");
            remove_vm();
            remove_env();
            progressBar1.Visible = false;
            MessageBox.Show("Subutai Social uninstalled", "Information", MessageBoxButtons.OK);
            label2.Visible = true;
            label2.Text = " Please, close this window";
            
            Environment.Exit(0);
        }
        private string stop_service(string serviceName, int timeoutMilliseconds)
        {
            ServiceController service = new ServiceController(serviceName);
            try
            {
                //timer1.Enabled = true;
                //timer1.Start();
                //timer1.Interval = 1000;
                //progressBar1.Maximum = 10;
                TimeSpan timeout = TimeSpan.FromMilliseconds(timeoutMilliseconds);
                label1.Text = "Stopping " + serviceName + " service";
                service.Stop();
                service.WaitForStatus(ServiceControllerStatus.Stopped, timeout);
                //timer1.Stop();
                //timer1.Enabled = false;
                Thread.Sleep(2000);
                LaunchCommandLineApp("nssm", $"remove \"{serviceName}\" confirm");
                return serviceName + " removed";
            }
            catch (Exception ex)
            {
                label1.Text = "Can not stop service " + serviceName + "  " + ex.Message.ToString();
                return ex.Message.ToString();
            }

        }

        private string stop_process(string procName)
        {
            try
            {
                //timer1.Enabled = true;
                //timer1.Start();
                //timer1.Interval = 2000;
                //progressBar1.Maximum = 10;
                label1.Text = "Stopping " + procName + " process";
                Process[] proc = Process.GetProcessesByName(procName);
                proc[0].Kill();
                //timer1.Stop();
                //timer1.Enabled = false;
                Thread.Sleep(3000);
                return procName + " stopped";
            }
            catch (Exception ex)
            {
                label1.Text = "Can not stop process " + procName + ". " + ex.Message.ToString();
                return "Can not stop process " + procName + ". " + ex.Message.ToString();
            }

        }

        private string delete_dir(string dirName)
        {
            try
            {
                //timer1.Enabled = true;
                //timer1.Start();
                //timer1.Interval = 1000;
                //progressBar1.Maximum = 10;
                label1.Text = "Deleting " + dirName + " folder";
                Directory.Delete(dirName, true);
                //timer1.Stop();
                //timer1.Enabled = false;
                //deleteDirectory(dirName, true);
                Thread.Sleep(5000);
                return "Folder" + dirName + " deleted";
            }
            catch (Exception ex)
            {
                //timer1.Enabled = true;
                //timer1.Start();
                //timer1.Interval = 1000;
                //progressBar1.Maximum = 10;
                label1.Text = "Can not delete folder " + dirName + ". " + ex.Message.ToString();
                //timer1.Stop();
                //timer1.Enabled = false;
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
            label1.Text = "DCleanig Registry";
            string subkey; 
            RegistryKey rk;

            //HKEY_CLASSES_ROOT\Installer\Products\CF66AAA126027D4479D5BB7808A6CDA7
            subkey = "Installer\\Products";
            rk = Registry.ClassesRoot.OpenSubKey(subkey, true);
            if (rk != null)
                DeleteSubKeyTree("CF66AAA126027D4479D5BB7808A6CDA7", rk);

            //HKEY_LOCAL_MACHINE\SOFTWARE\Classes\Installer\Products\CF66AAA126027D4479D5BB7808A6CDA7
            subkey = "SOFTWARE\\Classes\\Installer\\Products";
            rk = Registry.ClassesRoot.OpenSubKey(subkey, true);
            if (rk != null)
                DeleteSubKeyTree("CF66AAA126027D4479D5BB7808A6CDA7", rk);

            //HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\Folders
            subkey = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Installer\\Folders";
            DeleteValueFound(subkey, "Subutai");

            //HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\UserData\S - 1 - 5 - 18\Components\001B050B63BD23B49988FFEB639D2F61
            //Components
            subkey = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Installer\\UserData\\S-1-5-18\\Components";
            DeleteSubKeyFound(subkey, "Subutai");

            //HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\UserData\S - 1 - 5 - 18\Products\CF66AAA126027D4479D5BB7808A6CDA7
            subkey = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Installer\\UserData\\S-1-5-18\\Products";
            rk = Registry.LocalMachine.OpenSubKey(subkey, true);
            if (rk != null)
                DeleteSubKeyTree("CF66AAA126027D4479D5BB7808A6CDA7", rk);//Main!!!

            //HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{ 1AAA66FC - 2062 - 44D7 - 975D - BB87806ADC7A}
            subkey = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall";
            rk = Registry.LocalMachine.OpenSubKey(subkey, true);
            if (rk != null)
                DeleteSubKeyTree("{1AAA66FC-2062-44D7-975D-BB87806ADC7A}", rk);

            //HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Caphyon\Advanced Installer\LZMA\{ 1AAA66FC - 2062 - 44D7 - 975D - BB87806ADC7A}
            subkey = "SOFTWARE\\Wow6432Node\\Caphyon\\Advanced Installer\\LZMA";
            rk = Registry.LocalMachine.OpenSubKey(subkey, true);
            if (rk != null)
                DeleteSubKeyTree("{1AAA66FC-2062-44D7-975D-BB87806ADC7A}", rk);

            //HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Subutai 4.0.07
            subkey = "SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Uninstall";
            DeleteSubKeyFound(subkey, "Subutai");

            //HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Subutai Social
            subkey = "SOFTWARE\\Wow6432Node";
            rk = Registry.LocalMachine.OpenSubKey(subkey, true);
            if (rk != null)
                 DeleteSubKeyTree("Subutai Social", rk);

            //HKEY_LOCAL_MACHINE\SYSTEM\ControlSet001\Control\Session Manager\Environment
            //Path, Subutai
            subkey = "SYSTEM\\ControlSet001\\Control\\Session Manager\\Environment";
            rk = Registry.LocalMachine.OpenSubKey(subkey, true);
            if (rk != null)
                //rk.DeleteValue("Subutai");
                DeleteSubKeyTree("Subutai", rk);

            //HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Environment
            subkey = "SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Environment";
            rk = Registry.LocalMachine.OpenSubKey(subkey, true);
            if (rk != null)
                //rk.DeleteValue("Subutai");
                DeleteSubKeyTree("Subutai", rk);

            //HKEY_LOCAL_MACHINE\SYSTEM\VritualRoot\MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\UserData\S - 1 - 5 - 18\Components\336320F1CCE3E3F45A57FD0D4E46AB34
            subkey = "SYSTEM\\VritualRoot\\MACHINE\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Installer\\UserData\\S-1-5-18\\Components";
            DeleteSubKeyFound(subkey, "Subutai");

            //HKEY_LOCAL_MACHINE\SYSTEM\VritualRoot\MACHINE\SOFTWARE\Wow6432Node\Caphyon\Advanced Installer\LZMA
            subkey = "SYSTEM\\VritualRoot\\MACHINE\\SOFTWARE\\Wow6432Node\\Caphyon\\Advanced Installer\\LZMA";
            rk = Registry.LocalMachine.OpenSubKey(subkey, true);
            if (rk != null)
                DeleteSubKeyTree("{1AAA66FC-2062-44D7-975D-BB87806ADC7A}", rk);

            subkey = "SYSTEM\\VritualRoot\\MACHINE\\SOFTWARE\\Wow6432Node";
            rk = Registry.LocalMachine.OpenSubKey(subkey, true);
            if (rk != null)
                DeleteSubKeyTree("Subutai Social", rk);
           
             
            //rk = Registry.LocalMachine.OpenSubKey(subkey, true);
            //if (rk != null)
            //    DeleteSubKeyTree("Subutai 4.0.14", rk);

            //subkey = "SYSTEM\\CurrentControlSet\\Services";
            //rk = Registry.LocalMachine.OpenSubKey(subkey, true);
            //if (rk != null)
            //    DeleteSubKeyTree("Subutai Social P2P", rk);

            subkey = "SOFTWARE\\Optimal-dynamics";
            rk = Registry.CurrentUser.OpenSubKey(subkey, true);
            if (rk != null)
                DeleteSubKeyTree("SS_Tray", rk);

            subkey = "SOFTWARE";
            rk = Registry.CurrentUser.OpenSubKey(subkey, true);
            if (rk != null)
                DeleteSubKeyTree("Optimal-dynamics", rk);

            subkey = "Environment";
            rk = Registry.CurrentUser.OpenSubKey(subkey, true);
            if (rk != null)
                DeleteSubKeyTree("Subutai", rk);
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
                    if (kvalue.Contains(str_2_find))
                    {
                        MessageBox.Show("value: " + kvalue, rk + "\\" + vname, MessageBoxButtons.OK);
                        rk.DeleteValue(vname);
                    }
                }
            }

        }


        public void remove_env()
        {
            string strPath = Environment.GetEnvironmentVariable("PATH");
            string strSubutai = Environment.GetEnvironmentVariable("Subutai");

            if (strPath == null || strPath == "")
                return;

            if (strSubutai == null || strSubutai == "")
                return;

            string[] strP = strPath.Split(';');

            foreach (string sP in strP)
            {
                if (sP.Contains(strSubutai))
                {
                    strPath = strPath.Replace(sP + ";","");
                    Environment.SetEnvironmentVariable("PATH", strPath);
                }
             }
         }

    }
}
