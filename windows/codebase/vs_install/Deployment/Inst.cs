using System;
using System.Threading;
using System.IO;
using Microsoft.Win32;
using NLog;
using Renci.SshNet;

namespace Deployment
{
    class Inst
    {
        private static NLog.Logger logger = LogManager.GetCurrentClassLogger();

        //Define if application is installed
        public static int app_installed(string appName)
        {
            string subkey = Path.Combine("SOFTWARE\\Wow6432Node", appName);
            string subkey86 = Path.Combine("SOFTWARE\\", appName);
            RegistryKey rk = Registry.LocalMachine.OpenSubKey(subkey);
            RegistryKey rk86 = Registry.LocalMachine.OpenSubKey(subkey86);
            if (rk == null && rk86 == null)
            {
                return 0;
            }
            return 1;
        }

        
        //Install TAP driver and utilities
        public static void inst_TAP(string instDir)
        {
            string res = "";
            Form1.StageReporter("", "TAP driver");
            if (app_installed("TAP-Windows") == 0)
            {
                res = Deploy.LaunchCommandLineApp($"{instDir}\\redist\\tap-driver.exe", "/S");
                logger.Info("TAP driver: {0}", res);
            } else
            {
                Form1.StageReporter("", "TAP driver already installed");
                logger.Info("TAP driver is already installed: {0}", res);
            }

            if (app_installed("TAP-Windows") == 1)
            {
                var pathTAPin = Path.Combine(instDir, "redist");
                var pathTAPout = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "TAP-Windows", "bin");
           
                try
                {
                    File.Copy(Path.Combine(pathTAPin, "addtap.bat"), Path.Combine(pathTAPout, "addtap.bat"), true);
                    logger.Info("Copying {0}\\addtap.bat to {1}\\addtap.bat", pathTAPin.ToString(), pathTAPout.ToString());
                }
                catch (Exception ex)
                {
                    logger.Error(ex.Message + " copying utility addtap");
                }
                try
                {
                    File.Copy(Path.Combine(pathTAPin, "deltapall.bat"), Path.Combine(pathTAPout, "deltapall.bat"), true);
                    logger.Info("Copying {0}\\deltapall.bat to {1}", pathTAPin.ToString(), pathTAPout.ToString());
                }
                catch (Exception ex)
                {
                    logger.Error(ex.Message + " copying utility deltapall");
                }
            }
         }

        //Install Chrome
        public static void inst_Chrome(string instDir)
        {
            string res = "";
            //HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Clients\StartMenuInternet\Google Chrome
            if (app_installed("Clients\\StartMenuInternet\\Google Chrome") == 0)
            {
                Form1.StageReporter("", "Chrome");
                res = Deploy.LaunchCommandLineApp("msiexec", $"/qn /i \"{instDir}\\redist\\chrome.msi\"");
                logger.Info("Chrome: {0}", res);
            }
            else
            {
                Form1.StageReporter("", "Google\\Chrome is already installed");
                logger.Info("Google\\Chrome is already installed");
            }
        }

        //Install E2E extension - create subkey in Registry
        public static bool create_subkey(string keyPath, string subKeyPath)
        {
            string keyPath0 = keyPath; //"SOFTWARE\\Wow6432Node\\Google\\Chrome\\Extensions";
            string subKeyPath0 = subKeyPath; //"kpmiofpmlciacjblommkcinncmneeoaa";
            RegistryKey kPath = Registry.LocalMachine.OpenSubKey(keyPath, true);
            if (kPath != null) //Extensions subkey exists
            {
                kPath.CreateSubKey(subKeyPath);//create E2E subkey
                kPath.Close();
                string keyPath1 = Path.Combine(keyPath, subKeyPath);
                logger.Info("After create subkey {0}", keyPath1);
                kPath = Registry.LocalMachine.OpenSubKey(keyPath1, true);
                if (kPath != null) //E2E key exists, was created
                {
                    logger.Info("Subkey {0} exists", subKeyPath);
                    kPath.Close();
                    return true;
                } else
                {
                    logger.Info("Subkey {0} was not added", subKeyPath);
                    return false;
                }
            } else { //there is no  key to add subkey
                string [] keyPathArr = keyPath.Split(new[] { "\\" },  StringSplitOptions.None);
                logger.Info("keyPath = {0}, keyPathArr[0] = {1}, keyPathArr[3] = {2}", keyPath, keyPathArr[0], keyPathArr[3]);

                string keyPath2 = "";
                    
                Registry.LocalMachine.OpenSubKey(keyPathArr[0], true);
                for (int i = 0; i < keyPathArr.Length; i++)
                {
                    keyPath2 = Path.Combine(keyPath2, keyPathArr[i]);
                    kPath = Registry.LocalMachine.OpenSubKey(keyPath2, true);
                    logger.Info("i = {0}  keyPath2 = {1}", i, keyPath2);
                    if (kPath == null)
                    {
                        kPath = Registry.LocalMachine.CreateSubKey(keyPath2);
                    }
                    kPath.Close();
                }
                if (create_subkey(keyPath0, subKeyPath0))
                {
                    return true; //Subkey exist
                } else
                {
                    logger.Info("Could not create subkey {0}", subKeyPath0);
                    return false;
                }
            }
        }

        public static void inst_E2E()
        {
            Form1.StageReporter("", "Installing Chrome E2E extension");
            logger.Info("Installing Chrome E2E extension");
            string e2ePath = "SOFTWARE\\Wow6432Node\\Google\\Chrome\\Extensions";
            string e2eName = "kpmiofpmlciacjblommkcinncmneeoaa";

            string e2eKey = Path.Combine(e2ePath, e2eName);
            RegistryKey extPath = Registry.LocalMachine.OpenSubKey(e2eKey, true);
            if (extPath == null)
            {
                logger.Info("Setting E2E extension  subkey");
                
                if (create_subkey(e2ePath, e2eName))
                {
                    extPath = Registry.LocalMachine.OpenSubKey(e2eKey, true);
                    
                    if (extPath == null)
                        logger.Info("Can not open E2E subkey");
                }
            }
            else
            {
                logger.Info("Chrome E2E subkey exists");
            }
            extPath.SetValue("update_url", "http://clients2.google.com/service/update2/crx", RegistryValueKind.String);
            logger.Info("Chrome E2E extension values sat");
        }

        public static void inst_VBox(string instDir)
        {
            string res = "";
            if (app_installed("Oracle\\VirtualBox") == 0)
            {
                res = Deploy.LaunchCommandLineApp($"{instDir}\\redist\\virtualbox.exe", "--silent");
                Deploy.CreateShortcut(
                    $"{Environment.GetEnvironmentVariable("ProgramFiles")}\\Oracle\\VirtualBox\\VirtualBox.exe",
                    $"{Environment.GetEnvironmentVariable("Public")}\\Desktop\\Oracle VM VirtualBox.lnk",
                    "", true);
                Deploy.CreateShortcut(
                    $"{Environment.GetEnvironmentVariable("ProgramFiles")}\\Oracle\\VirtualBox\\VirtualBox.exe",
                    $"{Environment.GetEnvironmentVariable("Public")}\\Desktop\\Oracle VM VirtualBox.lnk",
                    "", true);
                Deploy.CreateShortcut(
                    $"{Environment.GetEnvironmentVariable("ProgramFiles")}\\Oracle\\VirtualBox\\VirtualBox.exe",
                    $"{Environment.GetEnvironmentVariable("ProgramData")}\\Microsoft\\Windows\\Start Menu\\Programs\\Oracle VM VirtualBox\\Oracle VM VirtualBox.lnk",
                    "", true);
                logger.Info("Virtual Box: {0} ", res);
            }
            else
            {
                Form1.StageReporter("", "Oracle\\VirtualBox is already installed");
                logger.Info("Oracle\\VirtualBox is already installed");
            }

            //Adding windows firewall rules for vboxheadless.exe and virtualbox.exe
            string VBoxDir = "";
            VBoxDir = Environment.GetEnvironmentVariable("VBOX_MSI_INSTALL_PATH");
            if (VBoxDir == "" || VBoxDir == null)
            {
                VBoxDir = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                VBoxDir = Path.Combine(VBoxDir, "Oracle", "VirtualBox");
            }
            string VBoxPath = Path.Combine(VBoxDir, "VBoxHeadless.exe");
            //VBoxPath = VBoxDir.ToLower();
            Net.set_fw_rules(VBoxPath.ToLower(), "vboxheadless", false);

            VBoxPath = Path.Combine(VBoxDir, "VirtualBox.exe");
            VBoxDir = VBoxDir.ToLower();
            Net.set_fw_rules(VBoxPath.ToLower(), "virtualbox", false);
        }

        public static void service_stop(string serviceName)
        {
            string res = "";
            res = Deploy.LaunchCommandLineApp("nssm", $"stop \"{serviceName}\"");
            logger.Info("Stopping service {0}: {1}", serviceName, res);
        }

        public static void service_install(string serviceName, string binPath, string binArgument)
        {
            string res = "";
            res = Deploy.LaunchCommandLineApp("nssm", $"install \"{serviceName}\" \"{binPath}\" \"{binArgument}\"");
            logger.Info("Installing P2P service: {0}", res);
        }

        public static void service_start(string serviceName)
        {
            string res = "";
            res = Deploy.LaunchCommandLineApp("nssm", $"start \"{serviceName}\"");
            logger.Info("Starting P2P service: {0}", res);
            Thread.Sleep(2000);
        }

        public static void service_config(string serviceName)
        {
            string res = "";
            res = Deploy.LaunchCommandLineApp("sc", $"failure \"{serviceName}\" actions= restart/10000/restart/15000/restart/18000 reset= 86400");
            logger.Info("Configuring P2P service {0}", res);
            Thread.Sleep(5000);
        }

        public static void p2p_logs_config(string sname)
        {
            string logPath = FD.logDir();
            logPath = Path.Combine(logPath, "p2p_log.txt");
            logger.Info("Logs are in {0}", logPath);
            //Create Registry keys for parameters
            string sPath = $"System\\CurrentControlSet\\Services\\{sname}\\Parameters";
            string ksPath = $"HKEY_LOCAL_MACHINE\\{sPath}";
            logger.Info("Registry key", sPath);
            RegistryKey kPath = Registry.LocalMachine.OpenSubKey(sPath);

            if (kPath != null)
            {
                //Path to logs
                Registry.SetValue(ksPath, "AppStdout", logPath, RegistryValueKind.ExpandString);
                logger.Info("AppStdout: {0}", logPath);
                Registry.SetValue(ksPath, "AppStderr", logPath, RegistryValueKind.ExpandString);
                logger.Info("AppStderr: {0}", logPath);
                //Logs rotation
                //kPath.SetValue("AppRotateSeconds", 86400, RegistryValueKind.DWord);
                //Registry.SetValue(sPath, "AppRotateSeconds", filepath, RegistryValueKind.ExpandString);
                int maxBytes = 5242880;
                Registry.SetValue(ksPath, "AppRotate", maxBytes, RegistryValueKind.ExpandString);
                logger.Info("AppRotateBytes: {0}", maxBytes);
                kPath.Close();
            }
        }

        public static void install_mh_nw()
        {
            //installing master template
            Form1.StageReporter("", "Importing master");
            logger.Info("Importing master");
            VMs.import_templ("master");

            // installing management template
            Form1.StageReporter("", "Importing management");
            bool b_res = VMs.import_templ("management");
            if (!b_res)
            {
                logger.Info("trying import management again");
                b_res = VMs.import_templ("management");
                if (!b_res)
                {
                    logger.Info("import management failed second time");
                    Program.ShowError("Management template was not installed, installation failed, please try to install later", "Management template was not imported");
                    Program.form1.Visible = false;
                }
            }
            string ssh_res = "";
            ssh_res = Deploy.SendSshCommand("127.0.0.1", 4567, "ubuntu", "ubuntu", 
                "sudo bash subutai management_network detect");
            logger.Info("Import management address: {0}", ssh_res);

            if (Deploy.com_out(ssh_res, 0) != "0")
            {
                logger.Error("import management failed second time", "Management template was not installed");
                Program.ShowError("Management template was not installed, installation failed, removing", "Management template was not imported");
                Program.form1.Visible = false;
            }
        }

        public static void install_mh_lc(PrivateKeyFile[] privateKeys)
        {
            //installing master template
            Form1.StageReporter("", "Importing master");
            logger.Info("Importing master");
            string ssh_res = Deploy.SendSshCommand("127.0.0.1", 4567, "ubuntu", privateKeys, "sudo echo -e 'y' | sudo subutai -d import master 2>&1 > master_log");
            logger.Info("Import master: {0}", ssh_res);
            ssh_res = Deploy.SendSshCommand("127.0.0.1", 4567, "ubuntu", "ubuntu", "ls -l master_log| wc -l");
            logger.Info("Import master log: {0}", ssh_res);

            // installing management template
            Form1.StageReporter("", "Importing management");
            ssh_res = Deploy.SendSshCommand("127.0.0.1", 4567, "ubuntu", privateKeys, "sudo echo -e 'y' | sudo subutai -d import management 2>&1 > management_log ");
            logger.Info("Import management: {0}", ssh_res);
            if (Deploy.com_out(ssh_res, 0) != "0")
            {
                logger.Error("Management template was not installed");
                Program.ShowError("Management template was not installed, instllation failed, please uninstall and try to install later", "Management template was not imported");
                Program.form1.Visible = false;
            }
        }
    }
}
