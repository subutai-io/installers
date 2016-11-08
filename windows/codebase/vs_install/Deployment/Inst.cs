﻿using System;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Win32;
using NLog;
using Renci.SshNet;

namespace Deployment
{
    /// <summary>
    /// class Inst
    /// Install components
    /// </summary>
    class Inst
    {
        //Installation of components
        private static NLog.Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// public static int app_installed(string appName)
        /// Define if application installed 
        /// Looking in Registry
        /// </summary>
        /// <param name="appName">Application name (with Registry path)</param>
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

        /// <summary>
        /// public static string subutai_path()
        /// Get Subutai installation path to check if already installed
        /// Looking in Registry
        /// </summary>
        public static string subutai_path()
        {
            string subkey86 = Path.Combine("SOFTWARE", "Subutai Social", "Subutai");
            RegistryKey rk = Registry.CurrentUser.OpenSubKey(subkey86);
            if (rk == null)
            {
                //return "NA";
                return "E:\\Gubutai";
            }
            string path = rk.GetValue("Path").ToString();
            return path;
        }

        /// <summary>
        /// public static bool update_uninstallString(string strUninst)
        /// Update uninstall string
        /// Changing in Registry
        /// </summary>
        /// <param name="strUninst">Uninstall string</param>
        public static bool update_uninstallString(string strUninst)
        {
            string subkey = "SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\Subutai";
            RegistryKey rk = Registry.CurrentUser.OpenSubKey(subkey, true);
            if (rk == null)
            {
                return false;
            }
            try
            {
                rk.SetValue("UninstallString", strUninst);
                return true;
            }
            catch(Exception ex)
            {
                string res = ex.Message;
                return false;
            }
        }


        /// <summary>
        /// public static void inst_TAP(string instDir)
        /// Install TAP driver and utilities
        /// </summary>
        /// <param name="instDir">Installation directory</param>
        public static void inst_TAP(string instDir)
        {
            string res = "";
            Deploy.StageReporter("", "TAP driver");
            if (app_installed("TAP-Windows") == 0)
            {
                res = Deploy.LaunchCommandLineApp($"{instDir}\\redist\\tap-driver.exe", "/S");
                logger.Info("TAP driver: {0}", res);
            } else
            {
                Deploy.StageReporter("", "TAP driver already installed");
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

        /// <summary>
        /// public static void inst_Chrome(string instDir)
        /// Install Google Chrome Browser
        /// </summary>
        /// <param name="instDir">Installation directory</param>
        public static void inst_Chrome(string instDir)
        {
            string res = "";
            //HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Clients\StartMenuInternet\Google Chrome
            if (app_installed("Clients\\StartMenuInternet\\Google Chrome") == 0)
            {
                Deploy.StageReporter("", "Chrome");
                res = Deploy.LaunchCommandLineApp("msiexec", $"/qn /i \"{instDir}redist\\chrome.msi\"");
                logger.Info("Chrome: {0}", res);
            }
            else
            {
                Deploy.StageReporter("", "Google\\Chrome is already installed");
                logger.Info("Google\\Chrome is already installed");
            }
        }

        /// <summary>
        /// public static bool create_subkey(string keyPath, string subKeyPath)
        /// Create subkey in Registry 
        /// for E2E plugin installation
        /// </summary>
        /// <param name="keyPath">Parent key for subkey to be created</param>
        /// <param name="subKeyPath">Subkey to be created</param>
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
                string[] keyPathArr = keyPath.Split(new[] { "\\" }, StringSplitOptions.None);
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

        /// <summary>
        /// public static void inst_E2E()
        /// Installation of E2E plugin
        /// Can be installen only using Registry
        /// </summary>
        public static void inst_E2E()
        {
            Deploy.StageReporter("", "Installing Chrome E2E extension");
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

        /// <summary>
        /// public static void inst_VBox(string instDir)
        /// Installation of Oracle VirtualBox software
        /// and adding firewall rules
        /// </summary>
        /// <param name="instDir">Installation directory</param>
        public static void inst_VBox(string instDir)
        {
            string res = "";
            if (app_installed("Oracle\\VirtualBox") == 0)
            {
                res = Deploy.LaunchCommandLineApp($"{instDir}\\redist\\virtualbox.exe", "--silent");
                Deploy.CreateShortcut(
                    $"{Environment.GetEnvironmentVariable("ProgramFiles")}\\Oracle\\VirtualBox\\VirtualBox.exe",
                    $"{Environment.GetEnvironmentVariable("Public")}\\Desktop\\Oracle VM VirtualBox.lnk",
                    "", "", true);
                //Deploy.CreateShortcut(
                //    $"{Environment.GetEnvironmentVariable("ProgramFiles")}\\Oracle\\VirtualBox\\VirtualBox.exe",
                //    $"{Environment.GetEnvironmentVariable("Public")}\\Desktop\\Oracle VM VirtualBox.lnk",
                //    "", true, "");
                Deploy.CreateShortcut(
                    $"{Environment.GetEnvironmentVariable("ProgramFiles")}\\Oracle\\VirtualBox\\VirtualBox.exe",
                    $"{Environment.GetEnvironmentVariable("ProgramData")}\\Microsoft\\Windows\\Start Menu\\Programs\\Oracle VM VirtualBox\\Oracle VM VirtualBox.lnk",
                    "", "", true);
                logger.Info("Virtual Box: {0} ", res);
            }
            else
            {
                Deploy.StageReporter("", "Oracle\\VirtualBox is already installed");
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

        /// <summary>
        /// public static void inst_ssh(string instDir)
        /// Create home folder (soft link) for ssh to create .ssh directory
        /// to keep ssh files - known_hosts, keys etc
        /// </summary>
        /// <param name="instDir">Installation directory</param>
        public static void inst_ssh(string instDir)
        {
            string path_t = Path.Combine(FD.sysDrive(), "Users");
            string path_l = Path.Combine(instDir, "home");
            if (!Directory.Exists(path_l))
            {
                string res = Deploy.LaunchCommandLineApp("cmd.exe", $"/C mklink /d {path_l} {path_t}");
                logger.Info("ssh - creating home: {0}", res);
            } else
            {
                logger.Info("link {0} already exists", path_l);
            }

        }

        /// <summary>
        /// public static void remove_ssh(string instDir)
        /// Removes home folder (soft link) 
        /// before new installation (will fail if not deleted)
        /// </summary>
        /// <param name="instDir">Installation directory</param>
        public static void remove_ssh(string instDir)
        {
            //string path_t = Path.Combine(FD.sysDrive(), "Users");
            string path_l = Path.Combine(instDir, "home");
            if (Directory.Exists(path_l))
            {
                Directory.Delete(path_l);
                logger.Info("ssh home dir exists, removing {0}", path_l);
            }
        }

        /// <summary>
        /// public static void service_stop(string serviceName)
        /// Stops Subutai Social P2P service (in case if running - was not deleted by previous installation)
        /// </summary>
        /// <param name="serviceName">Name of service to be stopped</param>
        public static void service_stop(string serviceName)
        {
            string res = "";
            //res = Deploy.LaunchCommandLineApp("nssm", $"stop \"{serviceName}\"");
            res = Deploy.LaunchCommandLineApp("sc", $"stop \"{serviceName}\"");
            logger.Info("Stopping service {0}: {1}", serviceName, res);
        }

        /// <summary>
        /// public static void service_install(string serviceName, string binPath, string binArgument)
        /// Installs Subutai Social P2P service 
        /// using nssm
        /// </summary>
        /// <param name="serviceName">Name of service to be installed</param>
        /// <param name="binPath">bath to binary</param>
        /// <param name="binArgument">arguments</param>
        public static void service_install(string serviceName, string binPath, string binArgument)
        {
            string res = "";
            res = Deploy.LaunchCommandLineApp("nssm", $"install \"{serviceName}\" \"{binPath}\" \"{binArgument}\"");
            string cmd = $"create \"{serviceName}\" binpath= \"\"{binPath}\" \"{binArgument}\"\" start= auto";
            //res = Deploy.LaunchCommandLineApp("sc", cmd);
            logger.Info("Installing P2P service: {0} {1}", cmd, res);
        }

        /// <summary>
        /// public static void service_start(string serviceName)
        /// Starts Subutai Social P2P service 
        /// </summary>
        /// <param name="serviceName">Name of service to be installed</param>
        public static void service_start(string serviceName)
        {
            string res = "";
            res = Deploy.LaunchCommandLineApp("nssm", $"start \"{serviceName}\"");
            //res = Deploy.LaunchCommandLineApp("sc", $"start \"{serviceName}\"");
            logger.Info("Starting P2P service: {0}", res);
            Thread.Sleep(2000);
        }

        /// <summary>
        /// public static void service_config(string serviceName)
        /// Starts Subutai Social P2P service 
        /// </summary>
        /// <param name="serviceName">Name of service to be installed</param>
        public static void service_config(string serviceName)
        {
            string res = "";
            res = Deploy.LaunchCommandLineApp("sc", $"failure \"{serviceName}\" actions= restart/10000/restart/15000/restart/18000 reset= 86400");
            logger.Info("Configuring P2P service {0}", res);
            Thread.Sleep(5000);
        }

        /// <summary>
        /// public static void p2p_logs_config(string sname)
        /// Config logging optiond for Subutai Social P2P service 
        /// </summary>
        /// <param name="serviceName">Name of service to be installed</param>
        public static void p2p_logs_config(string serviceName)
        {
            string logPath = FD.logDir();
            logPath = Path.Combine(logPath, "p2p_log.txt");
            logger.Info("Logs are in {0}", logPath);
            //Create Registry keys for parameters
            string sPath = $"System\\CurrentControlSet\\Services\\{serviceName}\\Parameters";
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
                int maxBytes = 50 * 1024;//
                Registry.SetValue(ksPath, "AppRotate", maxBytes, RegistryValueKind.ExpandString);
                logger.Info("AppRotateBytes: {0}", maxBytes);
                kPath.Close();
            }
        }

        /// <summary>
        /// public static void install_mh_nw()
        /// Installing management host, ssh using name-password
        /// </summary>
        public static void install_mh_nw()
        {
            //installing master template
            Deploy.StageReporter("", "Importing master");
            logger.Info("Importing master");
            bool b_res = import_templ_task("master");

            // installing management template
            Deploy.StageReporter("", "Importing management");
            b_res = import_templ_task("management");
            if (!b_res)
            {
                logger.Info("trying import management again");
                b_res = import_templ_task("management");
                if (!b_res)
                {
                    logger.Info("import management failed second time");
                    Program.ShowError("Management template was not installed, installation failed, please try to install later", "Management template was not imported");
                    Program.form1.Visible = false;
                }
            }
            string ssh_res = "", ssh_res_old = "";
            ssh_res = Deploy.SendSshCommand("127.0.0.1", 4567, "ubuntu", "ubuntu",
                "sudo bash subutai info ipaddr");
            //todo: delete old
            ssh_res_old = Deploy.SendSshCommand("127.0.0.1", 4567, "ubuntu", "ubuntu",
                "sudo bash subutai management_network detect");
            logger.Info("Import management address returned by subutai info: {0}, by network_management: {1}", ssh_res, ssh_res_old);
            
            string rhIP = Deploy.com_out(ssh_res, 1);
            if (!is_ip_address(rhIP))
            {
                logger.Error("import management failed ", "Management template was not installed");
                Program.ShowError("Management template was not installed, installation failed, removing", "Management template was not imported");
                Program.form1.Visible = false;
            }
        }

        /// <summary>
        /// public static bool is_ip_address(string in_str)
        /// Checks if string is likely ip address (To do - improve checking)
        /// </summary>
        /// <param name="in_str">String to be checked</param>
        public static bool is_ip_address(string in_str)
        {
            string[] ips = in_str.Split('.');
            if (ips.Length != 4)
            {

                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// public static void install_mh_lc(PrivateKeyFile[] privateKeys)
        /// Installing management host, ssh using private keys
        /// </summary>
        /// <param name="privateKeys">Private keys array</param>
        public static void install_mh_lc(PrivateKeyFile[] privateKeys)
        {
            //installing master template
            Deploy.StageReporter("", "Importing master");
            logger.Info("Importing master");
            string ssh_res = Deploy.SendSshCommand("127.0.0.1", 4567, "ubuntu", privateKeys, "sudo echo -e 'y' | sudo subutai -d import master 2>&1 > master_log");

            logger.Info("Import master: {0}", ssh_res);
            ssh_res = Deploy.SendSshCommand("127.0.0.1", 4567, "ubuntu", "ubuntu", "ls -l master_log| wc -l");
            logger.Info("Import master log: {0}", ssh_res);

            // installing management template
            Deploy.StageReporter("", "Importing management");
            ssh_res = Deploy.SendSshCommand("127.0.0.1", 4567, "ubuntu", privateKeys, "sudo echo -e 'y' | sudo subutai -d import management 2>&1 > management_log ");
            logger.Info("Import management: {0}", ssh_res);
            if (Deploy.com_out(ssh_res, 0) != "0")
            {
                logger.Error("Management template was not installed");
                Program.ShowError("Management template was not installed, instllation failed, please uninstall and try to install later", "Management template was not imported");
                Program.form1.Visible = false;
            }
        }

        /// <summary>
        /// public static bool import_templ(string tname)
        /// import template 
        /// </summary>
        /// <param name="tname">Template name</param>
        public static bool import_templ(string tname)
        {
            string ssh_res = Deploy.SendSshCommand("127.0.0.1", 4567,
                "ubuntu", "ubuntu", $"sudo subutai -d import {tname} 2>&1 > {tname}_log");

            string stcode = Deploy.com_out(ssh_res, 0);
            string sterr = Deploy.com_out(ssh_res, 2);

            logger.Info("Import {0}: {1}, code: {2}, err: {3}",
                tname, ssh_res, stcode, sterr);

            if (stcode != "0") //&&  sterr != "Empty")
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// public static bool import_templ_task(string tname)
        /// import template in separate task to know if import is running
        /// </summary>
        /// <param name="tname">Template name</param>
        public static bool import_templ_task(string tname)
        {
            // Cancellation token to cancel watcher when import is finished or cancelled
            var tokenSource = new CancellationTokenSource();
            var token = tokenSource.Token;

            //Starting Watcher task as parent, import as child
            var watcher = Task.Factory.StartNew(() =>
            {
                string res0 = "";
                int cnt = 0;
                logger.Info("Import {0}",  tname);
                while (true)
                {
                    Thread.Sleep(5000);
                    if (token.IsCancellationRequested)
                    {
                        logger.Info("Watcher cancelled");
                        break;
                    }
                    string res = check_templ(tname);
                    logger.Info("res = {0}", res);
                    if (res == res0)
                    {
                        //will check 5 times more
                        cnt++;
                        if (cnt >= 5)
                        {
                            //stop 
                            logger.Info("Cancelling from watcher");
                            tokenSource.Cancel();
                            //break;      //////////////////check this!
                        }
                    }
                    res0 = res;
                }
            }, token);

            var import = Task.Factory.StartNew(() =>
            {
                string ssh_res = Deploy.SendSshCommand("127.0.0.1", 4567,
                        "ubuntu", "ubuntu", $"sudo subutai -d import {tname} 2>&1 > {tname}_log");

                string stcode = Deploy.com_out(ssh_res, 0);
                string sterr = Deploy.com_out(ssh_res, 2);

                logger.Info("Import {0}: {1}, code: {2}, err: {3}",
                    tname, ssh_res, stcode, sterr);

                if (stcode != "0") //&&  sterr != "Empty")
                {
                    return false;
                }
                return true;
            }, token);

            import.Wait();//import finished
            logger.Info("Cancelling from outer");
            tokenSource.Cancel();//cancel  watcher
            bool b_res = false;
            if (import.IsCompleted)
            {
                b_res = import.Result;
            } 
            return b_res;
       }

        /// <summary>
        /// private static  string check_templ(string tname)
        /// Check if import template is running, runs as separate task
        /// </summary>
        /// <param name="tname">Template name</param>
        private static  string check_templ(string tname)
        {
            string ssh_res = Deploy.SendSshCommand("127.0.0.1", 4567,
                    "ubuntu", "ubuntu", $"du -b {tname}_log"); //find size in bytes
            string stcode = Deploy.com_out(ssh_res, 0);
            string stres = Deploy.com_out(ssh_res, 1);
            string sterr = Deploy.com_out(ssh_res, 2);
            return stres;
        }
    }
}