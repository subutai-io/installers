using System;
using Microsoft.Win32;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
//using System.Windows.Forms;

namespace uninstall_clean
{
    /// <summary>
    /// Windows REgistry cleaning
    /// </summary>
    class RG
    {
  
        /// <summary>
        /// Deletes Subutai keys and variables from Registry.
        /// </summary>
        public static void delete_from_reg()
        {
            //user environment
            string SubutaiProdName = "Subutai";
            string SubutaiProdID = "{D8AEAA94-0C20-4F7E-A106-4E9617A3D7B9}";
            string SubutaiVendor = "Subutai Social";
            string SubutaiTrayKey = "subutai";
            string SubutaiTraySubKey = "tray";
            string SubutaiE2E = "kpmiofpmlciacjblommkcinncmneeoaa";
            //label1.Text = "Cleanig Registry";
            string subkey;
            RegistryKey rk;
            //clean.SetIndeterminate(true);
            clean.UpdateProgress(0);

            //Removing main string 
            //HKCU\Software\Microsoft\Installer\Products
            subkey = "Software\\Microsoft\\Installer\\Products";
            DeleteKeyByValue(subkey, "Subutai", RegistryHive.CurrentUser);

            //HKEY_CLASSES_ROOT\Installer\Products\9990BF19B70441847BF1D18B0D97D968
            subkey = "Installer\\Installer\\Products";
            DeleteKeyByValue(subkey, "Subutai", RegistryHive.ClassesRoot);
            clean.UpdateProgress(20);

            //HKEY_LOCAL_MACHINE\SOFTWARE\Classes\Installer\Products\9990BF19B70441847BF1D18B0D97D968
            subkey = "SOFTWARE\\Classes\\Installer\\Products";
            DeleteKeyByValue(subkey, "Subutai", RegistryHive.LocalMachine);

            //Removing registration string
            //HKEY_LOCAL_MACHINE\Software\Microsoft\Windows\CurrentVersion\Uninstall
            //HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Subutai
            subkey = "SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Uninstall";
            DeleteSubKeyTree(SubutaiProdName, subkey, RegistryHive.LocalMachine);
            clean.UpdateProgress(40);

            //Removing registration string
            //HKEY_LOCAL_MACHINE\Software\Microsoft\Windows\CurrentVersion\Uninstall
            //HKEY_LOCAL_MACHINE\\SOFTWARE\\WOW6432Node\\Microsoft\\Windows\\CurrentVersion\\Uninstall\{D8AEAA94-0C20-4F7E-A106-4E9617A3D7B9}_is1
            //"C:\Subutai\unins000.exe" / SILENT  - uninstal string

            subkey = "SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Uninstall";
            DeleteSubKeyTree($"{SubutaiProdID}_is1", subkey, RegistryHive.LocalMachine);
            clean.UpdateProgress(50);

            //HKLM\SOFTWARE\Wow6432Node\Subutai Social\Subutai
            subkey = "SOFTWARE\\Wow6432Node";
            DeleteSubKeyTree(SubutaiVendor, subkey, RegistryHive.LocalMachine);
            clean.UpdateProgress(60);

            //HKCU\Software\Subutai Social\Subutai
            subkey = "SOFTWARE";
            DeleteSubKeyTree(SubutaiVendor, subkey, RegistryHive.CurrentUser);
            clean.UpdateProgress(70);

            //subutai key for SubutayTray
            //HKEY_CURRENT_USER\Software\subutai
            subkey = Path.Combine("Software", SubutaiTrayKey);
            rk = Registry.CurrentUser.OpenSubKey(subkey, true);
            DeleteSubKeyTree(SubutaiTraySubKey, subkey, RegistryHive.CurrentUser);
            subkey = "Software";
            DeleteSubKeyTree(SubutaiTrayKey, subkey, RegistryHive.CurrentUser);
            clean.UpdateProgress(80);
            
            //Google E2E plugin
            //HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Google\Chrome\Extensions\kpmiofpmlciacjblommkcinncmneeoaa
            subkey = "SOFTWARE\\Wow6432Node\\Google\\Chrome\\Extensions";
            rk = Registry.LocalMachine.OpenSubKey(subkey, true);
            DeleteSubKeyTree(SubutaiE2E, subkey, RegistryHive.LocalMachine);
            clean.UpdateProgress(90);

            //Run - Autostart
            //HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Run
            subkey = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run";
            rk = Registry.LocalMachine.OpenSubKey(subkey, true);
            if (rk != null)
            {
                try
                {
                    rk.DeleteValue("SubutaiTray");
                }
                catch (Exception ex)
                {
                    string tmp = ex.Message;
                }
            }
            clean.UpdateProgress(100);
        }

        /// <summary>
        /// Delete key by name.
        /// </summary>
        /// <param name="KeyName">Name of the key.</param>
        /// <param name="baseKey">The base (parent) key.</param>
        /// <returns></returns>
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
        /// Deletes the subkey tree.
        /// </summary>
        /// <param name="KeyName">Name of the subkey to be deleted.</param>
        /// <param name="rk">Registry key containign subkey to be deleted.</param>
        /// <returns></returns>
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
        /// Delete key by name.
        /// </summary>
        /// <param name="subkey">The subkey name to strat with.</param>
        /// <param name="str_2_find">The string to find in name.</param>
        /// <param name="rh">Registry Hive.</param>
        public static void DeleteKeyByName(string subkey, string str_2_find, RegistryHive rh)
        {
            var baseKey = RegistryKey.OpenBaseKey(rh, RegistryView.Registry64);

            RegistryKey rk = baseKey;
            if (subkey != "")
            {
                rk = baseKey.OpenSubKey(subkey, true);
            }

            if (rk != null)
            {
                foreach (string rsk in rk.GetSubKeyNames()) //Product
                {
                    if (rsk.ToLower().Contains(str_2_find.ToLower()))
                    {
                        DeleteSubKeyTree(rsk, ref rk);
                    }

                }
                rk.Close();
            }
            baseKey.Close();
        }

        /// <summary>
        /// Delete the variable if substring  found in value.
        /// </summary>
        /// <param name="subkey">The subkey.</param>
        /// <param name="str_2_find">The string 2 find.</param>
        /// <param name="rh">The rh.</param>
        public static void DeleteValueFound(string subkey, string str_2_find, RegistryHive rh)
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
                        //MessageBox.Show($"Vname: {vname} = {kvalue}", "DeleteValue", MessageBoxButtons.OK);
                        rk.DeleteValue(vname);
                    }
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
                        } else
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
        /// Runs the Registry cleaner file (obsolete).
        /// </summary>
        public static void run_reg_cleaner()
        {
            string rclean_path = FD.logDir();
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
            }
        }

        /// <summary>
        /// Read Value from Registry
        /// </summary>
        /// <param name="subkey">Subkey name.</param>
        /// <param name="vname">Variable name.</param>
        /// <param name="rh">Registry Hive.</param>
        /// <returns></returns>
        public static string get_value(string subkey, string vname, RegistryHive rh)
        {
            var baseKey = RegistryKey.OpenBaseKey(rh, RegistryView.Registry64);
            RegistryKey rk = baseKey.OpenSubKey(subkey, true);//Components
            string kvalue = "";
            if (rk != null)
            {
                try
                {
                    kvalue = Convert.ToString(rk.GetValue(vname));
                }
                catch (Exception e)
                {
                    string tmp = e.Message;
                    return "";
                }
                rk.Close();
                baseKey.Close();
            }
            return kvalue;
        }

        //Frind and read valye by name
        public static string find_and_get_value(string subkey, string name2find, string value2get, RegistryHive rh)
        {
            var baseKey = RegistryKey.OpenBaseKey(rh, RegistryView.Registry64);
            RegistryKey rk = baseKey.OpenSubKey(subkey, true);//Products
            string kvalue = "";
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
                        string tmpkey = Path.Combine(rsk, "InstallProperties");
                        RegistryKey installKey = rk.OpenSubKey(tmpkey, true);
                        tmpkey = Path.Combine(subkey, rsk, "InstallProperties");
                        if (installKey != null)
                        {
                            string prodName = installKey.GetValue("DisplayName").ToString();
                            if (prodName.Contains(name2find))
                            {
                                kvalue = get_value(tmpkey, value2get, rh);
                                installKey.Close();
                                productKey.Close();
                                rk.Close();
                                return kvalue;
                            }
                            installKey.Close();
                        }
                        productKey.Close();
                    }
                }
                rk.Close();
            }
            baseKey.Close();
            return kvalue;
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
            if (guids.Count > 0) {
                chromeGUID = guids["Google Chrome"];
                //MessageBox.Show($"Chrome GUID {chromeGUID}", "Chrome GUID", MessageBoxButtons.OK);
                if (guids.Count > 1)
                {
                    chromeGUIDbinaries = guids["Google Chrome binaries"];
                    //MessageBox.Show($"Chrome binaries GUID {chromeGUIDbinaries}", "Chrome GUID", MessageBoxButtons.OK);
                }
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
    }
}