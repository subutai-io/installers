using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Management;
using Microsoft.Win32;

namespace Deployment
{
    class SysCheck
    {
        public static  Boolean res = true;
        public static string check_vt()
        {
            ManagementClass managClass = new ManagementClass("win32_processor");
            ManagementObjectCollection managCollec = managClass.GetInstances();
            foreach (ManagementObject managObj in managCollec)
            {
                foreach (var prop in managObj.Properties)
                {
                    if (prop.Name == "VirtualizationFirmwareEnabled")
                    {
                        return prop.Value.ToString();
                    }
                }
            }
            return "Not found";
        }

        public static string vbox_version()
        {
            //HKEY_LOCAL_MACHINE\SOFTWARE\Oracle\VirtualBox
            string subkey = "SOFTWARE\\Oracle\\VirtualBox";
            RegistryKey rk = Registry.LocalMachine.OpenSubKey(subkey);
            if (rk == null)
            {
                return "0";
            }
            var vers = rk.GetValue("Version");
            return vers.ToString();
        }

        public static bool vbox_version_fit(string versFit, string versCheck)
        {
            string[] vb = versFit.Split('.');
            string[] vb_check = versCheck.Split('.');
            if (versCheck == "0")//
            {
                return true;
            }
            if (versCheck.Equals(versFit, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            int bound = Math.Min(vb.Length, vb_check.Length);
            int[] vi = new int[bound];//minimal version
            int[] vi_check = new int[bound];//checked version
            for (int i = 0; i < bound; ++i)
            {
                if (!Int32.TryParse(vb[i], out vi[i]) || !Int32.TryParse(vb_check[i], out vi_check[i]))
                {
                    bound = i;
                    break;
                }
            }

            for (int i = 0; i < bound; ++i)
            {
                if (i < 2 && vi_check[i] < vi[i])
                {
                    return false;
                }
                if (i < 2 && vi_check[i] > vi[i])
                {
                    return true;
                }
                if (i > 2)//previous is equal
                {
                    if (vi_check[i] < vi[i])
                        return false;
                }
            }
            return true;
        }

        public static string OS_name()
        {
            String subKey = @"SOFTWARE\Wow6432Node\Microsoft\Windows NT\CurrentVersion";
            RegistryKey key = Registry.LocalMachine.OpenSubKey(subKey);
            if (key == null)
            {
                return "Unknown";
            }
            var vers = key.GetValue("ProductName");
            return vers.ToString();
        }

     }
}
