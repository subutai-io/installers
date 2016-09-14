using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using NLog;

namespace Deployment
{
    class VMs
    {
        private static NLog.Logger logger = LogManager.GetCurrentClassLogger();
        public static bool start_vm(string name)
        {
            //Form1.StageReporter("", "Starting VM");
            string res = Deploy.LaunchCommandLineApp("vboxmanage", 
                $"startvm --type headless {name} ");

            logger.Info("vm 1: {0} starting: {1}", name, Deploy.com_out(res, 0));
            logger.Info("vm 1: {0} stdout: {1}", name, Deploy.com_out(res, 1));

            string err = Deploy.com_out(res, 2);
            if (err != null && err!="" )
            {
                logger.Info("vm 1: {0} stdout: {1}", name, err);
                return false;
            }
          return true;
        }

        public static bool stop_vm(string name)
        {
            Deploy.StageReporter("Stopping machine", "");
            string res = Deploy.LaunchCommandLineApp("vboxmanage", 
                $"controlvm {name} poweroff soft");
            logger.Info("Stopping machine: {0}", res);
            logger.Info("vm 1: {0} starting: {1}", name, Deploy.com_out(res, 0));
            logger.Info("vm 1: {0} stdout: {1}", name, Deploy.com_out(res, 1));

            string err = Deploy.com_out(res, 2);
            if (err != null && err != "")
            {
                logger.Info("vm 1: {0} stdout: {1}", name, err);
                return false;
            }
            return true;
        }

        //Cloning VM
        public static bool clone_vm(string vmName)
        {
            string res = "";

            // clone VM
            Deploy.StageReporter("", "Cloning VM");
            res = Deploy.LaunchCommandLineApp("vboxmanage", $"clonevm --register --name {vmName} snappy");
            logger.Info("vboxmanage clone vm --register --name {0} snappy: {1} ", vmName, res);
            if (res.ToLower().Contains("error"))
            {
                logger.Error("Can not run command, please check if VirtualBox installed properly", "Importing Snappy");
                Program.ShowError("Can not clone VM, please check if VitrualBox installed properly", "Prepare VBox");
                Program.form1.Visible = false;
            }
            res = Deploy.LaunchCommandLineApp("vboxmanage", $"unregistervm --delete snappy");
            logger.Info("vboxmanage unregistervm --delete snappy: {0}", res);
            if (res.ToLower().Contains("error"))
            {
                logger.Error("Can not run command, please check if VirtualBox installed properly", "Importing Snappy");
                Program.ShowError("Can not unregister VM, please check if VitrualBox installed properly", "Prepare VBox");
                Program.form1.Visible = false;
            }
            return true;//check res
        }

        public static bool vm_set_RAM(string vmName)
        {
            string res = "";

            var hostRam = new Microsoft.VisualBasic.Devices.ComputerInfo().TotalPhysicalMemory / 1024 / 1024;
            ulong vmRam = 2048; //Minimal size
            //Need to be tested for low-memory machines
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
            res = Deploy.LaunchCommandLineApp("vboxmanage", $"modifyvm {vmName} --memory {vmRam}");
            logger.Info("vboxmanage modifyvm {0} --memory {1}: {2}", vmName, vmRam, res);
            if (res.ToLower().Contains("error"))
            {
                logger.Error("Can not run command, please check if VirtualBox installed properly", "Importing Snappy");
                Program.ShowError("Can not modify VM, please check if VitrualBox installed properly", "Prepare VBox");
                Program.form1.Visible = false;
            }
            return true;
        }

        public static bool vm_set_CPUs(string vmName)
        {
            string res = "";

            int hostCores = Environment.ProcessorCount; //number of logical processors
            ulong vmCores = 2;
            if (hostCores > 4 && hostCores < 17) //to ensure that not > than half phys processors will be used
            {
                vmCores = (ulong)hostCores / 2;
            }
            else if (hostCores > 16)
            {
                vmCores = 8;
            }

            res = Deploy.LaunchCommandLineApp("vboxmanage", $"modifyvm {vmName} --cpus {vmCores}");
            logger.Info("vboxmanage modifyvm {0} --cpus {1}: {2}", vmName, vmCores.ToString(), res);
            if (res.ToLower().Contains("error"))
            {
                logger.Error("Can not run command, please check if VirtualBox installed properly", "Importing Snappy");
                Program.ShowError("Can not modify VM, please check if VitrualBox installed properly", "Prepare VBox");
                Program.form1.Visible = false;
            }

            return true;
        }

        public static bool vm_set_timezone(string vmName)
        {
            string res = "";
            res = Deploy.LaunchCommandLineApp("vboxmanage", $"modifyvm {vmName} --rtcuseutc on");
            logger.Info("vboxmanage modifyvm {0} --rtcuseutc: {1}", vmName, res);
            if (res.ToLower().Contains("error"))
            {
                logger.Error("Can not run command, please check if VirtualBox installed properly", "Importing Snappy");
                Program.ShowError("Can not modify VM, please check if VitrualBox installed properly", "Prepare VBox");
                Program.form1.Visible = false;
            }

            Thread.Sleep(4000);
            return true;
        }

        public static bool waiting_4ssh(string name)
        {
            //Form1.StageReporter("", "Waiting for SSH ");
            logger.Info("starting to wait for SSH");
            bool res_b = Deploy.WaitSsh("127.0.0.1", 4567, "ubuntu", "ubuntu");
            if (!res_b)
            {
                logger.Info("SSH false, restarting VM and trying again");
                stop_vm(name);
                Thread.Sleep(10000);
                if (!start_vm(name))
                {
                    Program.ShowError("Can not start VM, please try to start manualy", "Waiting for SSH");
                    Program.form1.Visible = false;
                    return false;
                }
                res_b = Deploy.WaitSsh("127.0.0.1", 4567, "ubuntu", "ubuntu");
                if (!res_b)
                {
                    return false;
                }
            }
            return true;
        }

        public static bool set_bridged(string name)
        {
            Deploy.StageReporter("", "Setting nic1 bridged");
            //get default routing interface
            string netif = Net.gateway_if();
            logger.Info("Gateway interface: {0}", netif);
            if (netif == "No Gateway")
            {
                Program.ShowError("Can not find default gateway interface", "Network settings error");
                Program.form1.Visible = false;
            }
            //Bridge eth0
            string br_cmd = $"modifyvm {name} --nic1 bridged --bridgeadapter1 \"{netif}\"";
            logger.Info("br_cmd: {0}", br_cmd);
            string res = Deploy.LaunchCommandLineApp("vboxmanage", br_cmd);
            logger.Info("Enable bridged nic1: {0}", res);

            string err = Deploy.com_out(res, 2);
            if (err != null && err != "")
            {
                logger.Info("vm 1: {0} stdout: {1}", name, err);
                return false;
            }
            return true;
         }

        public static bool set_nat(string name)
        {
            //NAT (eth1) 
            //NAT on nic2
            Deploy.StageReporter("", "Setting nic2 NAT");
            string res = Deploy.LaunchCommandLineApp("vboxmanage",
               $"modifyvm {name} --nic2 nat --cableconnected2 on --natpf2 \"ssh-fwd,tcp,,4567,,22\" --natpf2 \"https-fwd,tcp,,9999,,8443\"");//
            logger.Info("Enable NAT nic2: {0}", res);

            string err = Deploy.com_out(res, 2);
            if (err != null && err != "")
            {
                logger.Info("vm 1: {0} stdout: {1}", name, err);
                return false;
            }
            return true;
        }

        //Setting Host-Only
        public static string set_hostonly(string name)
        {
            string netif_vbox0 = Net.vm_vbox0_ifname();
            logger.Info("Hostonly interface name: {0}", netif_vbox0);
            string res = "";
            if (netif_vbox0 == "Not defined") // need to create new 
            {
                res = Deploy.LaunchCommandLineApp("vboxmanage", $" hostonlyif create ");
                logger.Info("Host-Only interface creation:  {0}", res);
                if (res.Contains("successfully created"))
                {
                    int start = res.IndexOf("'") + 1;
                    int end = res.IndexOf("'", start);
                    netif_vbox0 = res.Substring(start, end - start);
                    logger.Info("New Host-Only interface name: /{0}/", netif_vbox0);
                    res = Deploy.LaunchCommandLineApp("vboxmanage", $" hostonlyif ipconfig \"{netif_vbox0}\" --ip 192.168.56.1  --netmask 255.255.255.0");
                    logger.Info("hostonly ip config: {0}", res);
                    res = Deploy.LaunchCommandLineApp("vboxmanage", $" dhcpserver add --ifname \"{netif_vbox0}\" --ip 192.168.56.1 --netmask 255.255.255.0 --lowerip 192.168.56.100 --upperip 192.168.56.200");
                    logger.Info("dhcp server add: {0}", res);
                    res = Deploy.LaunchCommandLineApp("vboxmanage", $" dhcpserver modify --ifname \"{netif_vbox0}\" --enable ");
                    logger.Info("dhcp server modify: {0}", res);
                }
                else
                {
                    netif_vbox0 = "Not defined"; // interface not created
                }
            }
            logger.Info("Final Host-Only interface name: {0}", netif_vbox0);
            if (netif_vbox0 != "Not defined") // created, start
            {
                //enable hostonly 
                res = Deploy.LaunchCommandLineApp("vboxmanage", 
                    $"modifyvm {name} --nic3 hostonly --hostonlyadapter3 \"{netif_vbox0}\"");
                logger.Info("Enable hostonly: {0}", res);
                return netif_vbox0;
            }
            res = Deploy.LaunchCommandLineApp("vboxmanage", $"modifyvm {name} --nic3 none");
            logger.Info("No hostonly: {0}", res);
            return netif_vbox0;
        }

        public static bool vm_reconfigure_nic(string vmName)
        {
            //stop VM
            string res = "";
            Deploy.StageReporter("", "Stopping VM");
            stop_vm(vmName);
            Thread.Sleep(5000);
            Deploy.StageReporter("Setting network interfaces", "");
            set_bridged(vmName);
            //NAT on nic2
            set_nat(vmName);
            //Hostonly eth2 on nic 3
            Deploy.StageReporter("", "Setting nic3 hostonly");
            string if_name = set_hostonly(vmName);
            // start VM
            Deploy.StageReporter("", "Starting VM");
            res = Deploy.LaunchCommandLineApp("vboxmanage", $"startvm --type headless {vmName} ");
            logger.Info("vm 1: {0} starting: {1}", vmName, Deploy.com_out(res, 0));
            logger.Info("vm 1: {0} stdout: {1}", vmName, Deploy.com_out(res, 1));

            string err = Deploy.com_out(res, 2);
            logger.Info("vm 1: {0} stdout: {1}", vmName, err);

            if (err != null && err.Contains(" error:") && err.Contains(if_name))
            {
                Deploy.StageReporter("VBox Host-Only adapter problem", "Trying to turn off Host-Only adapter");
                Thread.Sleep(10000);
                res = Deploy.LaunchCommandLineApp("vboxmanage", $"modifyvm {vmName} --nic3 none");
                logger.Info("nic3 none: {0}", res);
                Deploy.StageReporter("", "Trying to turn off Host-Only adapter");
                res = Deploy.LaunchCommandLineApp("vboxmanage", $"startvm --type headless {vmName} ");
                logger.Info("vm 2: {0} starting: {1}", vmName, res);
                err = Deploy.com_out(res, 2);
                if (err != null || err != "")
                {
                    return false;
                }
            }
            return true;
        }

        //Run installation scripts
        public static void run_scripts(string appDir, string vmName)
        {
            string ssh_res = "";
            // creating tmpfs folder
            Deploy.StageReporter("", "Creating tmps folder");
            ssh_res = Deploy.SendSshCommand("127.0.0.1", 4567, "ubuntu", "ubuntu", "mkdir tmpfs; mount -t tmpfs -o size=1G tmpfs/home/ubuntu/tmpfs");
            logger.Info("Creating tmpfs folder: {0}", ssh_res);
            // copying snap
            Deploy.StageReporter("", "Copying Subutai SNAP");

            Deploy.SendFileSftp("127.0.0.1", 4567, "ubuntu", "ubuntu", new List<string>() {
                $"{appDir}/redist/subutai/prepare-server.sh",
                $"{appDir}/redist/subutai/{TC.snapFile}"
                }, "/home/ubuntu/tmpfs");
            logger.Info("Copying Subutai SNAP: {0}, prepare-server.sh", TC.snapFile);

            // adopting prepare-server.sh
            Deploy.StageReporter("", "Adapting installation scripts");
            ssh_res = Deploy.SendSshCommand("127.0.0.1", 4567, "ubuntu", "ubuntu", "sed -i 's/IPPLACEHOLDER/192.168.56.1/g' /home/ubuntu/tmpfs/prepare-server.sh");
            logger.Info("Adapting installation scripts: {0}", ssh_res);
            // running prepare-server.sh script
            Deploy.StageReporter("", "Running installation scripts");
            ssh_res = Deploy.SendSshCommand("127.0.0.1", 4567, "ubuntu", "ubuntu", "sudo bash /home/ubuntu/tmpfs/prepare-server.sh");
            logger.Info("Running installation scripts: {0}", ssh_res);
            // deploying peer options
            Thread.Sleep(20000);
            ssh_res = Deploy.SendSshCommand("127.0.0.1", 4567, "ubuntu", "ubuntu", "sudo sync;sync");
            Thread.Sleep(5000);
            bool res_b = VMs.vm_reconfigure_nic(vmName);//stop and start machine
            logger.Info("Waiting for SSH - 2");
            res_b = VMs.waiting_4ssh(vmName);
            if (!res_b)
            {
                logger.Info("SSH 2 false", "Can not open ssh, please check VM state manually and report error");
                Program.form1.Visible = false;
            }
        }

     }
}
