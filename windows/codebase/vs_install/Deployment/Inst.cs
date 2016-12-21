using System;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;
using Microsoft.Win32;
using NLog;
using Renci.SshNet;
using System.Windows.Forms;

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
        private static string rhTemplatePlace = "/mnt/lib/lxc/tmpdir";
        public static bool imported = false;

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
                return "NA";
            }
            string path = rk.GetValue("Path").ToString();
            return path;
        }

        /// <summary>
        /// Defines the name of the unique identifier by app name.
        /// </summary>
        /// <param name="key">The key where to look.</param>
        /// <param name="vname2check">The vname2check - name of variable containing app name.</param>
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
            return dguids;
        }


        /// <summary>
        /// public static bool update_uninstallString(string strUninst)
        /// Update uninstall string
        /// Changing in Registry
        /// </summary>
        /// <param name="strUninst">Uninstall string</param>
        public static bool update_uninstallString(string strUninst)
        {
            string key = "SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Uninstall";
            Dictionary<string, string> guids = define_GUID_by_name(key, "DisplayName", "Subutai version", RegistryHive.LocalMachine, true);
            string SubutaiGUID = "";
            logger.Info("Updating uninstall string");
            if (guids.Count > 0)
            {
                foreach (string k in guids.Keys)
                {
                    SubutaiGUID = guids[k];
                    logger.Info("Subutai GUIDs: {0}: {1}", k, SubutaiGUID);
                    string subkey = $"SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\{SubutaiGUID}";
                    //"SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\Subutai";
                    RegistryKey rk = Registry.LocalMachine.OpenSubKey(subkey, true);
                    if (rk == null)
                    {
                        return false;
                    }
                    try
                    {
                        rk.SetValue("UninstallString", strUninst);
                        rk.SetValue("QuietUninstallString", $"{strUninst} Silent NoAll");
                        rk.Close();
                        logger.Info("Updated uninstall strings");
                        return true;
                    }
                    catch (Exception ex)
                    {
                        string res = ex.Message;
                        logger.Error(ex.Message + " Changing uninstall string");
                        rk.Close();
                        return false;
                    }
                }
            }
            return false;
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
            }
            else
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
                }
                else
                {
                    logger.Info("Subkey {0} was not added", subKeyPath);
                    return false;
                }
            }
            else
            { //there is no  key to add subkey
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
                }
                else
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
            }
            else
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
        /// public static void remove_repo_desc(string instDir, string repoName)
        /// Removes the repo descriptor file if exists.
        /// </summary>
        /// <param name="instDir">The inst dir.</param>
        /// <param name="repoName">Name of the repo descriptor file.</param>
        public static void remove_repo_desc(string instDir, string repoName)
        {
            //string path_t = Path.Combine(FD.sysDrive(), "Users");
            string path_l = Path.Combine(instDir, repoName);
            if (File.Exists(path_l))
            {
                File.Delete(path_l);
                logger.Info("repo description file exists, removing {0}", path_l);
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

            bool b_res = import_templ_task("master");

            // installing management template
            b_res = import_templ_task("management");
            string ssh_res = "";
            if (!b_res)
            {
                logger.Info("trying import management again");
                //need to remove previouis import
                string killcmd = "sudo kill `ps -ef | grep import | grep -v grep | awk '{print $2}'`";
                ssh_res = Deploy.SendSshCommand("127.0.0.1", 4567, "ubuntu", "ubuntu",
                    killcmd);
                logger.Info("Importing stuck first time, killing processes: {0}", ssh_res);

                if (ssh_res.Contains("Connection Error"))
                {
                    string kh_path = Path.Combine($"{ Program.inst_Dir}\\home", Environment.UserName, ".ssh", "known_hosts");
                    FD.edit_known_hosts(kh_path);

                    //restarting VM
                    if (!VMs.restart_vm(TC._cloneName))
                    { 
                        //can not restart VM
                        Program.ShowError("Can not restart VM, please check VM state and try to install later", "Can not start VM");
                        Program.form1.Visible = false;
                    };
                }
                //remove previously installed master
                ssh_res = Deploy.SendSshCommand("127.0.0.1", 4567, "ubuntu", "ubuntu",
                    "sudo subutai destroy master");
                logger.Info("Destroying master to import second time: {0}", ssh_res);

                ssh_res = Deploy.SendSshCommand("127.0.0.1", 4567, "ubuntu", "ubuntu",
                    "sudo subutai destroy management");
                logger.Info("Destroying management to import second time: {0}", ssh_res);

                b_res = import_templ_task("master");

                b_res = import_templ_task("management");
                if (!b_res)
                {
                    logger.Info("import management failed second time");
                    Program.form1.Invoke((MethodInvoker)delegate
                    {
                        Program.ShowError("Management template was not installed, installation failed, please try to install later", "Management template was not imported");
                        //Program.form1.Visible = false;
                    });
                }
            }

            ssh_res = Deploy.SendSshCommand("127.0.0.1", 4567, "ubuntu", "ubuntu",
                "sudo bash subutai info ipaddr");
            //todo: delete old
            logger.Info("Import management address returned by subutai info: {0}", ssh_res);

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
        /// public static bool import_templ_task(string tname)
        /// import template in separate task to know if import is running
        /// </summary>
        /// <param name="tname">Template name</param>
        public static bool import_templ_task(string tname)
        {
            // Cancellation token to cancel watcher when import is finished or cancelled
            var tokenSource = new CancellationTokenSource();
            var token = tokenSource.Token;

            Deploy.StageReporter("", $"Importing {tname}");
            logger.Info("Importing {0}", tname);

            //Starting Watcher task as parent, import as child
            var watcher = Task.Factory.StartNew(() =>
            {
                string res0 = "";
                int cnt = 0; //counter for all checks
                int cnt_download = 0; //counter for stuck download - downloaded size not changed
                int cnt_ssh = 0; //counter for failed ssh - checks connection with RH
                logger.Info("Import {0}", tname);
                while (true)
                {
                    cnt++;
                    if (token.IsCancellationRequested)
                    {
                        logger.Info("Watcher cancelled");
                        break;
                    }
                    Thread.Sleep(10000);//checking every 10 seconds
                    string res = check_templ(tname);
                    logger.Info("res = {0}", res);
                    if (res.Contains("Connection Error"))
                    {
                        logger.Info("No ssh connection: {0}", res);
                        cnt_ssh++;
                        if (cnt_ssh > 5) //(~5 min)
                        {
                            logger.Info("Cancelling from watcher - no ssh connection");
                            imported = false;
                            tokenSource.Cancel();
                        }
                        continue;
                    }
                    else
                    {
                        cnt_ssh = 0;
                    }


                    if (res == res0)
                    {
                        //will check 5 times more
                        cnt_download++;
                        //wait 6*30 seconds - 3 minutes for connection recovered
                        if (cnt_download >= 6)
                        {
                            //if waiting more than 3 minutes - stop 
                            logger.Info("Cancelling from watcher - stuck download");
                            tokenSource.Cancel();
                        }
                    }
                    else
                    {
                        cnt_download = 0;
                    }
                    res0 = res;
                }
            }, token);

            var import = Task.Factory.StartNew(() =>
            {
                string ssh_res = Deploy.SendSshCommand("127.0.0.1", 4567,
                        "ubuntu", "ubuntu", $"sudo subutai -d import {tname} 2>&1 > {tname}_log");

                string stcode = Deploy.com_out(ssh_res, 0);
                string stcout = Deploy.com_out(ssh_res, 1);
                string sterr = Deploy.com_out(ssh_res, 2);

                logger.Info("Import {0}: {1}, code: {2}, err: {3}",
                    tname, ssh_res, stcode, sterr);

                if (stcode != "0") //&&  sterr != "Empty")
                {
                    return false;
                }
                return true;
            }, token);
            //Waiting
            while (import.Status != TaskStatus.RanToCompletion)
            {
                Thread.Sleep(5000);
                if (token.IsCancellationRequested)
                {
                    logger.Info("Will cancel from Import ");
                    break;
                }
            }

            logger.Info("Cancelling from import");
            tokenSource.Cancel();//cancel  watcher
            bool b_res = false;
            if (import.IsCompleted)
            {
                b_res = import.Result;
                imported = b_res;
            }
            return b_res;
        }

        /// <summary>
        /// private static  string check_templ(string tname)
        /// Check if import template is running, runs as separate task
        /// Checks sum of sizes of all *.tar.gz files in /mnt/liblxc/tmpdir
        /// </summary>
        /// <param name="tname">Template name</param>
        private static string check_templ(string tname)
        {

            string fname = $"{rhTemplatePlace}/*.tar.gz";
            string ssh_res = Deploy.SendSshCommand("127.0.0.1", 4567,
                    "ubuntu", "ubuntu", $"du -b {fname} --total | grep total"); //find size in bytes
            logger.Info("Checking RH import: {0}", ssh_res);
            string stcode = Deploy.com_out(ssh_res, 0);
            string stres = Deploy.com_out(ssh_res, 1);
            string sterr = Deploy.com_out(ssh_res, 2);
            return stres;
        }

        /// <summary>
        /// Creates tmpfs folder, uploads snap file and prepare-server.sh. Runs installation scripts.
        /// </summary>
        /// <param name="appDir">The application instalation directory.</param>
        /// <param name="vmName">Name of the VM.</param>
        public static void run_scripts(string appDir, string vmName)
        {
            bool b_res = false;
            // creating tmpfs folder
            b_res = create_tmpfs();
            if (!b_res)
            {
                Program.ShowError("Can not open ssh to create tmpfs, please check network and VM state and reinstall later", "No tmpfs");
                Program.form1.Visible = false;
            }

            // copying snap
            b_res = upload_files(appDir);
            logger.Info("Copying Subutai files: {0}, prepare-server.sh", TC.snapFile);
            if (!b_res)
            {
                Program.ShowError("Cannot upload Subutai files to RH, canceling", "Setting up RH");
                Program.form1.Visible = false;
            }

            // adopting prepare-server.sh
            if (!adopt_scripts())
            {
                Program.ShowError("Can not open ssh to adapt scripts, please check network and VM state and reinstall later", "No tmpfs");
                Program.form1.Visible = false;
            }
            
            // running prepare-server.sh script
            b_res = prepare_server_task(appDir);
            if (!b_res)
            {
                Program.ShowError("Can not open ssh to run scripts, please check network and VM state and reinstall later", "No prepare-server");
                Program.form1.Visible = false;
            }
            
            // configuring nic
            bool res_b = VMs.vm_reconfigure_nic(vmName);

            if (!res_b)
            {
                logger.Error("VM not started", "Can not start VM, please check VM state manually and report error");
                Program.ShowError("Can not start VM after NIC reconfiguration, please check network and VM state and report error", "VM not started");
                Program.form1.Visible = false;
            }
            
            //stop and start machine
            Deploy.StageReporter("", "Waiting for SSH");
            logger.Info("Waiting for SSH - 2");
            res_b = VMs.waiting_4ssh(vmName);
            if (!res_b)
            {
                logger.Info("SSH 2 false", "Can not open ssh, please check VM state manually and report error");
                Program.ShowError("Can not open ssh after NIC reconfiguration, please check network and VM state and reinstall later", "No SSH");
                Program.form1.Visible = false;
            }
        }

        /// <summary>
        /// Creates TMPFS.
        /// </summary>
        /// <returns></returns>
        public static bool create_tmpfs()
        {
            string ssh_res = "";
            // creating tmpfs folder
            Deploy.StageReporter("", "Creating tmps folder");
            ssh_res = Deploy.SendSshCommand("127.0.0.1", 4567, "ubuntu", "ubuntu", "mkdir tmpfs; sudo mount -t tmpfs -o size=1G tmpfs /home/ubuntu/tmpfs");
            logger.Info("Creating tmpfs folder: {0}", ssh_res);
            if (ssh_res.Contains("Connection Error"))
            {
                if (!VMs.restart_vm(TC._cloneName))
                {
                    Program.ShowError("Can not communicate with VM, please check network and VM state and reinstall later", "No SSH");
                }
                ssh_res = Deploy.SendSshCommand("127.0.0.1", 4567, "ubuntu", "ubuntu", "mkdir tmpfs; sudo mount -t tmpfs -o size=1G tmpfs /home/ubuntu/tmpfs");
                logger.Info("Creating tmpfs folder second time: {0}", ssh_res);
                if (ssh_res.Contains("Connection Error"))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Uploads files to RH.
        /// </summary>
        /// <param name="appDir">The application directory.</param>
        /// <returns></returns>
        public static bool upload_files(string appDir)
        {
            Deploy.StageReporter("", "Copying Subutai files");
            string ftp_res = Deploy.SendFileSftp("127.0.0.1", 4567, "ubuntu", "ubuntu", new List<string>() {
                $"{appDir}/redist/subutai/prepare-server.sh",
                $"{appDir}/redist/subutai/{TC.snapFile}"
                }, "/home/ubuntu/tmpfs");
            logger.Info("Copying Subutai files: {0}, prepare-server.sh", TC.snapFile);
            if (!ftp_res.Equals("Uploaded"))
            {
                ftp_res = Deploy.SendFileSftp("127.0.0.1", 4567, "ubuntu", "ubuntu", new List<string>() {
                    $"{appDir}/redist/subutai/prepare-server.sh",
                    $"{appDir}/redist/subutai/{TC.snapFile}"
                    }, "/home/ubuntu/tmpfs");
                if (!ftp_res.Equals("Uploaded"))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Adapts the scripts before running.
        /// </summary>
        /// <returns></returns>
        public static bool adopt_scripts()
        {
            string ssh_res = "";
            Deploy.StageReporter("", "Adapting installation scripts");
            ssh_res = Deploy.SendSshCommand("127.0.0.1", 4567, "ubuntu", "ubuntu", "sed -i 's/IPPLACEHOLDER/192.168.56.1/g' /home/ubuntu/tmpfs/prepare-server.sh");
            logger.Info("Adapting installation scripts: {0}", ssh_res);
            if (ssh_res.Contains("Connection Error"))
            {
                ssh_res = Deploy.SendSshCommand("127.0.0.1", 4567, "ubuntu", "ubuntu", "sed -i 's/IPPLACEHOLDER/192.168.56.1/g' /home/ubuntu/tmpfs/prepare-server.sh");
                logger.Info("Adapting installation scripts second time: {0}", ssh_res);
                if (ssh_res.Contains("Connection Error"))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Running prepares-server script.
        /// </summary>
        /// <returns></returns>
        public static bool prepare_server(string appDir)
        {
            // running prepare-server.sh script
            string ssh_res = "";
            string ssh_res_1 = "";
            bool b_res = false;
            Deploy.StageReporter("", "Running installation scripts");
            ssh_res = Deploy.SendSshCommand("127.0.0.1", 4567, "ubuntu", "ubuntu", "sudo bash /home/ubuntu/tmpfs/prepare-server.sh");
            logger.Info("Running installation scripts: {0}", ssh_res);
            if (ssh_res.Contains("Connection Error"))
            {
                if (!VMs.restart_vm(TC._cloneName))
                {
                    Program.ShowError("Can not communicate with VM, please check network and VM state and reinstall later", "No SSH");
                    Program.form1.Visible = false;
                }
                ssh_res = Deploy.SendSshCommand("127.0.0.1", 4567, "ubuntu", "ubuntu", "ls /home/ubuntu/tmpfs/prepare-server.sh");
                ssh_res_1 = Deploy.SendSshCommand("127.0.0.1", 4567, "ubuntu", "ubuntu", $"sudo ls bash /home/ubuntu/tmpfs/{TC.snapFile}");
                if (ssh_res.ToLower().Contains("no such file") || ssh_res_1.ToLower().Contains("no such file"))
                {
                    b_res = upload_files(appDir);
                    if (!b_res)
                    {
                        Program.ShowError("Cannot upload Subutai files to RH, canceling", "Setting up RH");
                        Program.form1.Visible = false;
                    }
                }

                logger.Info("Running installation scripts second time: {0}", ssh_res);
                if (ssh_res.Contains("Connection Error"))
                {
                    return false;
                }
            }
            // deploying peer options
            Thread.Sleep(5000);
            ssh_res = Deploy.SendSshCommand("127.0.0.1", 4567, "ubuntu", "ubuntu", "sudo sync;sync");
            Thread.Sleep(5000);
            return true;
        }

        /// <summary>
        /// Running prepares-server script with connection control.
        /// </summary>
        /// <returns></returns>
        public static bool prepare_server_task(string appDir)
        {
            // running prepare-server.sh script
            string ssh_res = "";
            string ssh_res_1 = "";
            bool b_res = false;
            Deploy.StageReporter("", "Running installation scripts");

            ssh_res = Deploy.SendSshCommand_task("127.0.0.1", 4567, "ubuntu", "ubuntu", "sudo bash /home/ubuntu/tmpfs/prepare-server.sh");
            logger.Info("Running installation scripts: {0}", ssh_res);
            if (ssh_res.Contains("Error"))
            {
                if (!VMs.restart_vm(TC._cloneName))
                {
                    Program.ShowError("Can not communicate with VM, please check network and VM state and reinstall later", "No SSH");
                    Program.form1.Visible = false;
                }
                ssh_res = Deploy.SendSshCommand("127.0.0.1", 4567, "ubuntu", "ubuntu", "sudo ls /home/ubuntu/tmpfs");
                if (ssh_res.ToLower().Contains("no such file"))
                {
                    b_res = create_tmpfs();
                    if (!b_res)
                    {
                        Program.ShowError("Can not open ssh to create tmpfs, please check network and VM state and reinstall later", "No tmpfs");
                        Program.form1.Visible = false;
                    }
                } else
                {
                    ssh_res =  Deploy.SendSshCommand("127.0.0.1", 4567, "ubuntu", "ubuntu", "sudo chmod -R 0777 /home/ubuntu/tmpfs");
                    logger.Info("Chmod tmpfs", ssh_res);
                }

                ssh_res = Deploy.SendSshCommand("127.0.0.1", 4567, "ubuntu", "ubuntu", "sudo ls /home/ubuntu/tmpfs/prepare-server.sh");
                ssh_res_1 = Deploy.SendSshCommand("127.0.0.1", 4567, "ubuntu", "ubuntu", $"sudo ls  /home/ubuntu/tmpfs/{TC.snapFile}");
                if (ssh_res.ToLower().Contains("no such file") || ssh_res_1.ToLower().Contains("no such file"))
                {
                    b_res = upload_files(appDir);
                    if (!b_res)
                    {
                        Program.ShowError("Cannot upload Subutai files to RH, canceling", "Setting up RH");
                        Program.form1.Visible = false;
                    }
                }

                b_res = adopt_scripts();
                if (!b_res)
                {
                    Program.ShowError("Can not open ssh to adapt scripts, please check network and VM state and reinstall later", "No tmpfs");
                    Program.form1.Visible = false;
                }

                Deploy.StageReporter("", "Running installation scripts");
                ssh_res = Deploy.SendSshCommand_task("127.0.0.1", 4567, "ubuntu", "ubuntu", "sudo bash /home/ubuntu/tmpfs/prepare-server.sh");
                logger.Info("Running installation scripts second time: {0}", b_res);
                if (ssh_res.Contains("Error"))
                {

                    return false;
                }
            }
            // deploying peer options
            Thread.Sleep(5000);
            ssh_res = Deploy.SendSshCommand("127.0.0.1", 4567, "ubuntu", "ubuntu", "sudo sync;sync");
            Thread.Sleep(5000);
            return true;
        }
    }
}