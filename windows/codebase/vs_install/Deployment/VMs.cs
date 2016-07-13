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
            if (err != null && err!="")
            {
                logger.Info("vm 1: {0} stdout: {1}", name, err);
                return false;
            }
          return true;
        }

        public static bool stop_vm(string name)
        {
            Form1.StageReporter("Stopping machine", "");
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

        public static bool waiting_4ssh(string name)
        {
            //Form1.StageReporter("", "Waiting for SSH ");
            logger.Info("starting to waiting for SSH");
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
            Form1.StageReporter("", "Setting nic1 bridged");
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
            Form1.StageReporter("", "Setting nic2 NAT");
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

        public static bool import_templ(string tname)
        {
            string ssh_res = Deploy.SendSshCommand("127.0.0.1", 4567, 
                "ubuntu", "ubuntu", $"sudo subutai -d import {tname} 2>&1 > {tname}_log");
            string stcode = Deploy.com_out(ssh_res, 0);
            string sterr = Deploy.com_out(ssh_res, 2);

            logger.Info("Import {0}: {1}, code: {2}, err: {3}", 
                tname, ssh_res, stcode, sterr);

            if (stcode != "0" &&  sterr != "Empty")
            {
                return false;
            }
            return true;
        }
    }
}
