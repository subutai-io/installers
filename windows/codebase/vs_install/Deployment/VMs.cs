using System;
using System.Windows.Forms;
using System.Threading;
using NLog;

namespace Deployment
{
    /// <summary>
    /// Working with Virtual Machines
    /// </summary>
    class VMs
    {
        private static NLog.Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Starts Virtual Machine.
        /// </summary>
        /// <param name="name">The name of virtual machine</param>
        /// <returns>true if started, false if not</returns>
        public static bool start_vm(string name)
        {
            Deploy.StageReporter("", "Starting virtual machine");
            string res = Deploy.LaunchCommandLineApp("vboxmanage", 
                $"startvm --type headless {name} ",
                240000);

            logger.Info("vm 1: {0} starting: {1}", name, Deploy.com_out(res, 0));
            logger.Info("vm 1: {0} stdout: {1}", name, Deploy.com_out(res, 1));

            string err = Deploy.com_out(res, 2);
            //if (err != null && err!="" )
            if (!res.Contains("successfully"))
            {
                logger.Info("vm 1: {0} stdout: {1}", name, err);
                return false;
            }
            Thread.Sleep(30000);
            return true;
        }

        /// <summary>
        /// Stops the VM.
        /// </summary>
        /// <param name="name">The name of virtual machine</param>
        /// <returns>true if stopped, false if not</returns>
        public static bool stop_vm(string name)
        {
            Deploy.StageReporter("", "Stopping virtual machine");
            string res = Deploy.LaunchCommandLineApp("vboxmanage", 
                $"controlvm {name} poweroff soft",
                60000);
            logger.Info("Stopping machine: {0}", res);
            logger.Info("vm 1: {0} starting: {1}", name, Deploy.com_out(res, 0));
            logger.Info("vm 1: {0} stdout: {1}", name, Deploy.com_out(res, 1));

            string err = Deploy.com_out(res, 2);
            //if (err != null && err != "")
            if (!res.Contains("100%"))
            {
                logger.Info("vm 1: {0} stdout: {1}", name, err);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Starts VN and tries to establish the ssh connection. If no success - restarts VM and waits again.
        /// Successful connection means we can proceed with set up. 
        /// </summary>
        /// <param name="name">The name of VM.</param>
        /// <returns>true if success, false if not</returns>
        public static bool waiting_4ssh(string name)
        {
            //Form1.StageReporter("", "Waiting for SSH ");
            logger.Info("starting to wait for SSH");
            bool res_b = Deploy.WaitSsh("127.0.0.1", 4567, "ubuntu", "ubuntu");
            if (!res_b)
            {
                logger.Info("SSH false, restarting VM and trying again");
                stop_vm(name);
                Thread.Sleep(15000);
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

        /// <summary>
        /// Restarts the VM and checks ssh connection.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns></returns>
        public static bool restart_vm(string name)
        {
            bool b_res = stop_vm(name);
            //even if did not stop - proceed (VM could be powered off for example
            Thread.Sleep(20000);
            b_res = start_vm(name);
            if (!b_res)
            {
                return false;
            }
            Thread.Sleep(20000);
            b_res = waiting_4ssh(name);
            if (!b_res)
            {
                return false;
            }
            return true;
        }


        
        
        //Cloning VM
        /// <summary>
        /// Clones the VM.
        /// </summary>
        /// <param name="vmName">Name of the VM.</param>
        /// <returns>true if cloned, false if not</returns>
        public static bool clone_vm(string vmName)
        {
            string res = "";

            // clone VM
            Deploy.StageReporter("", "Cloning VM");
            res = Deploy.LaunchCommandLineApp("vboxmanage", $"clonevm --register --name {vmName} snappy", 360000);
            logger.Info("vboxmanage clone vm --register --name {0} snappy: {1} ", vmName, res);
            if (res.ToLower().Contains("error"))
            {
                Program.ShowError("Can not clone VM, please check if VitrualBox installed properly", "Prepare VBox");
                Program.form1.Visible = false;
            }
            res = Deploy.LaunchCommandLineApp("vboxmanage", $"unregistervm --delete snappy", 420000);
            logger.Info("vboxmanage unregistervm --delete snappy: {0}", res);
            if (res.ToLower().Contains("error"))
            {
                MessageBox.Show("VM snappy was not removed, please delete VM snappy and it's files manually after installation and check logs in <SystemDrive>:\\Users\\<UserName>\\.Virtualbox folder","Deleting snappy",MessageBoxButtons.OK);
            }
            return true;//check res
        }

        /// <summary>
        /// Sets VM's RAM. Minimum is 2GB.
        /// If host's RAM is less than 16GB but more than 8GB VM's RAM will be (Host RAM)/2
        /// </summary>
        /// <param name="vmName">Name of the VM.</param>
        /// <returns>true if RAM sat, false if not</returns>
        public static bool vm_set_RAM(string vmName)
        {
            string res = "";

            var hostRam = new Microsoft.VisualBasic.Devices.ComputerInfo().TotalPhysicalMemory / 1024 / 1024;
            ulong vmRam = Program.vmRAM;
            if (vmRam == 0)
                vmRam = vm_RAM();

            res = Deploy.LaunchCommandLineApp("vboxmanage", $"modifyvm {vmName} --memory {vmRam}", 120000);
            logger.Info("vboxmanage modifyvm {0} --memory {1}: {2}", vmName, vmRam, res);
            if (res.ToLower().Contains("error"))
            {
                logger.Error("Can not modify RAM, please check if VirtualBox installed properly", "Setting memory");
                Program.ShowError("Can not modify RAM, please check if VitrualBox installed properly", "Setting memory");
                Program.form1.Visible = false;
            }
            return true;
        }

        /// <summary>
        /// Calculating RAM for VM.
        /// </summary>
        /// <returns>
        /// minimal is 2048, 
        /// if host RAM > 8100 returns hostRAM/2
        /// if host RAM > 16500 returns 8124
        /// </returns>
        public static ulong vm_RAM()
        {
            var hostRam = new Microsoft.VisualBasic.Devices.ComputerInfo().TotalPhysicalMemory / 1024 / 1024;
            ulong vmRam = 2048; //Minimal size
            //Tested - NO
            //if (hostRam < 2000)
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
            return vmRam;
        }


        /// <summary>
        /// Set up CPU quantity for VM
        /// </summary>
        /// <param name="vmName">Name of the VM.</param>
        /// <returns>true if successfully set</returns>
        public static bool vm_set_CPUs(string vmName)
        {
            string res = "";

            int hostCores = Environment.ProcessorCount; //number of logical processors
            ulong vmCores = Program.vmCPUs;
            if (vmCores == 0)
                vmCores = vm_CPUs();
            
            res = Deploy.LaunchCommandLineApp("vboxmanage", $"modifyvm {vmName} --cpus {vmCores}", 120000);
            logger.Info("vboxmanage modifyvm {0} --cpus {1}: {2}", vmName, vmCores.ToString(), res);
            if (res.ToLower().Contains("error"))
            {
                logger.Error("Can not run command, please check if VirtualBox installed properly", "Setting CPUs");
                Program.ShowError("Can not modify VM, please check if VitrualBox installed properly", "Prepare VBox");
                Program.form1.Visible = false;
            }
            return true;
        }

        /// <summary>
        /// Calculates number of CPUs for Vms.
        /// </summary>
        /// <returns>
        /// minimal 2
        /// if hostcores>4 hostcores/2
        /// if hostcores>16 8
        /// </returns>
        public static ulong vm_CPUs()
        {
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
            return vmCores;
        }

        /// <summary>
        /// Set timezone for VM.
        /// </summary>
        /// <param name="vmName">Name of the VM.</param>
        /// <returns>true if success, false if not</returns>
        public static bool vm_set_timezone(string vmName)
        {
            string res = "";
            res = Deploy.LaunchCommandLineApp("vboxmanage", $"modifyvm {vmName} --rtcuseutc on", 120000);
            logger.Info("vboxmanage modifyvm {0} --rtcuseutc: {1}", vmName, res);
            if (res.ToLower().Contains("error"))
            {
                logger.Error("Can not run command, please check if VirtualBox installed properly", "Setting timezone");
                Program.ShowError("Can not modify VM, please check if VitrualBox installed properly", "Prepare VBox");
                Program.form1.Visible = false;
            }

            Thread.Sleep(4000);
            return true;
        }

        /// <summary>
        /// Sets the bridged interface on VM. It will be dafault gateway interface
        /// </summary>
        /// <param name="name">The name of VM.</param>
        /// <returns>true if success, false if not</returns>
        public static bool set_bridged(string name)
        {
            Deploy.StageReporter("", "Setting nic1 bridged");
            //get default routing interface
            string netif = Net.gateway_if();
            logger.Info("Gateway interface: {0}", netif);
            if (netif == "No Gateway")
            {
                Program.ShowError("Can not find default gateway interface, please check Internet connection", "Network settings error");
                Program.form1.Visible = false;
            }
            //Bridge eth0
            string br_cmd = $"modifyvm {name} --nic1 bridged --bridgeadapter1 \"{netif}\"";
            logger.Info("br_cmd: {0}", br_cmd);
            string res = Deploy.LaunchCommandLineApp("vboxmanage", br_cmd, 120000);
            logger.Info("Enable bridged nic1: {0}", res);

            string err = Deploy.com_out(res, 2);
            if (err != null && err != "")
            {
                logger.Info("vm 1: {0} stdout: {1}", name, err);
                return false;
            }
            return true;
         }

        /// <summary>
        /// Sets the nat interface for VM.
        /// </summary>
        /// <param name="name">The name of VM.</param>
        /// <returns>true if success, false if not</returns>
        public static bool set_nat(string name)
        {
            //NAT on nic2
            Deploy.StageReporter("", "Setting nic2 NAT");
            string res = Deploy.LaunchCommandLineApp("vboxmanage",
               $"modifyvm {name} --nic2 nat --cableconnected2 on --natpf2 \"ssh-fwd,tcp,,4567,,22\" --natpf2 \"https-fwd,tcp,,9999,,8443\"",
               60000);//
            logger.Info("Enable NAT nic2: {0}", res);

            string err = Deploy.com_out(res, 2);
            if (err != null && err != "")
            {
                logger.Info("vm 1: {0} stdout: {1}", name, err);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Sets the host-only interface for VM.
        /// </summary>
        /// <param name="name">The name of VM.</param>
        /// <returns>
        /// Name of host-only interface.
        /// </returns>
        public static string set_hostonly(string name)
        {
            string netif_vbox0 = Net.vm_vbox0_ifname();
            logger.Info("Hostonly interface name: {0}", netif_vbox0);
            string res = "";
            if (netif_vbox0 == "Not defined") // need to create new 
            {
                res = Deploy.LaunchCommandLineApp("vboxmanage", $" hostonlyif create ", 120000);
                logger.Info("Host-Only interface creation:  {0}", res);
                if (res.Contains("successfully created"))
                {
                    int start = res.IndexOf("'") + 1;
                    int end = res.IndexOf("'", start);
                    netif_vbox0 = res.Substring(start, end - start);
                    logger.Info("New Host-Only interface name: /{0}/", netif_vbox0);
                    res = Deploy.LaunchCommandLineApp("vboxmanage", $" hostonlyif ipconfig \"{netif_vbox0}\" --ip 192.168.56.1  --netmask 255.255.255.0", 120000);
                    logger.Info("hostonly ip config: {0}", res);
                }
                else
                {
                    netif_vbox0 = "Not defined"; // interface not created
                    res = Deploy.LaunchCommandLineApp("vboxmanage", $"modifyvm {name} --nic3 none", 60000);
                    logger.Info("No hostonly: {0}", res);
                    return netif_vbox0;
                }
            }
            logger.Info("Final Host-Only interface name: {0}", netif_vbox0);
            if (!netif_vbox0.Contains("Not defined")) // created, start
            {
                //Remove dhcp server present on interface
                res = Deploy.LaunchCommandLineApp("vboxmanage", $" dhcpserver remove --ifname \"{netif_vbox0}\"", 180000);
                logger.Info("dhcp server remove: {0}", res);

                //Add dhcp server
                res = Deploy.LaunchCommandLineApp("vboxmanage",
                    $" dhcpserver add --ifname \"{netif_vbox0}\" --ip 192.168.56.1 --netmask 255.255.255.0 --lowerip 192.168.56.100 --upperip 192.168.56.200",
                    180000);
                logger.Info("dhcp server add: {0}", res);

                //Enable dhcp server
                res = Deploy.LaunchCommandLineApp("vboxmanage", $" dhcpserver modify --ifname \"{netif_vbox0}\" --enable ", 180000);
                logger.Info("dhcp server modify: {0}", res);
                //enable hostonly 
                res = Deploy.LaunchCommandLineApp("vboxmanage",
                    $"modifyvm {name} --nic3 hostonly --hostonlyadapter3 \"{netif_vbox0}\"", 180000);
                logger.Info("Enable hostonly: {0}", res);
                return netif_vbox0;
            }
            return netif_vbox0;
        }

        /// <summary>
        /// Sets network interfaces and attempts to start VM.
        /// If cannot start - tries to turm off host-only adapter and restart
        /// </summary>
        /// <param name="vmName">Name of the VM.</param>
        /// <returns>true if success, false if not</returns>
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
            res = Deploy.LaunchCommandLineApp("vboxmanage", $"startvm --type headless {vmName} ", 60000);
            logger.Info("vm 1: {0} starting: {1}", vmName, Deploy.com_out(res, 0));
            logger.Info("vm 1: {0} stdout: {1}", vmName, Deploy.com_out(res, 1));

            string err = Deploy.com_out(res, 2);
            logger.Info("vm 1: {0} stdout: {1}", vmName, err);

            //Cannot start, checking host-only and bridged interfaces
            if (err != null && err.Contains(" error:"))
            {
                //Host-only problem
                if (err.Contains(if_name))
                {
                   
                    Deploy.StageReporter("VBox Host-Only adapter problem", "Trying to turn off Host-Only adapter");
                    Thread.Sleep(10000);
                    res = Deploy.LaunchCommandLineApp("vboxmanage", $"modifyvm {vmName} --nic3 none", 60000);
                    logger.Info("nic3 none: {0}", res);
                    Deploy.StageReporter("", "Trying to turn off Host-Only adapter");
                    res = Deploy.LaunchCommandLineApp("vboxmanage", $"startvm --type headless {vmName} ", 60000);
                    logger.Info("vm 2: {0} starting: {1}", vmName, res);
                    err = Deploy.com_out(res, 2);
                    if (res.Contains("successfully started"))   //(err != null || err != "")
                    {
                        return true;
                    }
                }

                //Bridged problem: Nonexistent host networking interface
                if (err.Contains("Nonexistent host networking interface"))
                {
                    Deploy.StageReporter("VBox Bridged adapter problem", "Trying to turn off Bridged adapter");
                    Thread.Sleep(10000);
                    res = Deploy.LaunchCommandLineApp("vboxmanage", $"modifyvm {vmName} --nic1 none", 60000);
                    logger.Info("nic1 none: {0}", res);
                    Deploy.StageReporter("", "Trying to turn off Bridged adapter");
                    res = Deploy.LaunchCommandLineApp("vboxmanage", $"startvm --type headless {vmName} ", 60000);
                    logger.Info("vm 2: {0} starting: {1}", vmName, res);
                    err = Deploy.com_out(res, 2);
                    if (!res.Contains("successfully started"))
                    {
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
            }
            return true;
        }
     }
}
