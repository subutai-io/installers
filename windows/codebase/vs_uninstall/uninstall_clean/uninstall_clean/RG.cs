using System;
using Microsoft.Win32;
using System.IO;
using System.Diagnostics;
using System.Windows.Forms;

namespace uninstall_clean
{
    class RG
    {
        //Clean Registry
        public static void delete_from_reg()
        {
            //user environment
            string SubutaiProdName = "Subutai";
            string SubutaiProdID = "{D8AEAA94-0C20-4F7E-A106-4E9617A3D7B9}_is1";
            string SubutaiVendor = "Subutai Social";
            string SubutaiTrayVendor = "Optimal-dynamics";
            string SubutaiTrayKeyName = "SS_Tray";
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
            DeleteSubKeyTree(SubutaiProdID, subkey, RegistryHive.LocalMachine);
            clean.UpdateProgress(50);

            //HKLM\SOFTWARE\Wow6432Node\Subutai Social\Subutai
            subkey = "SOFTWARE\\Wow6432Node";
            DeleteSubKeyTree(SubutaiVendor, subkey, RegistryHive.LocalMachine);
            clean.UpdateProgress(60);

            //HKCU\Software\Subutai Social\Subutai
            subkey = "SOFTWARE";
            DeleteSubKeyTree(SubutaiVendor, subkey, RegistryHive.CurrentUser);
            clean.UpdateProgress(70);

            //Optimal-dynamics key for SubutayTray
            //HKEY_CURRENT_USER\Software\Optimal-dynamics
            subkey = Path.Combine("Software", SubutaiTrayVendor);
            rk = Registry.CurrentUser.OpenSubKey(subkey, true);
            DeleteSubKeyTree(SubutaiTrayKeyName, subkey, RegistryHive.CurrentUser);
            subkey = "Software";
            DeleteSubKeyTree(SubutaiTrayVendor, subkey, RegistryHive.CurrentUser);
            clean.UpdateProgress(80);
            
            //Google E2E plugin
            //HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Google\Chrome\Extensions\kpmiofpmlciacjblommkcinncmneeoaa
            subkey = "SOFTWARE\\Wow6432Node\\Google\\Chrome\\Extensions";
            rk = Registry.LocalMachine.OpenSubKey(subkey, true);
            DeleteSubKeyTree(SubutaiE2E, subkey, RegistryHive.LocalMachine);
            
            clean.UpdateProgress(100);
        }

        //Delete key by name
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

        //Delete key and all subkeys
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

        //Delete child subkey tree if substring was found in child key values
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

        //Delete child key tree if substring was found in child key name
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

        //Delete subkey in root key
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

        //Read Value from Registry
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
    }
}