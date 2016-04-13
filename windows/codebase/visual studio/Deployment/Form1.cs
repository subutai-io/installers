using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
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

        //private readonly string _cloneName = $"subutai-{DateTime.Now.ToString("yyyyMMddhhmm")}";
        private readonly string _cloneName = string.Format("subutai-{0}", DateTime.Now.ToString("yyyyMMddhhmm"));

        private readonly PrivateKeyFile[] _privateKeys = new PrivateKeyFile[]{};

        private void ParseArguments()
        {
            foreach (var splitted in _args.Select(argument => argument.Split(new[] {"="}, StringSplitOptions.None)).Where(splitted => splitted.Length == 2))
            {
                _arguments[splitted[0]] = splitted[1];
            }
        }

        public Form1()
        {
            InitializeComponent();

            ParseArguments();

            _deploy = new Deploy(_arguments);

            timer1.Start();
        }

        public void StageReporter(string stageName, string subStageName)
        {
            Program.form1.Invoke((MethodInvoker)delegate
            {
                if (stageName != "")
                {
                    this.labelControl1.Text = stageName;
                }
                if (subStageName != "")
                {
                    this.progressPanel1.Description = subStageName;
                }
            });
        }

        private void progressPanel1_Click(object sender, EventArgs e)
        {

        }

        #region TASKS FACTORY

        private void TaskFactory(object sender, AsyncCompletedEventArgs e)
        {
            Task.Factory.StartNew(() =>
            {

            })

               .ContinueWith((prevTask) =>
               {
                   if (_arguments["network-installation"].ToLower() == "true")
                   {
                       check_md5();
                       //unzip_repo();
                       //MessageBox.Show("Unzip repo");
                   }
               })

               .ContinueWith((prevTask) =>
               {
                   if (_arguments["network-installation"].ToLower() == "true")
                   {
                       unzip_extracted();
                       //MessageBox.Show("Unzip extracted");
                   }
               })

               .ContinueWith((prevTask) =>
               {
                   if (_arguments["params"].Contains("deploy-redist"))
                   {
                       deploy_redist();

                       //MessageBox.Show("Deploy redist");
                   }
               })

               .ContinueWith((prevTask) =>
               {
                   if (_arguments["params"].Contains("prepare-vbox"))
                   {
                       prepare_vbox();
                       //MessageBox.Show("Prepare VBox");
                   }
               })
               
               .ContinueWith((prevTask) =>
               {
                   if (_arguments["params"].Contains("prepare-rh"))
                   {
                       prepare_rh();
                       //MessageBox.Show("Prepare RH");
                   }
               })

               .ContinueWith((prevTask) =>
               {
                   if (_arguments["params"].Contains("deploy-p2p"))
                   {
                       deploy_p2p();
                       //MessageBox.Show("Deploy P2P");
                   }
               })
               
               .ContinueWith((prevTask) =>
               {
                   Program.form1.Invoke((MethodInvoker) delegate
                   {
                       Program.form1.Visible = false;
                   });

                   Program.form2.Invoke((MethodInvoker) delegate
                   {
                       Program.form2.Show();
                   });
               }).ContinueWith((prevTask) =>
               {
                   //Deploy.LaunchCommandLineApp($"{_arguments["appDir"]}/bin/tray/SubutaiTray.exe", "");
                 Deploy.LaunchCommandLineApp(string.Format("{0}/bin/tray/SubutaiTray.exe", _arguments["appDir"]), "");
               });
        }
        #endregion

        #region TASK FACTORY COMPONENTS
        private void download_repo()
        {
            //DOWNLOAD REPO
            StageReporter("Downloading prerequisites", "");

            Deploy.HideMarquee();
            download_description_file();
        }

        private void download_description_file()
        {
            //StageReporter("", "Getting description file");
            _deploy.DownloadFile(
                url: _arguments["kurjunUrl"], 
                //destination: $"{_arguments["appDir"]}/{_arguments["repo_descriptor"]}", 
                destination: string.Format("{0}/{1}", _arguments["appDir"], _arguments["repo_descriptor"]),
                onComplete: download_prerequisites, 
                report: "Getting repo descriptor", 
                async: true, 
                kurjun: true);
        }

        private int _prerequisitesDownloaded = 0;

        private void download_prerequisites(object sender, AsyncCompletedEventArgs e)
        {
            //var rows = File.ReadAllLines($"{_arguments["appDir"]}/{_arguments["repo_descriptor"]}");
            var rows = File.ReadAllLines(string.Format("{0}/{1}", _arguments["appDir"], _arguments["repo_descriptor"]));

            var row = rows[_prerequisitesDownloaded];
            var folderFile = row.Split(new[] {"|"}, StringSplitOptions.None);

            var folder = folderFile[0].Trim();
            var file = folderFile[1].Trim();

            if (_prerequisitesDownloaded != rows.Length - 3) //.snap?
            {
                _deploy.DownloadFile(
                    url: _arguments["kurjunUrl"],
                    destination: string.Format("{0}/{1}/{2}", _arguments["appDir"], folder, file),
                    onComplete: download_prerequisites,
                    report: string.Format("Getting {0}", file),
                    async: true,
                    kurjun: true
                    );
                _prerequisitesDownloaded++;
            }
            else //snap
            {
                var destfile = file;
                if ((_arguments["params"].Contains("dev")) || (_arguments["params"].Contains("master")))
                {
                    if (_arguments["params"].Contains("dev"))
                    {
                        row = rows[_prerequisitesDownloaded + 1];
                    } else //master
                    {
                        row = rows[_prerequisitesDownloaded + 2];
                    }
                    folderFile = row.Split(new[] { "|" }, StringSplitOptions.None);
                    folder = folderFile[0].Trim();
                    file = folderFile[1].Trim();
                }
                //MessageBox.Show("file:" + folder + "\\" + file + "destfile:" + destfile);
                 _deploy.DownloadFile(
                    url: _arguments["kurjunUrl"],
                    destination: string.Format("{0}/{1}/{3}", _arguments["appDir"], folder, destfile),
                    onComplete: TaskFactory,
                    report: string.Format("Getting {0}", file),
                    async: true,
                    kurjun: true);
            }
        }

        private void check_md5()
        {
            //UNZIP REPO
            StageReporter("Verifying MD5", "");

            Deploy.HideMarquee();

            foreach (var info in PrerequisiteFilesInfo)
            {
                var filepath = info.Key;
                var filename = Path.GetFileName(info.Key);
                var kurjunFileInfo = info.Value;
                var calculatedMd5 = Deploy.Calc_md5(filepath, upperCase: false);

                StageReporter("", "Checking " + filename);

                if (calculatedMd5 != kurjunFileInfo.id.Split(new [] {"."}, StringSplitOptions.None)[1])
                {
                    Program.ShowError(
                        string.Format("Verification of MD5 checksums for {0} failed. Interrupting installation.", filename), "MD5 checksums mismatch");
                }
            }
        }
        private void unzip_extracted()
        {
            // UNZIP FILES

            Deploy.HideMarquee();

            _deploy.unzip_files(_arguments["appDir"]);
        }

        private void deploy_redist()
        {
            // DEPLOY REDISTRIBUTABLES
            StageReporter("Installing redistributables", "");

            Deploy.ShowMarquee();

            StageReporter("", "TAP driver");
            Deploy.LaunchCommandLineApp(string.Format("{0}\\redist\\tap-driver.exe", _arguments["appDir"]), "/S");

            StageReporter("", "MS Visual C++");
            Deploy.LaunchCommandLineApp(string.Format("{0}\\redist\\vcredist64.exe", _arguments["appDir"]), "/install /quiet");

            StageReporter("", "Chrome");
            Deploy.LaunchCommandLineApp("msiexec", string.Format("/qn /i \"{0}\\redist\\chrome.msi\"", _arguments["appDir"]));

            StageReporter("", "Virtual Box");
            Deploy.LaunchCommandLineApp(string.Format("{0}\\redist\\virtualbox.exe", _arguments["appDir"]), "--silent");
            Deploy.CreateShortcut(
                string.Format("{0}\\Oracle\\VirtualBox\\VirtualBox.exe", Environment.GetEnvironmentVariable("ProgramFiles")),
                string.Format("{0}\\Desktop\\Oracle VM VirtualBox.lnk", Environment.GetEnvironmentVariable("Public")),
                "", true);
            Deploy.CreateShortcut(
                string.Format("{0}\\Oracle\\VirtualBox\\VirtualBox.exe", Environment.GetEnvironmentVariable("ProgramFiles")),
                string.Format("{0}\\Desktop\\Oracle VM VirtualBox.lnk", Environment.GetEnvironmentVariable("Public")),
                "", true);

            Deploy.CreateShortcut(
                string.Format("{0}\\Oracle\\VirtualBox\\VirtualBox.exe", Environment.GetEnvironmentVariable("ProgramFiles")),
                string.Format("{0}\\Microsoft\\Windows\\Start Menu\\Programs\\Oracle VM VirtualBox\\Oracle VM VirtualBox.lnk", Environment.GetEnvironmentVariable("ProgramData")),
                "", true);
        }

        private void prepare_vbox()
        {
            // PREPARE VBOX
            StageReporter("Preparing Virtual Box", "");

            Deploy.ShowMarquee();

            // prepare NAT network
            StageReporter("", "NAT network");
            Deploy.LaunchCommandLineApp("vboxmanage", "natnetwork add --netname natnet1 --network '10.0.5.0/24' --enable --dhcp on");

            // import OVAs
            StageReporter("", "Importing Snappy");
            Deploy.LaunchCommandLineApp("vboxmanage", 
              string.Format("import {0}\\ova\\snappy.ova", _arguments["appDir"]));
        }

        private void prepare_rh()
        {
            // PREPARE RH
            StageReporter("Preparing resource host", "");

            Deploy.ShowMarquee();

            // clone VM
            StageReporter("", "Cloning VM");
            Deploy.LaunchCommandLineApp("vboxmanage", 
              string.Format("clonevm --register --name {0} snappy", _cloneName));

            // prepare NIC
            StageReporter("", "Preparing NIC");
            Deploy.LaunchCommandLineApp("vboxmanage", 
              string.Format("modifyvm {0} --nic4 none", _cloneName));
            Deploy.LaunchCommandLineApp("vboxmanage",
                string.Format("modifyvm {0} --nic1 nat --cableconnected1 on --natpf1 'ssh-fwd,tcp,,4567,,22' --natpf1 'mgt-fwd,tcp,,9999,,8443'", _cloneName));

            // set RAM
            StageReporter("", "Setting RAM");
            var hostRam = new Microsoft.VisualBasic.Devices.ComputerInfo().TotalPhysicalMemory / 1024 / 1024;
            ulong vmRam = 0;
            if (hostRam < 10500)
            {
                vmRam = hostRam/2;
            }
            else
            {
                vmRam = 8125;
            }
            Deploy.LaunchCommandLineApp("vboxmanage", 
              string.Format("modifyvm {0} --memory {1}", _cloneName, vmRam));

            //number of cores
            StageReporter("", "Setting number of processors");
            int hostCores = Environment.ProcessorCount; //number of logical processors
            //textBox1.Text = "hostCores=" + hostCores.ToString();
            ulong vmCores = 1;
            if (hostCores > 4 && hostCores < 17) //to ensure that not > than halph phys processors will be used
            {
                vmCores = (ulong)hostCores / 2;
            } else if (hostCores >16)
            {
                vmCores = 8;
            }

            //textBox1.Text = "vmCores=" + vmCores.ToString();
            Deploy.LaunchCommandLineApp("vboxmanage", 
              string.Format("modifyvm {0} --cpus {1}", _cloneName, vmCores));

            // time settings
            StageReporter("", "Setting timezone");
            Deploy.LaunchCommandLineApp("vboxmanage", 
              string.Format("modifyvm {} --rtcuseutc on", _cloneName));

            //start VM
            StageReporter("", "Starting VM");
            Deploy.LaunchCommandLineApp("vboxmanage", 
              string.Format("startvm --type headless {0}", _cloneName));


            // DEPLOY PEER
            StageReporter("Setting up peer", "");

            // waiting SSH session
            StageReporter("", "Waiting for SSH");
            Deploy.WaitSsh("127.0.0.1", 4567, "ubuntu", "ubuntu");

            // creating tmpfs folder
            StageReporter("", "Creating tmps folder");
            Deploy.SendSshCommand("127.0.0.1", 4567, "ubuntu", "ubuntu", "mkdir tmpfs; mount -t tmpfs -o size=1G tmpfs/home/ubuntu/tmpfs");

            // copying snap
            StageReporter("", "Copying Subutai SNAP");

            Deploy.SendFileSftp("127.0.0.1", 4567, "ubuntu", "ubuntu", new List<string>() {
                string.Format("{0}/redist/subutai/prepare-server.sh", _arguments["appDir"]),
                string.Format("{0}/redist/subutai/subutai_4.0.0_amd64.snap", _arguments["appDir"])
                }, "/home/ubuntu/tmpfs");
                         
           // adopting prepare-server.sh
            StageReporter("", "Adapting installation scripts");
            Deploy.SendSshCommand("127.0.0.1", 4567, "ubuntu", "ubuntu", "sed -i 's/IPPLACEHOLDER/192.168.56.1/g' /home/ubuntu/tmpfs/prepare-server.sh");
             
            // running prepare-server.sh script
            StageReporter("", "Running installation scripts");
            Deploy.SendSshCommand("127.0.0.1", 4567, "ubuntu", "ubuntu", "sudo bash /home/ubuntu/tmpfs/prepare-server.sh");
 
            // deploying peer options
            StageReporter("", "Setting peer options");
            if (_arguments["peer"] != "rh-only")
            {
                if (_arguments["peer"] == "trial")
                {
                    if (_arguments["network-installation"].ToLower() != "true")
                    {
                        // setting iptables rules
                        StageReporter("", "Restricting SSH only");
                        Deploy.SendSshCommand("127.0.0.1", 4567, "ubuntu", _privateKeys,
                            "sudo iptables -P INPUT DROP; sudo iptables -P OUTPUT DROP; sudo iptables -A INPUT -p tcp -m tcp --dport 22 -j ACCEPT; sudo iptables -A OUTPUT -p tcp --sport 22 -m state --state ESTABLISHED, RELATED -j ACCEPT");
                    }


                    if (_arguments["network-installation"].ToLower() == "true")
                    {
                        // installing master template
                        StageReporter("", "Importing master");
                        Deploy.SendSshCommand("127.0.0.1", 4567, "ubuntu", "ubuntu", "sudo subutai -d import master");

                        // installing management template
                        StageReporter("", "Importing management");
                        Deploy.SendSshCommand("127.0.0.1", 4567, "ubuntu", "ubuntu", "sudo subutai -d import management");
                    }
                    else
                    {
                        // installing master template
                        StageReporter("", "Importing master");
                        Deploy.SendSshCommand("127.0.0.1", 4567, "ubuntu", _privateKeys, "sudo echo -e 'y' | sudo subutai -d import master");
                        
                        // installing management template
                        StageReporter("", "Importing management");
                        Deploy.SendSshCommand("127.0.0.1", 4567, "ubuntu", _privateKeys, "sudo echo -e 'y' | sudo subutai -d import management");
                    }

                    if (_arguments["network-installation"].ToLower() != "true")
                    {
                        // setting iptables rules
                        StageReporter("", "Allowing TCP trafic");
                        Deploy.SendSshCommand("127.0.0.1", 4567, "ubuntu", "ubuntu", "sudo iptables - P INPUT ACCEPT; sudo iptables -P OUTPUT ACCEPT");
                    }
                }
            }
        }

        private void deploy_p2p()
        {
            // DEPLOYING P2P SERVICE
            StageReporter("Installing P2P service", "");
            Deploy.ShowMarquee();

            var name = "Subutai Social P2P";
            var binPath = string.Format("{0}\\bin\\p2p.exe", _arguments["appDir"]);
            const string binArgument = "daemon";

            // installing service
            StageReporter("", "Installing P2P service");
            Deploy.LaunchCommandLineApp("nssm", 
              string.Format("install \"{0}\" \"{1}\" \"{2}\"", name, binPath, binArgument));

            // starting service
            StageReporter("", "Starting P2P service");
            Deploy.LaunchCommandLineApp("nssm", string.Format("start \"{0}\"", name));
        }

        #endregion

        private void Form1_VisibleChanged(object sender, EventArgs e)
        {
            if (((Form1)sender).Visible == false)
                Program.form2.Show();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
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
    }
}
