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
using File = System.IO.File;


namespace Deployment
{
    public partial class Form1 : DevExpress.XtraEditors.XtraForm
    {
        private readonly string[] _args = Environment.GetCommandLineArgs();
        private readonly Dictionary<string, string> _arguments = new Dictionary<string, string>();
        private readonly Deploy _deploy;
        public readonly Dictionary<string, KurjunFileInfo> PrerequisiteFilesInfo = new Dictionary<string, KurjunFileInfo>();

        private readonly string _cloneName = $"subutai-{DateTime.Now.ToString("yyyyMMddhhmm")}";
        private readonly PrivateKeyFile[] _privateKeys = new PrivateKeyFile[] { };

        private static NLog.Logger logger = LogManager.GetCurrentClassLogger();

        private static int stage_counter = 0;
        private static int finished = 0;

        private void ParseArguments()
        {
            foreach (var splitted in _args.Select(argument => argument.Split(new[] { "=" }, StringSplitOptions.None)).Where(splitted => splitted.Length == 2))
            {
                _arguments[splitted[0]] = splitted[1];
                logger.Info("Parsing arguments:  {0} =  {1}.", splitted[0], splitted[1] );
            }
        }

        public Form1()
        {
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
                   Program.ShowError(ne.Message, "checkmd5");
               }, TaskContinuationOptions.OnlyOnFaulted)

               .ContinueWith((prevTask) =>
               {
                   if (_arguments["network-installation"].ToLower() == "true")
                   {
                       unzip_extracted();
                       logger.Info("Stage unzip: {0} {1}", _arguments["network-installation"].ToLower(), "unzip-extracted");

                       //MessageBox.Show("Unzip extracted");
                   }
                   stage_counter++;
                   logger.Info("Stage: {0}", stage_counter);
               })

//////////////////////
               .ContinueWith((prevTask) =>
               {
                   Exception ne = (Exception)e.Error;
                   logger.Error(ne.Message, "unzipping");
                   Program.ShowError(ne.Message, "unzipping");
               }, TaskContinuationOptions.OnlyOnFaulted)

               .ContinueWith((prevTask) =>
               {
                   check_files();
                   stage_counter++;
                   logger.Info("Stage check_files: {0}", stage_counter);
               }, TaskContinuationOptions.OnlyOnRanToCompletion)

///////////////////////
               .ContinueWith((prevTask) =>
               {
                   Exception ne = (Exception)e.Error;
                   logger.Error(ne.Message, "check files");
                   Program.ShowError(ne.Message, "check files");
               }, TaskContinuationOptions.OnlyOnFaulted)

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
                    Program.ShowError(ne.Message, "prepare-vbox");
                }, TaskContinuationOptions.OnlyOnFaulted)

               .ContinueWith((prevTask) =>
               {
                   if (_arguments["params"].Contains("prepare-rh"))
                   {
                       prepare_rh();
                       //logger.Info("Stage: {0}", "prepare-rh");
                       //MessageBox.Show("Prepare RH");
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

               .ContinueWith((prevTask) =>
               {
                   logger.Info("stage_counter = {0}", stage_counter);
                   Program.form1.Invoke((MethodInvoker) delegate
                   {
                       logger.Info("form1.invoke");
                       Program.form1.Visible = false;
                   });

                   Program.form2.Invoke((MethodInvoker) delegate
                   {
                       logger.Info("form2.invoke");
                       show_finished();
                   });
               }, TaskContinuationOptions.NotOnFaulted)
               .ContinueWith((prevTask) =>
               {
                   if (finished == 1)
                       Deploy.LaunchCommandLineApp($"{_arguments["appDir"]}/bin/tray/SubutaiTray.exe", "");
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
            download_file("c:/temp/subutai-clean-registry.reg");
            
        }

        private void download_description_file(String arg_name)
        {
            StageReporter("", "Getting description file");
            _deploy.DownloadFile(
                url: _arguments["kurjunUrl"], 
                destination: $"{_arguments["appDir"]}/{_arguments[arg_name]}", 
                onComplete: download_prerequisites, 
                report: "Getting repo descriptor",
                async: true, //true
                kurjun: true);
            
        }

        private void download_file(String file_name)
        {
            StageReporter("", "Getting description file");
            logger.Info("Getting description file");
            _deploy.DownloadFile(
                url: _arguments["kurjunUrl"],
                destination: $"{file_name}",
                onComplete: null,
                report: "Getting target descriptor",
                async: true,
                kurjun: true);
        }

        private int _prerequisitesDownloaded = 0;

        private void download_prerequisites(object sender, AsyncCompletedEventArgs e)
        {
            //logger.Info("_prerequisitesDownloaded = {0}", _prerequisitesDownloaded.ToString());
            if (e != null)
            {
                //logger.Info("AsyncCompletedEventArgs e is not null");
                if (e.Cancelled)
                {
                    logger.Error("File download cancelled");
                    Program.form1.Visible = false;
                    Environment.Exit(1);
                }

                if (e.Error != null && _prerequisitesDownloaded > 0)
                {
                    if (e.Error is WebException)
                    {
                        WebException we = (WebException)e.Error;
                        logger.Error(we.Message);
                        Program.ShowError(we.Message, "File Download error, please uninstall partially installed Subutai Social");
                        Program.form1.Visible = false;
                        Environment.Exit(1);
                    }
                    else
                    {
                        Exception ne = (Exception)e.Error;
                        logger.Error(ne.Message);
                        Program.ShowError(ne.Message, "Download error, please uninstall partially installed Subutai Social");
                        Program.form1.Visible = false;
                        Environment.Exit(1);
                    }
                }
            } else // e == null
            {
                //if (_prerequisitesDownloaded != 0 && sender == null) //file was not downloaded
                //    _prerequisitesDownloaded++;
                //logger.Info("Sender is: {0}", sender.ToString());
                logger.Info("File not downloaded");
            }

            var rows = File.ReadAllLines($"{_arguments["appDir"]}/{_arguments["repo_descriptor"]}");

            var row = rows[_prerequisitesDownloaded];
            //logger.Info("_prerequisitesDownloaded = {0}, row: {1}", _prerequisitesDownloaded, row);

            var folderFile = row.Split(new[] {"|"}, StringSplitOptions.None);

            var folder = folderFile[0].Trim();
            var file = folderFile[1].Trim();
            if (file.Contains("tray") && _arguments["params"].Contains("dev"))
            {
                file = file.Replace("tray.", "tray-dev.");
            }
                                   
            logger.Info("Downloading prerequisites: {0}.", $"{_arguments["appDir"]}/{folder}/{file}");

            if (_prerequisitesDownloaded < rows.Length - 3) //.snap? (_prerequisitesDownloaded != rows.Length - 3) 
            {
                _prerequisitesDownloaded++;
                _deploy.DownloadFile(
                    url: _arguments["kurjunUrl"],
                    destination: $"{_arguments["appDir"]}/{folder}/{file}",
                    onComplete: download_prerequisites,
                    report: $"Getting {file}",
                    async: true,
                    kurjun: true
                    );
                //_prerequisitesDownloaded++;
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
                 _deploy.DownloadFile(
                    url: _arguments["kurjunUrl"],
                    destination: $"{_arguments["appDir"]}/{folder}/{file}",
                    onComplete: TaskFactory,
                    report: $"Getting {file}",
                    async: true,
                    kurjun: true);

            } 
       }

        private void check_md5()
        {
            //UNZIP REPO
            StageReporter("Verifying MD5", "");

            Deploy.HideMarquee();
            logger.Info("PrerequisiteFilesInfo: {0}", PrerequisiteFilesInfo.Count);
            foreach (var info in PrerequisiteFilesInfo)
            {
                var filepath = info.Key;
                var filename = Path.GetFileName(info.Key);
                var kurjunFileInfo = info.Value;
                var calculatedMd5 = Deploy.Calc_md5(filepath, upperCase: false);
                logger.Info("Checking md5: {0}//{1}", filepath, filename);
                StageReporter("", "Checking " + filename);
               
                //if (calculatedMd5 != kurjunFileInfo.id.Split(new [] {"."}, StringSplitOptions.None)[1])
                if (calculatedMd5 != kurjunFileInfo.id.Replace("raw.", ""))
                {
                    logger.Error("Verification of MD5 checksums for {0} failed. Interrupting installation.", filename);
                    Program.ShowError(
                        $"Verification of MD5 checksums for {filename} failed. Interrupting installation.", "MD5 checksums mismatch");
                    Program.form1.Visible = false;
                }
            }
            //logger.Info("md5 checked");
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
            string res = "";
            Deploy.ShowMarquee();
            StageReporter("", "TAP driver");
            if (_deploy.app_installed("TAP-Windows") == 0)
            {
                res = Deploy.LaunchCommandLineApp($"{_arguments["appDir"]}\\redist\\tap-driver.exe", "/S");
                logger.Info("TAP driver: {0}", res);
            } else
            {
                StageReporter("", "TAP driver already installed");
                logger.Info("TAP driver is already installed: {0}", res);
            }

            if (_deploy.app_installed("TAP-Windows") == 1)
            {
                var pathTAPin = Path.Combine(_arguments["appDir"], "redist");
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
           
            StageReporter("", "MS Visual C++");
            res = Deploy.LaunchCommandLineApp($"{_arguments["appDir"]}\\redist\\vcredist64.exe", "/install /quiet");
            logger.Info("MS Visual C++: {0}", res);

            //HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Clients\StartMenuInternet\Google Chrome
            if (_deploy.app_installed("Clients\\StartMenuInternet\\Google Chrome") == 0)
            {
                StageReporter("", "Chrome");
                res = Deploy.LaunchCommandLineApp("msiexec", $"/qn /i \"{_arguments["appDir"]}\\redist\\chrome.msi\"");
                logger.Info("Chrome: {0}", res);
            }
            else
            {
                StageReporter("", "Google\\Chrome is already installed");
                logger.Info("Google\\Chrome is already installed: {0}", res);
            }

            StageReporter("", "Virtual Box");
            if (_deploy.app_installed("Oracle\\VirtualBox") == 0)
            {
                res = Deploy.LaunchCommandLineApp($"{_arguments["appDir"]}\\redist\\virtualbox.exe", "--silent");
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
                StageReporter("", "Oracle\\VirtualBox is already installed");
                logger.Info("Oracle\\VirtualBox is already installed: {0}", res);
            }
        }

        private void prepare_vbox()
        {
            // PREPARE VBOX
            StageReporter("Preparing Virtual Box", "");
            logger.Info("Preparing Virtual Box");
            Deploy.ShowMarquee();
            // prepare NAT network
            Deploy.LaunchCommandLineApp("vboxmanage", "natnetwork add --netname natnet1 --network '10.0.5.0/24' --enable --dhcp on");
            logger.Info("vboxmanage natnetwork add --netname natnet1 --network '10.0.5.0/24 --enable --dhcp on");

            // import OVAs
            StageReporter("", "Importing Snappy");
            Deploy.LaunchCommandLineApp("vboxmanage", $"import {_arguments["appDir"]}\\ova\\snappy.ova");
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
            res = Deploy.LaunchCommandLineApp("vboxmanage", $"clonevm --register --name {_cloneName} snappy");
            logger.Info("vboxmanage clone vm --register --name {0} snappy: {1} ", _cloneName, res);
            ssh_res = Deploy.LaunchCommandLineApp("vboxmanage", $"unregistervm --delete snappy");
            logger.Info("vboxmanage unregistervm --delete snappy: {0}", res);

            StageReporter("", "Preparing NIC - NAT");
            logger.Info("Preparing NIC-NAT");
            res = Deploy.LaunchCommandLineApp("vboxmanage",
                $"modifyvm {_cloneName} --nic1 nat --cableconnected1 on --natpf1 'ssh-fwd,tcp,,4567,,22' --natpf1 'mgt-fwd,tcp,,9999,,8443'");
            logger.Info("nic 1 --nat: {0}", res);
            Deploy.LaunchCommandLineApp("vboxmanage", $"modifyvm {_cloneName} --nic4 none");

            // set RAM
            StageReporter("", "Setting RAM");

            var hostRam = new Microsoft.VisualBasic.Devices.ComputerInfo().TotalPhysicalMemory / 1024 / 1024;
            //ulong vmRam = 3072;
            ulong vmRam = 2048;
            //if (hostRam < 4100)
            //{
            //    vmRam = 1024;
            //}
            if ((hostRam <= 16500) && (hostRam > 8100))
            {
                vmRam = hostRam / 2;
            }
            else if (hostRam > 16500)
            {
                vmRam = 8124;
            }
            res = Deploy.LaunchCommandLineApp("vboxmanage", $"modifyvm {_cloneName} --memory {vmRam}");
            logger.Info("vboxmanage modifyvm {0} --memory {1}: {2}", _cloneName, vmRam, res);

            //number of cores
            StageReporter("", "Setting number of processors");
            int hostCores = Environment.ProcessorCount; //number of logical processors
            //textBox1.Text = "hostCores=" + hostCores.ToString();
            ulong vmCores = 2;
            if (hostCores > 4 && hostCores < 17) //to ensure that not > than halph phys processors will be used
            {
                vmCores = (ulong)hostCores / 2;
            }
            else if (hostCores > 16)
            {
                vmCores = 8;
            }

            //textBox1.Text = "vmCores=" + vmCores.ToString();
            res = Deploy.LaunchCommandLineApp("vboxmanage", $"modifyvm {_cloneName} --cpus {vmCores}");
            logger.Info("vboxmanage modifyvm {0} --cpus {1}: {2}", _cloneName, vmCores.ToString(), res);
            // time settings
            StageReporter("", "Setting timezone");
            res = Deploy.LaunchCommandLineApp("vboxmanage", $"modifyvm {_cloneName} --rtcuseutc on");
            logger.Info("vboxmanage modifyvm {0} --rtcuseutc: {1}", _cloneName, res);
            Thread.Sleep(4000);
           
            //start VM
            StageReporter("", "Starting VM");
            //res = Deploy.LaunchCommandLineApp("vboxmanage",
            //    $"startvm --type headless {_cloneName} ");
            //logger.Info("vboxmanage startvm --type headless {0}: {1}", _cloneName, res);

            if (!VMs.start_vm(_cloneName))
            {
                VMs.stop_vm(_cloneName);
                Thread.Sleep(10000);
                if (!VMs.start_vm(_cloneName))
                {
                    Program.ShowError("Can not start VM, please try to start manualy", "Waiting for SSH");
                    Program.form1.Visible = false;
                }
            }

            //res = Deploy.LaunchCommandLineApp("vboxmanage", $"startvm --type headless {_cloneName}");
            //logger.Info("VM {0} starting res: {1}", _cloneName, res);
            StageReporter("Setting up peer", "");
            logger.Info("Setting up peer");
            // waiting SSH session
            StageReporter("", "Waiting for SSH ");
            logger.Info("Waiting for SSH 1");
            bool res_b = VMs.waiting_4ssh(_cloneName);
            if (!res_b)
            {
                logger.Info("SSH 1 false");
                Program.ShowError("Can not open ssh, please check VM state manually and report error", "Waiting for SSH");
                Program.form1.Visible = false;
            }
            // DEPLOY PEER
   
            // creating tmpfs folder
            StageReporter("", "Creating tmps folder");
            ssh_res = Deploy.SendSshCommand("127.0.0.1", 4567, "ubuntu", "ubuntu", "mkdir tmpfs; mount -t tmpfs -o size=1G tmpfs/home/ubuntu/tmpfs");
            logger.Info("Creating tmpfs folder: {0}", ssh_res);
            // copying snap
            StageReporter("", "Copying Subutai SNAP");
            
            Deploy.SendFileSftp("127.0.0.1", 4567, "ubuntu", "ubuntu", new List<string>() {
                $"{_arguments["appDir"]}/redist/subutai/prepare-server.sh",
                $"{_arguments["appDir"]}/redist/subutai/subutai_4.0.0_amd64.snap"
                }, "/home/ubuntu/tmpfs");
            logger.Info("Copying Subutai SNAP, prepare-server.sh");

            // adopting prepare-server.sh
            StageReporter("", "Adapting installation scripts");
            ssh_res = Deploy.SendSshCommand("127.0.0.1", 4567, "ubuntu", "ubuntu", "sed -i 's/IPPLACEHOLDER/192.168.56.1/g' /home/ubuntu/tmpfs/prepare-server.sh");
            logger.Info("Adapting installation scripts: {0}", ssh_res);
            // running prepare-server.sh script
            StageReporter("", "Running installation scripts");
            ssh_res = Deploy.SendSshCommand("127.0.0.1", 4567, "ubuntu", "ubuntu", "sudo bash /home/ubuntu/tmpfs/prepare-server.sh");
            logger.Info("Running installation scripts: {0}", ssh_res);
            // deploying peer options
            Thread.Sleep(20000);
            //logger.Info("Before sync");
            ssh_res = Deploy.SendSshCommand("127.0.0.1", 4567, "ubuntu", "ubuntu", "sudo sync;sync");
            //logger.Info("Before poweroff: {0}", ssh_res);
            Thread.Sleep(5000);
            res_b = vm_reconfigure_nic();//stop and start machine
            logger.Info("Waiting for SSH - 2");
            res_b = VMs.waiting_4ssh(_cloneName);
            if (!res_b)
            {
                logger.Info("SSH 2 false");
                Program.ShowError("Can not open ssh, please check VM state manually and report error", "Waiting for SSH");
                Program.form1.Visible = false;
            }
                     
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
                        ssh_res = Deploy.SendSshCommand("127.0.0.1", 4567, "ubuntu", "ubuntu", "sudo subutai -d import master 2>&1 > master_log");
                        logger.Info("Import master: {0}", ssh_res);
                        ssh_res = Deploy.SendSshCommand("127.0.0.1", 4567, "ubuntu", "ubuntu", "ls -l master_log| wc -l");
                        logger.Info("Import master log: {0} ", ssh_res);

                        // installing management template
                        StageReporter("", "Importing management");
                        ssh_res = Deploy.SendSshCommand("127.0.0.1", 4567, "ubuntu", "ubuntu", "sudo subutai -d import management  2>&1 > management_log");
                        logger.Info("Import management: {0}", ssh_res);
                        ssh_res = Deploy.SendSshCommand("127.0.0.1", 4567, "ubuntu", "ubuntu", "bash ls -l management_log");
                        logger.Info("Import management log: {0}", ssh_res);
                        ssh_res = Deploy.SendSshCommand("127.0.0.1", 4567, "ubuntu", "ubuntu", "sudo bash subutai management_network detect");
                        logger.Info("Import management address: {0}", ssh_res);

                        if (Deploy.com_out(ssh_res, 0) != "0")
                        {
                            Program.ShowError("Management template was not installed, installation failed, please uninstall and try to install later", "Management template was not imported");
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
                        ssh_res = Deploy.SendSshCommand("127.0.0.1", 4567, "ubuntu", "ubuntu", "ls -l management_log");
                        logger.Info("Import management log: {0}", ssh_res);
                        if (Deploy.com_out(ssh_res, 0) != "0")
                        {
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

        private bool vm_reconfigure_nic()
        {
            //stop VM
            string res = "";
            StageReporter("Stopping machine","");
            VMs.stop_vm(_cloneName);
            Thread.Sleep(5000);

            StageReporter("Setting network interfaces", "");
            VMs.set_bridged(_cloneName);
            //NAT on nic2
            VMs.set_nat(_cloneName);
            //Hostonly eth2 on nic 3
            StageReporter("", "Setting nic3 hostonly");
            string if_name = VMs.set_hostonly(_cloneName);
            // start VM
            StageReporter("", "Starting VM");
            res = Deploy.LaunchCommandLineApp("vboxmanage", $"startvm --type headless {_cloneName} ");
            logger.Info("vm 1: {0} starting: {1}", _cloneName, Deploy.com_out(res, 0));
            logger.Info("vm 1: {0} stdout: {1}", _cloneName, Deploy.com_out(res, 1));
            
            string err = Deploy.com_out(res, 2);
            logger.Info("vm 1: {0} stdout: {1}", _cloneName, err);

            if (err != null && err.Contains(" error:") && err.Contains(if_name))
            {
                StageReporter("VBox Host-Only adapter problem", "Trying to turn off Host-Only adapter");
                //Program.ShowError("Trying to turn off Host-Only adapter","VBox Host-Only adapter problem");
                Thread.Sleep(10000);
                res = Deploy.LaunchCommandLineApp("vboxmanage", $"modifyvm {_cloneName} --nic3 none");
                logger.Info("nic3 none: {0}", res);
                StageReporter("", "Trying to turn off Host-Only adapter");
                res = Deploy.LaunchCommandLineApp("vboxmanage", $"startvm --type headless {_cloneName} ");
                logger.Info("vm 2: {0} starting: {1}", _cloneName, res);
                err = Deploy.com_out(res, 2);
                if (err != null || err != "")
                {
                    return false;
                    //Program.ShowError("Virtual machine can not start, try to start manually and report error", "Virtual machine can not start");
                    //Program.form1.Visible = false;
                }
            }
            return true;
        }

        private void deploy_p2p()
        {
            // DEPLOYING P2P SERVICE
            StageReporter("Installing P2P service", "");
            Deploy.ShowMarquee();

            string res = "";
            var name = "Subutai Social P2P";
            var binPath = $"{_arguments["appDir"]}bin\\p2p.exe";
            const string binArgument = "daemon";

            // installing service
            StageReporter("", "Installing P2P service");
            res = Deploy.LaunchCommandLineApp("nssm", $"install \"{name}\" \"{binPath}\" \"{binArgument}\"");
            logger.Info("Installing P2P service: {0}", res);

            // starting service
            StageReporter("", "Starting P2P service");
            res = Deploy.LaunchCommandLineApp("nssm", $"start \"{name}\"");
            logger.Info("Starting P2P service: {0}", res);
            Thread.Sleep(2000);

            //configuring service
            StageReporter("", "Configuring P2P service");
            res = Deploy.LaunchCommandLineApp("sc", $"failure \"{name}\" actions= restart/10000/restart/15000/restart/18000 reset= 86400");
            logger.Info("Configuring P2P service {0}", $"failure \"{name}\" actions= restart/10000/restart/15000/restart/18000 reset= 86400: {0}", res);

            finished = 1;
        }

        #endregion

        private void check_files()
        {
            StageReporter("", "Performing file check");
            logger.Info("Performing file check");
            download_file($"{ _arguments["appDir"]}/repo_tgt");
            //var rows = File.ReadAllLines("C:\\Subutai\\repotgt");
            String pth = $"{_arguments["appDir"]}/{_arguments["repo_tgt"]}";
            var rows = File.ReadAllLines(pth);
            logger.Info("Read rows = {0}", rows.ToString());
            foreach (var row in rows)
            {
                var folderFile = row.Split(new[] { "|" }, StringSplitOptions.None);
                var folderpath = folderFile[0].Trim();
                var filename = folderFile[1].Trim();
                String fullFolderPath = $"{_arguments["appDir"]}/{folderpath.ToString()}";
                String fullFileName = $"{_arguments["appDir"]}/{folderpath.ToString()}/{filename.ToString()}";
                StageReporter("", folderpath.ToString() + "/" + filename.ToString());
                logger.Info("Checking file {0}/{1}", fullFolderPath, filename);
 
                if (!Directory.Exists(fullFolderPath))
                {
                    Program.ShowError("We are sorry, but something was wrong with Subutai installation. \nFolder" +  fullFolderPath + "does not exist. \nUninstall Subutai from Control Panel, turn off all antivirus software, firewalls and SmartScreen and try again.", "Folder not exist");
                    logger.Info("Directory {0} not found.", fullFolderPath);
                    Environment.Exit(1);
                }
                if (!File.Exists(fullFileName))
                {
                    Program.ShowError("We are sorry, but something was wrong with Subutai installation. \nFile " + fullFileName + " does not exist. \n\nUninstall Subutai from Control Panel, turn off all antivirus software, firewalls and SmartScreen and try again.", "Folder not exist");
                    logger.Info("file {0}/{1} not found.", fullFolderPath, filename);
                    
                    Environment.Exit(2);
                }
            }
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
                //Deploy.ShowMarquee();//

                download_repo();
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            Invoke((MethodInvoker) Refresh);
        }

        private void clean()
        {
            string res = "";
            logger.Info("Cleaning failed installation");
            Program.ShowError("Cleaning failed installation", "Cleaning");
            res = Deploy.LaunchCommandLineApp($"{_arguments["appDir"]}/bin/uninstall-clean","");
            logger.Info("uninstall-clean: {0}", res);
            res = Deploy.LaunchCommandLineApp("regedit.exe", "/s c:/temp/subutai-clean-registry.reg");
            logger.Info("Cleaning registry: {0}", res);
            
            //try
            //{
            //    Directory.Delete($"{_arguments["appDir"]}", true);
            //    logger.Info("Deleting dir");
            //}
            //catch (Exception ex)
            //{
            //    logger.Error(ex.Message, "Deleting directory");
            //}
        }

        private void show_finished()
        {
            string st = " finished";
            logger.Info("show finished = {0}", finished);
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
            }
            logger.Info("show finished = {0}", finished);
            InstallationFinished form2 = new InstallationFinished(st);
            form2.Show();
        }

        private void Form1_VisibleChanged(object sender, EventArgs e)
        {
            logger.Info("Visible changed, finished = {0}", finished);
            
            if (((Form1)sender).Visible == false)
            {
                logger.Info("Visible false");
                show_finished();
            }
        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing ) 
            {
                logger.Info("Closed by user");
                switch (finished)
                {
                    case 0:
                        {
                            finished = 2;
                            logger.Info("FormClosing: Installation cancelled");
                            Program.ShowError("Installation cancelled, please uninstall partially installed Subutai Social", "Installation cancelled");
                            //clean();
                            //show_finished();
                        }
                        break;
                    case 1:
                        logger.Info("FormClosing: Installation finished");
                        break;
                    case 2:
                        logger.Info("FormClosing: Installation cancelled");
                        break;
                }
            }
        }
    }
}
