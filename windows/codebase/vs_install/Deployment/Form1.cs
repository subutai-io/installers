using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using NLog;
using Deployment.items;
using Renci.SshNet;


namespace Deployment
{
    public partial class Form1 : DevExpress.XtraEditors.XtraForm
    {
        private readonly string[] _args = Environment.GetCommandLineArgs();
        private readonly Dictionary<string, string> _arguments = new Dictionary<string, string>();
        public readonly Deploy _deploy;
        public readonly Dictionary<string, KurjunFileInfo> PrerequisiteFilesInfo = new Dictionary<string, KurjunFileInfo>();

        private readonly string _cloneName = $"subutai-{DateTime.Now.ToString("yyyyMMddhhmm")}";
        private readonly PrivateKeyFile[] _privateKeys = new PrivateKeyFile[] { };

        private static NLog.Logger logger = LogManager.GetCurrentClassLogger();

        private static int stage_counter = 0;
        public  int finished = 0;
        public static string snapFile = "";

        private void ParseArguments()
        {
            foreach (var splitted in _args.Select(argument => argument.Split(new[] { "=" }, StringSplitOptions.None)).Where(splitted => splitted.Length == 2))
            {
                _arguments[splitted[0]] = splitted[1];
                logger.Info("Parsing arguments:  {0} =  {1}", splitted[0], splitted[1] );
            }
        }

        public Form1()
        {
            logger.Info("version = {0}", $"{ DateTime.Now.ToString("yyyyMMddhhmm")}");
            InitializeComponent();
            ParseArguments();
            _deploy = new Deploy(_arguments);
            timer1.Start();
        }
        public static void StageReporter(string stageName, string subStageName)
        {
            Program.form1.Invoke((MethodInvoker)delegate
            {
                if (stageName != "")
                {
                    Program.form1.labelControl1.Text = stageName;
                }
                if (subStageName != "")
                {
                    Program.form1.progressPanel1.Description = subStageName;
                }
            });
        }

     
        #region TASKS FACTORY

        private void TaskFactory(object sender, AsyncCompletedEventArgs e)
        {
            Task.Factory.StartNew(() =>
            {
                logger.Info("Starting task factory");
            })
               .ContinueWith((prevTask) =>
               {
                   logger.Info("Stage: {0} {1}", _arguments["network-installation"].ToLower(), "checkmd5");
                   if (_arguments["network-installation"].ToLower() == "true")
                   {
                       check_md5();
                   }
                   stage_counter++;
                   logger.Info("Stage checkmd5: {0}", stage_counter);
               })

               .ContinueWith((prevTask) =>
               {
                   Exception ne = (Exception)e.Error;
                   logger.Error(ne.Message, "checkmd5");
                   finished = 3;
                   Program.ShowError(ne.Message, "checkmd5");
               }, TaskContinuationOptions.OnlyOnFaulted)

               .ContinueWith((prevTask) =>
               {
                   if (_arguments["network-installation"].ToLower() == "true")
                   {
                       unzip_extracted();
                       logger.Info("Stage unzip: {0}", "unzip-extracted");
                   }
                   stage_counter++;
                   logger.Info("Stage: {0}", stage_counter);
               }, TaskContinuationOptions.NotOnFaulted)

                              //
                              .ContinueWith((prevTask) =>
                              {
                                  Exception ne = (Exception)e.Error;
                                  logger.Error(ne.Message, "unzipping");
                                  //finished = 3;
                                  Program.ShowError(ne.Message, "unzipping");
                              }, TaskContinuationOptions.OnlyOnFaulted)

                              //               .ContinueWith((prevTask) =>
                              //               {
                              //                   check_files();
                              //                   stage_counter++;
                              //                   logger.Info("Stage check_files: {0}", stage_counter);
                              //               }, TaskContinuationOptions.NotOnFaulted)

                              /////////////////////////
                              //               .ContinueWith((prevTask) =>
                              //               {
                              //                   Exception ne = (Exception)e.Error;
                              //                   logger.Error(ne.Message, "check files");
                              //                   finished = 3;
                              //                   Program.ShowError(ne.Message, "check files");
                              //               }, TaskContinuationOptions.OnlyOnFaulted)

                              .ContinueWith((prevTask) =>
                              {
                                  if (_arguments["params"].Contains("deploy-redist"))
                                  {
                                      deploy_redist();
                                      logger.Info("Stage deploy-redist: {0}", "deploy-redist");
                                  }
                                  stage_counter++;
                                  logger.Info("Stage: {0}", stage_counter);
                              }, TaskContinuationOptions.NotOnFaulted)

                               .ContinueWith((prevTask) =>
                               {
                                   Exception ne = (Exception)e.Error;
                                   logger.Error(ne.Message, "deploy-redist");
                                   //finished = 3;
                                   Program.ShowError(ne.Message, "deploy-redist");
                               }, TaskContinuationOptions.OnlyOnFaulted)

                              .ContinueWith((prevTask) =>
                              {
                                  if (_arguments["params"].Contains("prepare-vbox"))
                                  {
                                      prepare_vbox();
                                      logger.Info("Stage: {0}", "prepare-vbox");
                                  }
                                  stage_counter++;
                                  logger.Info("Stage prepate-vbox: {0}", stage_counter);
                              }, TaskContinuationOptions.NotOnFaulted)

                               .ContinueWith((prevTask) =>
                               {
                                   Exception ne = (Exception)e.Error;
                                   logger.Error(ne.Message, "prepare-vbox");
                                   //finished = 3;
                                   Program.ShowError(ne.Message, "prepare-vbox");
                               }, TaskContinuationOptions.OnlyOnFaulted)

                              .ContinueWith((prevTask) =>
                              {
                                  if (_arguments["params"].Contains("prepare-rh"))
                                  {
                                      prepare_rh();
                                      //logger.Info("Stage: {0}", "prepare-rh");
                                  }
                                  stage_counter++;
                                  logger.Info("Stage prepare-rh: {0}", stage_counter);
                              }, TaskContinuationOptions.NotOnFaulted)

                               .ContinueWith((prevTask) =>
                               {
                                   Exception ne = (Exception)e.Error;
                                   logger.Error(ne.Message, "prepare-rh");
                                   Program.ShowError(ne.Message, "prepare-rh");
                               }, TaskContinuationOptions.OnlyOnFaulted)

                              .ContinueWith((prevTask) =>
                              {
                                  if (_arguments["params"].Contains("deploy-p2p"))
                                  {
                                      deploy_p2p();
                                      logger.Info("Stage: {0}", "deploy-p2p");
                                  }

                                  stage_counter++;
                                  logger.Info("Stage deploy-p2p: {0}", stage_counter);
                              }, TaskContinuationOptions.NotOnFaulted)

                               .ContinueWith((prevTask) =>
                               {
                                   Exception ne = (Exception)e.Error;
                                   logger.Error(ne.Message, "deploy-p2p");
                                   Program.ShowError(ne.Message, "deploy-p2p");
                               }, TaskContinuationOptions.OnlyOnFaulted)
//
               .ContinueWith((prevTask) =>
               {
                   stage_counter = 1;
                   finished = 1;
                   logger.Info("stage_counter = {0}", stage_counter);
                   Program.form1.Invoke((MethodInvoker) delegate
                   {
                       logger.Info("form1.invoke");
                       Program.form1.Visible = false;
                   });

                   Program.form2.Invoke((MethodInvoker)delegate
                   {
                      //logger.Info("show finished = {0}", finished);
                      InstallationFinished form2 = new InstallationFinished("complete", _arguments["appDir"]);
                      form2.Show();
                      //show_finished();
                    });
               }, TaskContinuationOptions.NotOnFaulted)
               .ContinueWith((prevTask) =>
               {
                   logger.Info("finished = {0}", finished);
                   if (finished == 1 || finished == 11)
                       Deploy.LaunchCommandLineApp($"{_arguments["appDir"]}bin\\tray\\SubutaiTray.exe", "");
               });
        }
        #endregion

        #region TASK FACTORY COMPONENTS
        private void download_repo()
        {
            //DOWNLOAD REPO
            StageReporter("Downloading prerequisites", "");

            Deploy.HideMarquee();
            logger.Info("Downloading repo_descriptor");
            download_description_file("repo_descriptor");
            string regfile = Path.Combine(FD.logDir(), "subutai-clean-registry.reg");
            download_file(regfile);
            
        }

        private void download_description_file(String arg_name)
        {
            StageReporter("", "Getting description file");
            _deploy.DownloadFile(
                url: _arguments["kurjunUrl"], 
                destination: $"{_arguments["appDir"]}{_arguments[arg_name]}", 
                onComplete: download_prerequisites, 
                report: "Getting repo descriptor",
                async: true, //true
                kurjun: true);
        }

        private void download_file(String file_name)
        {
            StageReporter("", $"Getting file {file_name}");
            logger.Info("Getting  file");
            _deploy.DownloadFile(
                url: _arguments["kurjunUrl"],
                destination: $"{file_name}",
                onComplete: null,
                report: $"Getting file {file_name}",
                async: true,
                kurjun: true);
        }

        private int _prerequisitesDownloaded = 0;

        private void download_prerequisites(object sender, AsyncCompletedEventArgs e)
        {
            if (e != null)
            {
                if (e.Cancelled)
                {
                    logger.Error("File download cancelled");
                    Program.form1.Visible = false;
                }

                if (e.Error != null && _prerequisitesDownloaded > 0)
                {
                    if (e.Error is WebException)
                    {
                        WebException we = (WebException)e.Error;
                        logger.Error(we.Message, "File download error");
                        Program.ShowError("File Download error, uninstalling partially installed Subutai Social", 
                            we.Message);
                        Program.form1.Visible = false;
                    }
                    else
                    {
                        Exception ne = (Exception)e.Error;
                        logger.Error(ne.Message);
                        Program.ShowError("Download error, please uninstall partially installed Subutai Social", 
                            ne.Message);
                        Program.form1.Visible = false;
                     }
                }
            } else // e == null
            {
               logger.Info("File not downloaded");
            }
            if (!File.Exists($"{_arguments["appDir"]}{_arguments["repo_descriptor"]}"))
            {
                Environment.Exit(1);
            }
            var rows = File.ReadAllLines($"{_arguments["appDir"]}{_arguments["repo_descriptor"]}");

            var row = rows[_prerequisitesDownloaded];
            var folderFile = row.Split(new[] {"|"}, StringSplitOptions.None);

            var folder = folderFile[0].Trim();
            var file = folderFile[1].Trim();
            if (file.Contains("tray") && _arguments["params"].Contains("dev"))
            {
                file = file.Replace("tray.", "tray-dev.");
            }
                                   
            logger.Info("Downloading prerequisites: {0}.", $"{_arguments["appDir"]}{folder}/{file}");

            if (_prerequisitesDownloaded < rows.Length - 3) //.snap? (_prerequisitesDownloaded != rows.Length - 3) 
            {
                _prerequisitesDownloaded++;
                _deploy.DownloadFile(
                    url: _arguments["kurjunUrl"],
                    destination: $"{_arguments["appDir"]}{folder}/{file}",
                    onComplete: download_prerequisites,
                    report: $"Getting {file}",
                    async: true,
                    kurjun: true
                    );
            }
            else //if (_prerequisitesDownloaded == rows.Length - 3) //snap
            {
                var destfile = file;
                if ((_arguments["params"].Contains("dev")) || (_arguments["params"].Contains("master")))
                {
                    if (_arguments["params"].Contains("dev"))
                    {
                        _prerequisitesDownloaded++;
                    } else //master
                    {
                        _prerequisitesDownloaded += 2;
                    }
                    row = rows[_prerequisitesDownloaded];
                    folderFile = row.Split(new[] { "|" }, StringSplitOptions.None);
                    folder = folderFile[0].Trim();
                    file = folderFile[1].Trim();
                }
                snapFile = file;
                 _deploy.DownloadFile(
                    url: _arguments["kurjunUrl"],
                    destination: $"{_arguments["appDir"]}{folder}/{file}",
                    onComplete: TaskFactory,
                    report: $"Getting {file}",
                    async: true,
                    kurjun: true);
            } 
       }

        private void check_md5()
        {
            //verify downloaded 
            StageReporter("Verifying MD5", "");
            Deploy.HideMarquee();
            foreach (var info in PrerequisiteFilesInfo)
            {
                var filepath = info.Key;
                var filename = Path.GetFileName(info.Key);
                StageReporter("", $"Checking {filepath}");
                var kurjunFileInfo = info.Value;
                var calculatedMd5 = Deploy.Calc_md5(filepath, upperCase: false);
                if (calculatedMd5 == "-1")
                {
                    logger.Info("File {0} does not exist: {1}", filepath, calculatedMd5);
                    continue;
                }
                
                if (calculatedMd5 != kurjunFileInfo.id.Replace("raw.", ""))
                {
                    logger.Error("Verification of MD5 checksums for {0} failed: calc = {1}", filepath, calculatedMd5);
                }
            }
        }
        private void unzip_extracted()
        {
            // UNZIP FILES
            StageReporter("Extracting", "");
            logger.Info("Unzipping");
            Deploy.HideMarquee();
            _deploy.unzip_files(_arguments["appDir"]);
        }

        private void deploy_redist()
        {
            // DEPLOY REDISTRIBUTABLES
            StageReporter("Installing redistributables", "");
            logger.Info("Installing redistributables");
            Deploy.ShowMarquee();
            string appDir = _arguments["appDir"];

            StageReporter("", "TAP driver");
            Inst.inst_TAP(appDir);

            string res = "";
            StageReporter("", "MS Visual C++");
            res = Deploy.LaunchCommandLineApp($"{_arguments["appDir"]}\\redist\\vcredist64.exe", "/install /quiet");
            logger.Info("MS Visual C++: {0}", res);

            StageReporter("", "Chrome browser");
            Inst.inst_Chrome(appDir);
 
            StageReporter("", "Checking Chrome E2E extension");
            Inst.inst_E2E();
  
            StageReporter("", "Virtual Box");
            Inst.inst_VBox(appDir);
        }

        private void prepare_vbox()
        {
            // PREPARE VBOX
            StageReporter("Preparing Virtual Box", "");
            StageReporter("", "Configuring network");
            logger.Info("Preparing Virtual Box");
            Deploy.ShowMarquee();
            logger.Info("Marqee");
            // prepare NAT network
            Deploy.LaunchCommandLineApp("VboxManage.exe", "natnetwork add --netname natnet1 --network '10.0.5.0/24' --enable --dhcp on");
            logger.Info("vboxmanage natnetwork add --netname natnet1 --network '10.0.5.0/24 --enable --dhcp on");

            // import OVAs
            StageReporter("", "Importing Snappy");
            Deploy.LaunchCommandLineApp("vboxmanage", $"import {_arguments["appDir"]}ova\\snappy.ova");
            logger.Info("vboxmanage import snappy.ova");
        }

        private void prepare_rh()
        {
            // PREPARE RH
            StageReporter("Preparing resource host", "");
            logger.Info("Preparing resource host");
            string ssh_res = "";
            string res = "";
            Deploy.ShowMarquee();

            // clone VM
            StageReporter("", "Cloning VM");
            VMs.clone_vm(_cloneName);

            //Preparing temporary network interfaces to upload and run installation scripts
            StageReporter("", "Preparing NIC - NAT");
            logger.Info("Preparing NIC-NAT");
            res = Deploy.LaunchCommandLineApp("vboxmanage",
                $"modifyvm {_cloneName} --nic1 nat --cableconnected1 on --natpf1 'ssh-fwd,tcp,,4567,,22' --natpf1 'mgt-fwd,tcp,,9999,,8443'");
            logger.Info("nic 1 --nat: {0}", res);
            res = Deploy.LaunchCommandLineApp("vboxmanage", $"modifyvm {_cloneName} --nic4 none");

            // set RAM
            StageReporter("", "Setting RAM");
            VMs.vm_set_RAM(_cloneName);

            //set number of cores
            StageReporter("", "Setting number of processors");
            VMs.vm_set_CPUs(_cloneName);

            //set UTC timezone
            StageReporter("", "Setting timezone");
            res = Deploy.LaunchCommandLineApp("vboxmanage", $"modifyvm {_cloneName} --rtcuseutc on");
            logger.Info("vboxmanage modifyvm {0} --rtcuseutc: {1}", _cloneName, res);
            Thread.Sleep(4000);
           
            //start VM, will try 2 times
            StageReporter("", "Starting VM");
            if (!VMs.start_vm(_cloneName))
            {
                VMs.stop_vm(_cloneName);
                Thread.Sleep(10000);
                if (!VMs.start_vm(_cloneName))
                {
                    logger.Error("Can not start VM, please try to start manualy", "Waiting for SSH");
                    Program.ShowError("Can not start VM, please try to start manualy", "Waiting for SSH");
                    Program.form1.Visible = false;
                }
            }

            StageReporter("Setting up peer", "");
            logger.Info("Setting up peer");
            // waiting SSH session
            StageReporter("", "Waiting for SSH ");
            logger.Info("Waiting for SSH 1");
            bool res_b = VMs.waiting_4ssh(_cloneName);
            if (!res_b)
            {
                logger.Error("SSH 1 false","Can not open ssh, please check VM state manually and report error");
                Program.ShowError("Can not open ssh, please check VM state manually and report error", "Waiting for SSH");
                Program.form1.Visible = false;
            }
            // DEPLOY PEER
            VMs.run_scripts(_arguments["appDir"], _cloneName);

            StageReporter("", "Setting peer options");
            logger.Info("Setting peer options");
            
            if (_arguments["peer"] != "rh-only")
            {
                StageReporter("Preparing management host", "");
                logger.Info("Preparing management host");
                
                if (_arguments["peer"] == "trial")
                {
                    logger.Info("trial - installing management host");
                    if (_arguments["network-installation"].ToLower() != "true")
                    {
                        // setting iptables rules
                        StageReporter("", "Restricting SSH only");
                        Deploy.SendSshCommand("127.0.0.1", 4567, "ubuntu", _privateKeys,
                            "sudo iptables -P INPUT DROP; sudo iptables -P OUTPUT DROP; sudo iptables -A INPUT -p tcp -m tcp --dport 22 -j ACCEPT; sudo iptables -A OUTPUT -p tcp --sport 22 -m state --state ESTABLISHED, RELATED -j ACCEPT");
                    }

                    if (_arguments["network-installation"].ToLower() == "true")
                    {
                        //installing master template
                        StageReporter("", "Importing master");
                        logger.Info("Importing master");
                        VMs.import_templ("master");

                        // installing management template
                        StageReporter("", "Importing management");
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
                             
                        ssh_res = Deploy.SendSshCommand("127.0.0.1", 4567, "ubuntu", "ubuntu", "sudo bash subutai management_network detect");
                        logger.Info("Import management address: {0}", ssh_res);

                        if (Deploy.com_out(ssh_res, 0) != "0")
                        {
                            logger.Error("import management failed second time", "Management template was not installed");
                            Program.ShowError("Management template was not installed, installation failed, removing", "Management template was not imported");
                            Program.form1.Visible = false;
                        }
                    }
                    else
                    {
                        //installing master template
                        StageReporter("", "Importing master");
                        logger.Info("Importing master");
                        ssh_res = Deploy.SendSshCommand("127.0.0.1", 4567, "ubuntu", _privateKeys, "sudo echo -e 'y' | sudo subutai -d import master 2>&1 > master_log");
                        logger.Info("Import master: {0}", ssh_res);
                        ssh_res = Deploy.SendSshCommand("127.0.0.1", 4567, "ubuntu", "ubuntu", "ls -l master_log| wc -l");
                        logger.Info("Import master log: {0}", ssh_res);

                        // installing management template
                        StageReporter("", "Importing management");
                        ssh_res = Deploy.SendSshCommand("127.0.0.1", 4567, "ubuntu", _privateKeys, "sudo echo -e 'y' | sudo subutai -d import management 2>&1 > management_log ");
                        logger.Info("Import management: {0}", ssh_res);
                        if (Deploy.com_out(ssh_res, 0) != "0")
                        {
                            logger.Error("Management template was not installed");
                            Program.ShowError("Management template was not installed, instllation failed, please uninstall and try to install later", "Management template was not imported");
                            Program.form1.Visible = false;
                        }
                    }

                    if (_arguments["network-installation"].ToLower() != "true")
                    {
                        // setting iptables rules
                        StageReporter("", "Allowing TCP trafic");
                        logger.Info("Allowing TCP trafic");
                        Deploy.SendSshCommand("127.0.0.1", 4567, "ubuntu", "ubuntu", "sudo iptables - P INPUT ACCEPT; sudo iptables -P OUTPUT ACCEPT");
                    }
                }
            }
            Deploy.SendSshCommand("127.0.0.1", 4567, "ubuntu", "ubuntu", "sudo sync;sync");
         }

 
        private void deploy_p2p()
        {
            // DEPLOYING P2P SERVICE
            StageReporter("Installing P2P service", "");
            Deploy.ShowMarquee();
            _arguments["appDir"] = "C:\\Subutai\\";
            string res = "";
            var name = "Subutai Social P2P";
            string name1 = "Subutai Social P2P";
            var binPath = $"{_arguments["appDir"]}bin\\p2p.exe";
            const string binArgument = "daemon";

            // installing service
            StageReporter("", "Installing P2P service");
            res = Deploy.LaunchCommandLineApp("nssm", $"install \"{name}\" \"{binPath}\" \"{binArgument}\"");
            logger.Info("Installing P2P service: {0}", res);

            StageReporter("", "Adding P2P service to firewall exceptions");
            Net.set_fw_rules(name1, "p2p_s",true);

            Net.set_fw_rules(binPath, "p2p", false);
 
            Net.set_fw_rules($"{_arguments["appDir"]}bin\\tray\\SubutaiTray.exe", "SubutaiTray", false);

            //Configuring service logs
            string logPath = FD.logDir();
            logPath = Path.Combine(logPath, "p2p_log.txt");
            logger.Info("Logs are in {0}", logPath);
            Net.p2p_logs_config("Subutai Social P2P", logPath);

            StageReporter("", "Starting P2P service");
            res = Deploy.LaunchCommandLineApp("nssm", $"start \"{name}\"");
            logger.Info("Starting P2P service: {0}", res);
            Thread.Sleep(2000);
              
            //configuring service restart on failure
            StageReporter("", "Configuring P2P service");
            res = Deploy.LaunchCommandLineApp("sc", $"failure \"{name}\" actions= restart/10000/restart/15000/restart/18000 reset= 86400");
            logger.Info("Configuring P2P service {0}", res);
            Thread.Sleep(5000);
            finished = 1;
           }
        #endregion

  
        private void Form1_Load(object sender, EventArgs e)
        {
            DevExpress.Skins.SkinManager.EnableFormSkins();
            DevExpress.LookAndFeel.LookAndFeelHelper.ForceDefaultLookAndFeelChanged();

            _deploy.SetEnvironmentVariables();

            if (_arguments["network-installation"].ToLower() == "true")
            {
                //DOWNLOAD REPO
                StageReporter("Downloading prerequisites", "");
                Deploy.HideMarquee();
                download_repo();
            }
         }

        private void timer1_Tick(object sender, EventArgs e)
        {
            Invoke((MethodInvoker) Refresh);
        }
        
        private void show_finished()
        {
            string st = " finished";
            switch (finished)
            {
                case 0:
                    st = "failed";
                    break;
                case 1:
                    st = "complete";
                    break;
                case 2:
                    st = "cancelled";
                    break;
                case 3:
                    st = "failed";
                    break;
            }
            
            logger.Info("show finished = {0}", finished);
            Program.form1.Visible = false;
            InstallationFinished form2 = new InstallationFinished(st, _arguments["appDir"]);
            if (finished != 11 )//&& finished !=1)
            {
                if (finished == 1)
                {
                    finished = 11;
                    form2.Show();
                } else
                {
                    finished = 11;
                    form2.ShowDialog();
                    Application.Exit();
                }
            }
        }

        private void Form1_VisibleChanged(object sender, EventArgs e)
        {
            int Vis = -1;
            if (((Form1)sender).Visible == false)
            {
                Vis = 0;
            } else
            {
                Vis = 1;
            }

            logger.Info("Visible changed - check changes, visible = {0}, finished = {1}", Vis, finished);
            if (((Form1)sender).Visible == false && (finished == 0 || finished == 1))
            {
                logger.Info("Visible false");
                show_finished();
            }
         }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing) 
            {
                logger.Info("Closed by user");
                switch (finished)
                {
                    case 0:
                        {
                            finished = 2;
                            logger.Info("FormClosing: Installation cancelled");
                            e.Cancel = false;
                            show_finished();
                        }
                        break;
                    case 1:
                        logger.Info("FormClosing: Installation finished");
                        break;
                    case 2:
                        logger.Info("FormClosing: Installation cancelled");
                        break;
                    case 3:
                        logger.Info("FormClosing: Installation error");
                        break;
                    case 11:
                        logger.Info("FormClosing: Installation finished");
                        break;
                }
            }
        }
    }
}
