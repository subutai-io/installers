using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using NLog;

namespace Deployment
{

    /// <summary>
    /// Network related work
    /// </summary>
    class Net
    { 
        static NLog.Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Define Name of Host-Only interface will be used by vboxmanage command.
        /// In Windows terms this is actually network interfacr description.
        /// </summary>
        /// <returns>Name(Description) of Host-Only interface or "Not defined"</returns>
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
                        //if (unicast_address_info.Address.ToString() == "192.168.56.1")
                        //{
                            return (adapter.Description.ToString());
                        //}
                    }
                }
            }
            return "Not defined";
        }

        /// <summary>
        /// Define name (description) of gateway interface.
        /// </summary>
        /// <returns>Gateway interface name or "No Gateway" if gateway not defined</returns>
        public static string gateway_if()
        {
            var gateway_address = NetworkInterface.GetAllNetworkInterfaces()
                .Where(e => e.OperationalStatus == OperationalStatus.Up
                )
                .SelectMany(e => e.GetIPProperties().GatewayAddresses)
                .FirstOrDefault();

            var gateway_if_address = gw_from_netstat();
            if (gateway_if_address.Contains("NA"))
            {
                return "No Gateway";
            }
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
                        if (unicast_address_info.Address.ToString() == gateway_if_address.ToString())
                        {
                            logger.Info("adapter found: Description: {0}, Name: {1}", adapter.Description.ToString(), adapter.Name.ToString());
                            return adapter.Description.ToString();
                        }
                    }
                }
            }
            return "No Gateway";
        }

        /// <summary>
        /// Gets the network address.
        /// </summary>
        /// <param name="address">The IP address.</param>
        /// <param name="subnetMask">The subnet mask.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">Lengths of IP address and subnet mask do not match.</exception>
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

        /// <summary>
        /// Determines whether IP address 1 is in same subnet with the specified IP address2.
        /// </summary>
        /// <param name="address2">The IP address2.</param>
        /// <param name="address1">The IP address1.</param>
        /// <param name="subnetMask">The subnet mask.</param>
        /// <returns>
        ///   <c>true</c> if adresses are in same subnet; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsInSameSubnet(IPAddress address2, IPAddress address1, IPAddress subnetMask)
        {
            IPAddress network1 = GetNetworkAddress(address1, subnetMask);
            IPAddress network2 = GetNetworkAddress(address2, subnetMask);

            return network1.Equals(network2);
        }

        /// <summary>
        /// Define gateway interface name using netstat - as another way does not work for wired adapters.
        /// </summary>
        /// <returns>IP address of default gateway</returns>
        public static string gw_from_netstat()
        {
            string res = Deploy.LaunchCommandLineApp("cmd.exe", " /C netstat -r| findstr /i /r \"0.0.0.0.*0.0.0.0");
            logger.Info("netstat = {0}", res);
            res = Deploy.com_out(res, 2);
            if (res == "" || res == null)
            {
                return ("NA");
            }
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


        /// <summary>
        /// Sets firewall rules for application and service.
        /// </summary>
        /// <param name="ppath">The application path.</param>
        /// <param name="rname">The Rule name.</param>
        /// <param name="is_service">if set to <c>true</c> [is service].</param>
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
