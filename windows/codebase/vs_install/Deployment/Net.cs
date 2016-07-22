using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Threading;
using NLog;
using Microsoft.Win32;

namespace Deployment
{
   
    class Net
    { 
        static NLog.Logger logger = LogManager.GetCurrentClassLogger();
    
        //Name of Host-Only interface
        public static string vm_vbox0_ifname()
        {
            int cnt = 0;
            NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface adapter in interfaces)
            {
                logger.Info("adapter: {0}", adapter.Name);
                foreach (UnicastIPAddressInformation unicast_address_info in adapter.GetIPProperties().UnicastAddresses)
                {
                    //logger.Info("uucast address: {0}", unicast_address_info.Address.ToString());
                    if ((unicast_address_info.Address.AddressFamily == AddressFamily.InterNetwork) &&
                        (
                         (adapter.Description.ToString().Contains("Host-Only") && adapter.Description.ToString().Contains("VirtualBox")) ||
                         (adapter.Name.ToString().Contains("Host-Only") && adapter.Name.ToString().Contains("VirtualBox"))
                         )
                        )
                    {
                        cnt++;
                        logger.Info("vbox0 Name = {0}, cnt = {1}", adapter.Description.ToString(), cnt);
                        if (unicast_address_info.Address.ToString() == "192.168.56.1")
                        {
                            return (adapter.Description.ToString());
                        }
                    }
                }
            }
            return "Not defined";
        }

         public static string gateway_if()
        {
            var gateway_address = NetworkInterface.GetAllNetworkInterfaces()
                .Where(e => e.OperationalStatus == OperationalStatus.Up
                )
                .SelectMany(e => e.GetIPProperties().GatewayAddresses)
                .FirstOrDefault();

            var gateway_if_address = gw_from_netstat();
            logger.Info("Gateway 1 address: {0}", gateway_if_address.ToString());
            IPHostEntry host;
            host = Dns.GetHostEntry(Dns.GetHostName());
            NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();

            foreach (NetworkInterface adapter in interfaces)
            {
                if (adapter.OperationalStatus.ToString() != "Up")
                    continue;
                foreach (UnicastIPAddressInformation unicast_address_info in adapter.GetIPProperties().UnicastAddresses)
                {
                    if ((unicast_address_info.Address.AddressFamily == AddressFamily.InterNetwork) &&
                        !(
                            adapter.Description.ToString().Contains("Virtual") ||
                            adapter.Description.ToString().Contains("Pseudo") ||
                            adapter.Description.ToString().Contains("Software") ||
                            adapter.Description.ToString().Contains("VMWare") ||
                            adapter.Description.ToString().Contains("TAP") ||
                            adapter.Name.ToString().Contains("VMWare") ||
                            adapter.Name.ToString().Contains("Software") ||
                            adapter.Name.ToString().Contains("TAP") ||
                            adapter.Name.ToString().Contains("Virtual")
                            )
                        )
                    {
                        IPAddress mask = unicast_address_info.IPv4Mask;
                        //logger.Info("adapter checking: {0}", unicast_address_info.Address.ToString());
                        //if (IsInSameSubnet(unicast_address_info.Address, gateway_address.Address, mask) &&
                        //    adapter.GetIPProperties().GatewayAddresses.FirstOrDefault().Address.ToString() == gateway_address.Address.ToString())
                        if (unicast_address_info.Address.ToString() == gateway_if_address.ToString())
                        {
                            logger.Info("adapter found: {0}", adapter.Description.ToString());
                            return adapter.Description.ToString();
                        }
                    }
                }
            }
            return "No Gateway";
        }

        public static IPAddress GetNetworkAddress(IPAddress address, IPAddress subnetMask)
        {
            byte[] ipAdressBytes = address.GetAddressBytes();
            byte[] subnetMaskBytes = subnetMask.GetAddressBytes();

            if (ipAdressBytes.Length != subnetMaskBytes.Length)
                throw new ArgumentException("Lengths of IP address and subnet mask do not match.");

            byte[] broadcastAddress = new byte[ipAdressBytes.Length];
            for (int i = 0; i < broadcastAddress.Length; i++)
            {
                broadcastAddress[i] = (byte)(ipAdressBytes[i] & (subnetMaskBytes[i]));
            }
            return new IPAddress(broadcastAddress);
        }

        public static bool IsInSameSubnet(IPAddress address2, IPAddress address1, IPAddress subnetMask)
        {
            IPAddress network1 = GetNetworkAddress(address1, subnetMask);
            IPAddress network2 = GetNetworkAddress(address2, subnetMask);

            return network1.Equals(network2);
        }

        public static string gw_from_netstat()
        {
            string res = Deploy.LaunchCommandLineApp("cmd.exe", " /C netstat -r| findstr /i /r \"0.0.0.0.*0.0.0.0");
            logger.Info("netstat = {0}", res);
            res = Deploy.com_out(res, 2);
            res = res.Replace("|", "");
            while (res.Contains("  "))
                res = res.Replace("  ", " ");
            res = res.Replace(" 0.0.0.0 0.0.0.0 ", "");
            //res = res.Remove(0, res.IndexOf(':') + 1);

            logger.Info("removed  = {0}", res);
            string[] splitted = res.Split(' ');
            logger.Info("removed  = {0}", res);
            return splitted[1];
        }

  
        public static void set_fw_rules(string ppath, string rname, bool is_service)
        {
            string res = "";
            if (is_service)
            {
                res = Deploy.LaunchCommandLineApp("netsh", $" advfirewall firewall add rule name=\"{rname}_in\" dir=in action=allow service=\"{ppath}\"  enable=yes");
                logger.Info("Adding {0} service to to firewall exceptions {1}: {2}", rname, ppath, res);

                res = Deploy.LaunchCommandLineApp("netsh", $" advfirewall firewall add rule name=\"{rname}_out\" dir=out action=allow service=\"{ppath}\" enable=yes");
                logger.Info("Adding {0} service to to firewall exceptions {1}: {2}", rname, ppath, res);
            }
            else
            {
                res = Deploy.LaunchCommandLineApp("netsh", $" advfirewall firewall add rule name=\"{rname}_in\" dir=in action=allow program=\"{ppath}\" enable=yes");
                logger.Info("Adding {0}_in rule to to firewall exceptions {1}: {2}", rname, ppath, res);

                res = Deploy.LaunchCommandLineApp("netsh", $" advfirewall firewall add rule name=\"{rname}_out\" dir=out action=allow  program=\"{ppath}\" enable=yes");
                logger.Info("Adding {0}_out rule to to firewall exceptions {1}: {2}", rname, ppath, res);
            }
        }
    }
}
