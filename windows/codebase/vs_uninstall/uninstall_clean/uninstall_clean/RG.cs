using System;
using Microsoft.Win32;
using System.IO;
using System.Diagnostics;
using System.Windows.Forms;

namespace uninstall_clean
{
    class RG
    {
        public static void delete_from_reg()
        {
            //user environment
            string SubutaiProdID = @"CF66AAA126027D4479D5BB7808A6CDA7";
            string InstallerProdID = @"{1AAA66FC-2062-44D7-975D-BB87806ADC7A}";
            //label1.Text = "Cleanig Registry";
            string subkey;
            RegistryKey rk;
            clean.SetIndeterminate(true);
            clean.UpdateProgress(0);
            //HKEY_CLASSES_ROOT\Installer\Products\CF66AAA126027D4479D5BB7808A6CDA7
            subkey = "Installer\\Products";
            DeleteSubKeyTree(SubutaiProdID, subkey, RegistryHive.ClassesRoot);
            clean.UpdateProgress(5);
            //HKEY_LOCAL_MACHINE\SOFTWARE\Classes\Installer\Products\CF66AAA126027D4479D5BB7808A6CDA7
            subkey = "SOFTWARE\\Classes\\Installer\\Products";
            DeleteSubKeyTree(SubutaiProdID, subkey, RegistryHive.LocalMachine);
            clean.UpdateProgress(10);
            //HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\Folders
            subkey = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Installer\\Folders";
            //DeleteValueFound(subkey, "Subutai", RegistryHive.LocalMachine);
            clean.UpdateProgress(25);
            //HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\UserData\S - 1 - 5 - 18\Components\001B050B63BD23B49988FFEB639D2F61
            //Components
            subkey = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Installer\\UserData\\S-1-5-18\\Components";
            //DeleteKeyByValue(subkey, "Subutai", RegistryHive.LocalMachine);
            clean.UpdateProgress(30);
            //********************************************************
            //HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\UserData\S-1-5-18\Products\CF66AAA126027D4479D5BB7808A6CDA7
            //HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\UserData\S-1-5-18\Products\CF66AAA126027D4479D5BB7808A6CDA7

            //var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            subkey = @"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Installer\\UserData\\S-1-5-18\\Products";
            DeleteSubKeyTree(SubutaiProdID, subkey, RegistryHive.LocalMachine);
            clean.UpdateProgress(35);
            //HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{ 1AAA66FC - 2062 - 44D7 - 975D - BB87806ADC7A}
            subkey = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall";
            DeleteSubKeyTree(SubutaiProdID, subkey, RegistryHive.LocalMachine);
            clean.UpdateProgress(40);
            //HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Subutai 4.0.07
            //Uninstalling version key is CLOSED
            //HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Subutai 4.0.2

            subkey = "SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Uninstall";
            DeleteKeyByValue(subkey, "Subutai", RegistryHive.LocalMachine);
            clean.UpdateProgress(45);
            //HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Caphyon\Advanced Installer\LZMA\{ 1AAA66FC - 2062 - 44D7 - 975D - BB87806ADC7A}
            subkey = "SOFTWARE\\Wow6432Node\\Caphyon\\Advanced Installer\\LZMA";
            DeleteSubKeyTree(SubutaiProdID, subkey, RegistryHive.LocalMachine);
            clean.UpdateProgress(50);
            //HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Subutai Social
            subkey = "SOFTWARE\\Wow6432Node";
            DeleteSubKeyTree("Subutai Social", subkey, RegistryHive.LocalMachine);
            clean.UpdateProgress(55);
            //HKEY_LOCAL_MACHINE\SYSTEM\ControlSet001\Control\Session Manager\Environment
            //Path, Subutai
            subkey = "SYSTEM\\ControlSet001\\Control\\Session Manager\\Environment";
            DeleteValueFound(subkey, "Subutai", RegistryHive.LocalMachine);
            clean.UpdateProgress(60);
            //HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Environment
            subkey = "SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Environment";
            DeleteSubKeyTree("Subutai", subkey, RegistryHive.LocalMachine);
            clean.UpdateProgress(65);
            //HKEY_LOCAL_MACHINE\SYSTEM\VritualRoot\MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\UserData\S - 1 - 5 - 18\Components\336320F1CCE3E3F45A57FD0D4E46AB34
            subkey = "SYSTEM\\VritualRoot\\MACHINE\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Installer\\UserData\\S-1-5-18\\Components";
            DeleteKeyByValue(subkey, "Subutai", RegistryHive.LocalMachine);
            clean.UpdateProgress(70);
            //HKEY_LOCAL_MACHINE\SYSTEM\VritualRoot\MACHINE\SOFTWARE\Wow6432Node\Caphyon\Advanced Installer\LZMA
            subkey = "SYSTEM\\VritualRoot\\MACHINE\\SOFTWARE\\Wow6432Node\\Caphyon\\Advanced Installer\\LZMA";
            DeleteSubKeyTree(InstallerProdID, subkey, RegistryHive.LocalMachine);
            clean.UpdateProgress(75);
            subkey = "SYSTEM\\VritualRoot\\MACHINE\\SOFTWARE\\Wow6432Node";
            DeleteSubKeyTree("Subutai Social", subkey, RegistryHive.LocalMachine);
            clean.UpdateProgress(80);
            //HKEY_CURRENT_USER\Software\Optimal-dynamics
            //HKEY_CURRENT_USER\Software\Optimal - dynamics\SS_Tray

            //subkey = "Software\\Optimal-dynamics";
            //DeleteSubKeyTree("SS_Tray", subkey, RegistryHive.CurrentUser);

            subkey = "Software";
            rk = Registry.CurrentUser.OpenSubKey(subkey, true);
            DeleteSubKeyTree("Optimal-dynamics", subkey, RegistryHive.CurrentUser);
            clean.UpdateProgress(90);
            //HKEY_CURRENT_USER\Environment
            subkey = "Environment";
            DeleteSubKeyTree("Subutai", subkey, RegistryHive.CurrentUser);
            clean.UpdateProgress(100);
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