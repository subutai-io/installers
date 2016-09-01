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
        public static string[] rows;
        public static string installation_type = "";

        private readonly string _cloneName = $"subutai-{DateTime.Now.ToString("yyyyMMddhhmm")}";
        private readonly PrivateKeyFile[] _privateKeys = new PrivateKeyFile[] { };
        private static CancellationTokenSource tokenSource = new CancellationTokenSource();

        private static NLog.Logger logger = LogManager.GetCurrentClassLogger();

        private static int stage_counter = 0;
        public  int finished = 0;
        private string st = " finished";
        public static string snapFile = "";


        private void ParseArguments()
        {
            foreach (var splitted in _args.Select(argument => argument.Split(new[] { "=" }, StringSplitOptions.None)).Where(splitted => splitted.Length == 2))
            {
                _arguments[splitted[0]] = splitted[1];
                logger.Info("Arguments:  {0} =  {1}", splitted[0], splitted[1] );
            }
        }

        public Form1()
        {
            logger.Info("date = {0}", $"{ DateTime.Now.ToString("yyyyMMddhhmm")}");
            InitializeComponent();
            ParseArguments();
            _deploy = new Deploy(_arguments);
            timer1.Start();
        }

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
                //Deploy.ShowMarquee();
                Inst.remove_ssh(_arguments["appDir"]);
                download_repo();
            }
            //Inst.inst_E2E();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            Invoke((MethodInvoker)Refresh);
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
            //var token = tokenSource.Token;
            object state = "";

            Task.Factory.StartNew(() =>
            {
                logger.Info("Starting task factory");
            })
               .ContinueWith((prevTask) =>
               {
                   
                   // Handle any exceptions to prevent UnobservedTaskException.             
                   logger.Info("Stage: {0} {1}", _arguments["network-installation"].ToLower(), "checkmd5");
                   if (_arguments["network-installation"].ToLower() == "true")
                   {
                       check_md5();
                   }
                   stage_counter++;
                   logger.Info("Stage checkmd5: {0}", stage_counter);
               }, TaskContinuationOptions.NotOnFaulted)

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
                   logger.Info("Stage unzip: {0}", stage_counter);
               },  TaskContinuationOptions.NotOnFaulted)

                .ContinueWith((prevTask) =>
                {
                    Exception ne = (Exception)e.Error;
                    logger.Error(ne.Message, "unzipping");
                    //finished = 3;
                    Program.ShowError(ne.Message, "unzipping");
                }, TaskContinuationOptions.OnlyOnFaulted)

                .ContinueWith((prevTask) =>
                {
                    if (_arguments["params"].Contains("deploy-redist"))
                    {
                        deploy_redist();
                        logger.Info("Stage deploy-redist: {0}", "deploy-redist");
                    }
                    stage_counter++;
                    logger.Info("Stage deploy redistributables: {0}", stage_counter);
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
                    if (_arguments["params"].Contains("prepare-vbox") && _arguments["peer"] != "client-only")
                    {
                        prepare_vbox();
                        logger.Info("Stage prepare vbox: {0}", "prepare-vbox");
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
                    if (_arguments["params"].Contains("prepare-rh") && _arguments["peer"] != "client-only")
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
                    if (_arguments["params"].Contains("deploy-p2p") && _arguments["peer"] != "rh-only")
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

               .ContinueWith((prevTask) =>
               {
                   //stage_counter = 1;
                   //finished = 1;
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
                       logger.Info("will show form2 from task factory");
                       form2.Show();
                       //show_finished();
                   });
               }, TaskContinuationOptions.NotOnFaulted)
               .ContinueWith((prevTask) =>
               {
                   logger.Info("finished = {0}", finished);
                   if (finished == 11  &&  st == "complete"  && _arguments["peer"] != "rh-only" ) //|| finished == 11)
                       Deploy.LaunchCommandLineApp($"{_arguments["appDir"]}bin\\tray\\SubutaiTray.exe", "");
                   //Environment.Exit(0);
               });
        }
        #endregion

        #region TASK FACTORY COMPONENTS
        private void download_repo()
        {
            //DOWNLOAD REPO
            StageReporter("Downloading prerequisites", "");

            //Deploy.HideMarquee();
            logger.Info("Downloading repo_descriptor");
            download_description_file("repo_descriptor");
            if (_arguments["params"].Contains("dev"))
            {
                installation_type = "dev";
            } else if (_arguments["params"].Contains("master"))
            {
                installation_type = "master";
            } else
            {
                installation_type = "prod";
            }
            StageReporter("", $"Creating download list for installation type: {installation_type}");

            rows = FD.repo_rows($"{_arguments["appDir"]}{_arguments["repo_descriptor"]}", _arguments["peer"],installation_type);
            string regfile = Path.Combine(FD.logDir(), "subutai-clean-registry.reg");
            Deploy.HideMarquee();
            download_file(regfile, download_prerequisites);
        }

        private void download_description_file(String arg_name)
        {
            StageReporter("", "Getting description file");
            _deploy.DownloadFile(
                url: _arguments["kurjunUrl"], 
                destination: $"{_arguments["appDir"]}{_arguments[arg_name]}", 
                onComplete: null, 
                report: "Getting repo descriptor",
                async: false, 
                kurjun: true);
        }

        private void download_file(String file_name, AsyncCompletedEventHandler cmplHandler)
        {
            StageReporter("", $"Getting file {file_name}");
            logger.Info("Getting  file");
            _deploy.DownloadFile(
                url: _arguments["kurjunUrl"],
                destination: file_name,
                onComplete: cmplHandler,
                report: $"Getting file {file_name}",
                async: true,
                kurjun: true);
        }

        private int _prerequisitesDownloaded = 0;

        private void download_prerequisites(object sender, AsyncCompletedEventArgs e)
        {
            //Check thread exception
            if (e != null)
            {
                if (e.Cancelled)
                {
                    logger.Error("File download cancelled");
                    finished = 2;
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
                        Program.ShowError("Download error, uninstalling partially installed Subutai Social", 
                            ne.Message);
                        Program.form1.Visible = false;
                     }
                }
            } else // e == null
            {
               logger.Info("File not downloaded");
            }
            
            //Start download
            var row = rows[_prerequisitesDownloaded];
            var folderFile = row.Split(new[] { "|" }, StringSplitOptions.None);

            var folder = folderFile[0].Trim();
            var file = folderFile[1].Trim();


            //download tray-dev if installing -dev version
            if (file.Contains("tray") && _arguments["params"].Contains("dev"))
            {
                file = file.Replace("tray.", "tray-dev.");
            }

            if (_prerequisitesDownloaded < rows.Length - 1) //For last row will change OnComplete
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
            else
            {//changed OnComplete
                var destfile = file;
                snapFile = file; //Last is .snap, need to remember name
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

            if (_arguments["peer"] != "rh-only") //install components if not installing RH only
            {
                string res = "";

                StageReporter("", "TAP driver");
                Inst.inst_TAP(appDir);

                StageReporter("", "MS Visual C++");
                res = Deploy.LaunchCommandLineApp($"{_arguments["appDir"]}redist\\vcredist64.exe", "/install /quiet");
                logger.Info("MS Visual C++: {0}", res);

                StageReporter("", "Chrome browser");
                Inst.inst_Chrome(appDir);

                StageReporter("", "Checking Chrome E2E extension");
                Inst.inst_E2E();

                StageReporter("", "SSH ");
                Inst.inst_ssh(Path.Combine(FD.sysDrive(), "Subutai"));
            }

            StageReporter("", "Virtual Box");//installing VBox if not Client-only installation
            if (_arguments["peer"] != "client-only")
            {
                Inst.inst_VBox(appDir);
            } 
         }



        private void prepare_vbox()
        {
            // PREPARE VBOX
            StageReporter("Preparing Virtual Box", "");
            StageReporter("", "Configuring network");
            logger.Info("Preparing Virtual Box");
            Deploy.ShowMarquee();
            // prepare NAT network
            string res = "";
            res = Deploy.LaunchCommandLineApp("vboxmanage", "natnetwork remove --netname natnet1 ");
            logger.Info("Removing NAT network: {0}", res);
            res = Deploy.LaunchCommandLineApp("vboxmanage", "natnetwork add --netname natnet1 --network '10.0.5.0/24' --enable --dhcp on");
            logger.Info("Configuring NAT network: {0}", res);
            //if (Deploy.com_out(res,2) == "Error")
            if (res.ToLower().Contains("error"))
            {
                logger.Error("Can not run command, please check if VirtualBox installed properly", "Configure VM");
                Program.ShowError("Can not configure VM, please check if VitrualBox installed properly", "Prepare VBox");
                Program.form1.Visible = false;
            }
            // import OVAs
            StageReporter("", "Importing Snappy");
            res = Deploy.LaunchCommandLineApp("vboxmanage", $"import {_arguments["appDir"]}ova\\snappy.ova");
            logger.Info("Importing snappy: {0}", Deploy.com_out(res, 0));
            if (res.ToLower().Contains("error"))
            {
                logger.Error("Can not run command, please check if VirtualBox installed properly", "Importing Snappy");
                Program.ShowError("Can not Import Snappy, please check if VitrualBox installed properly", "Prepare VBox");
                Program.form1.Visible = false;
            }
        }

        private void prepare_rh()
        {
            // PREPARE RH
            StageReporter("Preparing resource host", "");
            logger.Info("Preparing resource host");
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
            //if (res.ToLower().Contains("error"))
            //{
            //    logger.Error("Can not run command, please check if VirtualBox installed properly", "Importing Snappy");
            //    Program.ShowError("Can not modify VM, please check if VitrualBox installed properly", "Prepare VBox");
            //    Program.form1.Visible = false;
            //}
            res = Deploy.LaunchCommandLineApp("vboxmanage", $"modifyvm {_cloneName} --nic4 none");
            if (res.ToLower().Contains("error"))
            {
                logger.Error("Can not run command, please check if VirtualBox installed properly", "Importing Snappy");
                Program.ShowError("Can not modify VM, please check if VitrualBox installed properly", "Prepare VBox");
                Program.form1.Visible = false;
            }
            // set RAM
            StageReporter("", "Setting RAM");
            VMs.vm_set_RAM(_cloneName);

            //set number of cores
            StageReporter("", "Setting number of processors");
            VMs.vm_set_CPUs(_cloneName);

            //set UTC timezone
            StageReporter("", "Setting timezone");
            VMs.vm_set_timezone(_cloneName);
           
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
            
            if (_arguments["peer"] == "trial")
            {
                    StageReporter("Preparing management host", "");
                    logger.Info("Preparing management host");
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
                        Inst.install_mh_nw();
                    }
                    else
                    {
                        Inst.install_mh_lc(_privateKeys);
               }

               if (_arguments["network-installation"].ToLower() != "true")
               {
                        // setting iptables rules
                        StageReporter("", "Allowing TCP trafic");
                        logger.Info("Allowing TCP trafic");
                        Deploy.SendSshCommand("127.0.0.1", 4567, "ubuntu", "ubuntu", 
                            "sudo iptables - P INPUT ACCEPT; sudo iptables -P OUTPUT ACCEPT");
               }
               
            }
            if (_arguments["peer"] == "rh-only")
                    finished = 1;
            Deploy.SendSshCommand("127.0.0.1", 4567, "ubuntu", "ubuntu", "sudo sync;sync");
         }

 
        private void deploy_p2p()
        {
            // DEPLOYING P2P SERVICE
            StageReporter("Installing P2P service", "");
            Deploy.ShowMarquee();
            //_arguments["appDir"] = "C:\\Subutai\\"; for debug
            string name = "Subutai Social P2P";
            var binPath = $"{_arguments["appDir"]}bin\\p2p.exe";
            const string binArgument = "daemon";
            //Check if service is running and remove if yes
            Inst.service_stop(name);

            // installing service
            StageReporter("", "Installing P2P service");
            Inst.service_install(name, binPath, binArgument);

            StageReporter("", "Adding P2P service to firewall exceptions");
            Net.set_fw_rules(name, "p2p_s",true);

            Net.set_fw_rules(binPath, "p2p", false);
 
            Net.set_fw_rules($"{_arguments["appDir"]}bin\\tray\\SubutaiTray.exe", "SubutaiTray", false);

            //Configuring service logs
            StageReporter("", "Configuring P2P service logs");
            Inst.p2p_logs_config("Subutai Social P2P");

            //Starting P2P service 
            StageReporter("", "Starting P2P service");
            Inst.service_start(name);

            //configuring service restart on failure
            StageReporter("", "Configuring P2P service");
            Inst.service_config(name);
            if(_arguments["peer"] != "rh-only")
                finished = 1;
           }
        #endregion

  
        private void show_finished()
        {
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
                    tokenSource.Cancel();
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
                    logger.Info("will show form2 from sub");
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
                            //tokenSource.Cancel();
                            show_finished();
                        }
                        break;
                    case 1:
                        logger.Info("FormClosing: Installation finished");
                        break;
                    case 2:
                        logger.Info("FormClosing: Installation cancelled");
                        //tokenSource.Cancel();
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
