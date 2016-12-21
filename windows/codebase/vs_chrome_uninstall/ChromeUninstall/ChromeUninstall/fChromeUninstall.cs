using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Microsoft.Win32;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;



namespace ChromeUninstall
{
    public partial class fChromeUninstall : Form
    {
        public fChromeUninstall()
        {
            InitializeComponent();
            progressBar1.Visible = true;
            remove_chrome();
        }

        public static void remove_chrome()
        {
            string msg = string.Format("Remove Google Chrome browser? \n\nNOTE: It's prefferable to delete Chrome from Control Panel->Programs.");
            DialogResult drs = MessageBox.Show(msg, $"Removing Google Chrome",
                                   MessageBoxButtons.YesNo,
                                   MessageBoxIcon.Question,
                                   MessageBoxDefaultButton.Button1);

            if (drs == DialogResult.No)
                    Environment.Exit(0);

            Task.Factory.StartNew(() =>
            {
                //StageReporter("", "Starting uninstall");
                SetIndeterminate(true);
            })

              .ContinueWith((prevTask) =>
              {
                  Exception ex = prevTask.Exception;
                  if (prevTask.IsFaulted)
                  {
                      //prepare-vbox faulted with exception
                      while (ex is AggregateException && ex.InnerException != null)
                      {
                          ex = ex.InnerException;
                      }
                      MessageBox.Show(ex.Message, "Start", MessageBoxButtons.OK);
                      //throw new InvalidOperationException();
                  }
                  //Check if Chrome is running
                  stop_process("chrome.exe");
                  stop_process("Google Chrome");
                  stop_process("Google Chrome (32 bit)");
                  //Unpin from taskbar

                  rg_clean_chrome();
                  fd_clean_chrome();
                  Program.form1.Invoke((MethodInvoker)delegate
                  {
                      Program.form1.Close();
                  });
              });
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

        /// <summary>
        /// Clean Registry fron Chrome entries
        /// </summary>
        public static void rg_clean_chrome()
        {
            string chromeGUID = "{8A69D345-D564-463c-AFF1-A69D9E530F96}"; //default GUID
            string chromeGUIDbinaries = "{4DC8B4CA-1BDA-483e-B5FA-D3C12E15B62D}"; //default GUID
            string subkey = "";
            string key = "";
            string vname = "";

            //Define Chrome-related GUIDs: "Google Chrome" and "Google Chrome binaries"
            key = "Software\\Wow6432Node\\Google\\Update\\Clients";
            Dictionary<string, string> guids = define_GUID_by_name(key, "name", "Google Chrome", RegistryHive.LocalMachine, true);
            if (guids.Count > 0)
            {
                chromeGUID = guids["Google Chrome"];
                //MessageBox.Show($"Chrome GUID {chromeGUID}", "Chrome GUID", MessageBoxButtons.OK);
                chromeGUIDbinaries = guids["Google Chrome binaries"];
                //MessageBox.Show($"Chrome binaries GUID {chromeGUIDbinaries}", "Chrome GUID", MessageBoxButtons.OK);
            }

            //[-HKEY_LOCAL_MACHINE\SOFTWARE\Classes\ChromeHTML]
            subkey = "ChromeHTML";
            key = "SOFTWARE\\Classes";
            DeleteSubKeyTree(subkey, key, RegistryHive.LocalMachine);
            //MessageBox.Show("Deleted 1 ChromeHTML", "HKEY_LOCAL_MACHINE\\SOFTWARE\\Classes\\ChromeHTML", MessageBoxButtons.OK);

            //[-HKEY_LOCAL_MACHINE\SOFTWARE\Clients\StartMenuInternet\chrome.exe]
            //HKEY_LOCAL_MACHINE\SOFTWARE\Clients\StartMenuInternet\Google Chrome
            subkey = "Google Chrome";
            key = "SOFTWARE\\Clients\\StartMenuInternet";
            DeleteSubKeyTree(subkey, key, RegistryHive.LocalMachine);
            //MessageBox.Show("Deleted 2 StartMenuInternet", "HKLM\\SOFTWARE\\Clients\\StartMenuInternet\\Google Chrome", MessageBoxButtons.OK);

            //[HKEY_LOCAL_MACHINE\SOFTWARE\RegisteredApplications]
            //"Google Chrome"=-
            vname = "Google Chrome";
            key = "SOFTWARE\\RegisteredApplications";
            DeleteValueByName(key, vname, RegistryHive.LocalMachine);
            //MessageBox.Show("Deleted RegisteredApplications", "HKLM\\SOFTWARE\\RegisteredApplications\\Google Chrome", MessageBoxButtons.OK);

            //[-HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Uninstall\Chrome]
            subkey = "Chrome";
            key = "Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall";
            DeleteKeyByValue(key, "Chrome", RegistryHive.CurrentUser);

            //HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\
            subkey = "Chrome";
            key = "Software\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Uninstall";
            DeleteKeyByValue(key, "Chrome", RegistryHive.LocalMachine);

            //HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Google Chrome, Update
            //Deleteng "Google Chrome" and "Google Chrome binaries"
            foreach (var dkey in guids.Keys)
            {
                //HKLM\software\wow6432node\google\update
                //x64
                subkey = guids[dkey];
                //"{8A69D345-D564-463c-AFF1-A69D9E530F96}";
                key = "Software\\Wow6432Node\\Google\\Update\\Clients";
                DeleteSubKeyTree(subkey, key, RegistryHive.LocalMachine);

                key = "Software\\Wow6432Node\\Google\\Update\\ClientState";
                DeleteSubKeyTree(subkey, key, RegistryHive.LocalMachine);

                key = "Software\\Wow6432Node\\Google\\Update\\ClientStateMedium";
                DeleteSubKeyTree(subkey, key, RegistryHive.LocalMachine);

                //x86
                //HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Google Chrome, Update
                key = "Software\\Google\\Update\\Clients";
                DeleteSubKeyTree(subkey, key, RegistryHive.LocalMachine);

                key = "Software\\Google\\Update\\ClientState";
                DeleteSubKeyTree(subkey, key, RegistryHive.LocalMachine);

                key = "Software\\Google\\Update\\ClientStateMedium";
                DeleteSubKeyTree(subkey, key, RegistryHive.LocalMachine);

                subkey = "Chrome";
                //x64
                key = "Software\\Wow6432Node\\Google";
                DeleteSubKeyTree(subkey, key, RegistryHive.LocalMachine);
                //x86
                key = "Software\\Google";
                DeleteSubKeyTree(subkey, key, RegistryHive.LocalMachine);

            }

            //HKEY_CURRENT_USER\SOFTWARE\Wow6432Node\Google Chrome, Update
            //Deleteng "Google Chrome" and "Google Chrome binaries"
            foreach (var dkey in guids.Keys)
            {
                subkey = guids[dkey];
                //[-HKEY_CURRENT_USER\Software\Google\Update\Clients\{8A69D345-D564-463c-AFF1-A69D9E530F96}] No such key
                key = "Software\\Google\\Update\\Clients";
                DeleteSubKeyTree(subkey, key, RegistryHive.CurrentUser);

                //[-HKEY_CURRENT_USER\Software\Google\Update\ClientState\{8A69D345-D564-463c-AFF1-A69D9E530F96}]
                key = "Software\\Google\\Update\\ClientState";
                DeleteSubKeyTree(subkey, key, RegistryHive.CurrentUser);

                subkey = "Chrome";
                //x64
                key = "Software\\Wow6432Node\\Google";
                DeleteSubKeyTree(subkey, key, RegistryHive.CurrentUser);
                //x86
                key = "Software\\Google";
                DeleteSubKeyTree(subkey, key, RegistryHive.CurrentUser);

            }

        }

        /// <summary>
        ///Delete key and all subkeys.
        /// </summary>
        /// <param name="KeyName">Name of the subkey.</param>
        /// <param name="KeyPath">The parent key path.</param>
        /// <param name="rh">Registry Hive.</param>
        /// <returns></returns>
        public static bool DeleteSubKeyTree(string KeyName, string KeyPath, RegistryHive rh)
        {
            var baseKey = RegistryKey.OpenBaseKey(rh, RegistryView.Registry64);
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

        /// <summary>
        /// Delete key by value.
        /// Delete child subkey tree if substring was found in child key values
        /// </summary>
        /// <param name="subkey">The subkey name.</param>
        /// <param name="str_2_find">The string to be loog for.</param>
        /// <param name="rh">Registry Hive.</param>
        public static void DeleteKeyByValue(string subkey, string str_2_find, RegistryHive rh)
        {
            var baseKey = RegistryKey.OpenBaseKey(rh, RegistryView.Registry64);
            RegistryKey rk = baseKey;
            if (subkey != "")
            {
                rk = baseKey.OpenSubKey(subkey, true);
            }

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
                    //string rsk_path = Path.Combine(subkey, rsk);
                    if (productKey != null)
                    {
                        foreach (var vname in productKey.GetValueNames())
                        {
                            string kvalue = Convert.ToString(productKey.GetValue(vname));
                            if (kvalue.Contains(str_2_find))
                            {
                                //DeleteSubKeyTree(rsk, ref productKey);
                                rk.DeleteSubKeyTree(rsk);
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

        /// <summary>
        /// Delete the variable by name.
        /// </summary>
        /// <param name="subkey">The subkey.</param>
        /// <param name="vname">Value name.</param>
        /// <param name="rh">The rh.</param>
        public static void DeleteValueByName(string subkey, string vname, RegistryHive rh)
        {
            var baseKey = RegistryKey.OpenBaseKey(rh, RegistryView.Registry64);
            RegistryKey rk = baseKey.OpenSubKey(subkey, true);//Components
            if (rk != null)
            {
                try
                {
                    rk.DeleteValue(vname);
                }
                catch (Exception ex)
                {
                    string res = ex.Message; //for debugging
                }

            }
            rk.Close();
            baseKey.Close();
        }

        /// <summary>
        /// Defines the name of the unique identifier by app name.
        /// </summary>
        /// <param name="key">The key where to look.</param>
        /// <param name="vname2check">The vname2check - name of variable containing app name .</param>
        /// <param name="name2find">The name2find.</param>
        /// <param name="rh">Registry Hive.</param>
        /// <param name="isSubstring">if set to <c>true</c> [is substring] - will look for substrings.</param>
        /// <returns></returns>
        public static Dictionary<string, string> define_GUID_by_name(string key,
            string vname2check,
            string name2find,
            RegistryHive rh,
            bool isSubstring)
        {
            var baseKey = RegistryKey.OpenBaseKey(rh, RegistryView.Registry64);
            RegistryKey rk = baseKey.OpenSubKey(key, true);//Components
            Dictionary<string, string> dguids = new Dictionary<string, string>();
            if (rk != null)
            {
                foreach (var skey in rk.GetSubKeyNames()) //Product
                {
                    RegistryKey rsk = rk.OpenSubKey(skey, true);
                    if (rsk != null)
                    {
                        string name_value = Convert.ToString(rsk.GetValue(vname2check));
                        if (isSubstring)
                        {
                            if (name_value.Contains(name2find))
                            {
                                dguids.Add(name_value, skey);
                            }
                        }
                        else
                        {
                            if (name_value.Equals(name2find))
                            {
                                dguids.Add(name_value, skey);
                            }
                        }
                        rsk.Close();
                    }
                }
                rk.Close();
            }
            baseKey.Close();
            //if (dguids.Count == 0)
            //{
            //    dguids.Add("null","null");
            //} 
            return dguids;
        }

        /// <summary>
        /// Removes Chrome directories and shortcuts.
        /// </summary>
        public static void fd_clean_chrome()
        {

            var dirApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var dirPath = Path.Combine(dirApp, "Google", "Chrome");
            ////Deleting user's app folder
            //try
            //{
            //    if (Directory.Exists(dirPath))
            //        Directory.Delete(dirPath, true);

            //    //Thread.Sleep(5000);
            //    //return "0";
            //}
            //catch (Exception ex)
            //{

            //    MessageBox.Show("Can not delete folder " + dirPath + ". " + ex.Message.ToString());
            //}

            //Deleting from Program Files
            dirApp = Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFiles);
            dirPath = Path.Combine(dirApp, "Google", "Chrome");
            //Deleting user's app folder
            try
            {
                if (Directory.Exists(dirPath))
                    Directory.Delete(dirPath, true);

                //Thread.Sleep(5000);
                //return "0";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Can not delete folder " + dirPath + ". " + ex.Message.ToString());
            }

            //Deleting from Program Files
            dirApp = Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFilesX86);
            dirPath = Path.Combine(dirApp, "Google", "Chrome");
            //Deleting user's app folder
            try
            {
                if (Directory.Exists(dirPath))
                    Directory.Delete(dirPath, true);

                //Thread.Sleep(5000);
                //return "0";
            }
            catch (Exception ex)
            {

                MessageBox.Show("Can not delete folder " + dirPath + ". " + ex.Message.ToString());
            }

            //C:\ProgramData\Microsoft\Windows\Start Menu\Programs
            //C: \Users\Public\Desktop
            //Commom Desktop
            var shcutPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory);
            delete_Shortcut(shcutPath, "Google Chrome", false);
            //MessageBox.Show($"shcut {shcutPath} cleaned", "Chrome shortcuts", MessageBoxButtons.OK);

            //Common StartMenu/Programs
            //This path to Common Start Menu will be used later
            shcutPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu);
            shcutPath = Path.Combine(shcutPath, "Programs");
            delete_Shortcut(shcutPath, "Google Chrome", false);
            //MessageBox.Show($"shcut {shcutPath} cleaned", "Chrome shortcuts", MessageBoxButtons.OK);
        }

        /// <summary>
        /// Deletes  shortcut.
        /// </summary>
        /// <param name="shPath">The shoertcut path.</param>
        /// <param name="aName">Application name</param>
        /// <param name="isDir">if shortcut is directory: <c>true</c> [is dir].</param>
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
            }
        }

        public static void SetIndeterminate(bool isIndeterminate)
        {
            if (Program.form1.progressBar1.InvokeRequired)
            {
                Program.form1.progressBar1.BeginInvoke(
                    new Action(() =>
                    {
                        if (isIndeterminate)
                        {
                            Program.form1.progressBar1.Style = ProgressBarStyle.Marquee;
                        }
                        else
                        {
                            Program.form1.progressBar1.Style = ProgressBarStyle.Blocks;
                        }
                    }
                ));
            }
            else
            {
                if (isIndeterminate)
                {
                    Program.form1.progressBar1.Style = ProgressBarStyle.Marquee;
                }
                else
                {
                    Program.form1.progressBar1.Style = ProgressBarStyle.Blocks;
                }
            }
        }
    }
}
