using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Management;
using System.Management.Instrumentation;

namespace vs_preinstall
{
   
    public partial class Preinstall_check : Form
    {
        public int hostCores; //number of logical processors
        public Boolean host64;
        public String hostOSversion;
        private long hostRam;
        private String hostVT;
        private String shortVersion;

        public Preinstall_check()
        {
            InitializeComponent();
            showing();
        }

        private void showing()
        {
            hostOSversion = Environment.OSVersion.Version.ToString();
            shortVersion = hostOSversion.Substring(0, 3);
            hostCores = Environment.ProcessorCount; //number of logical processors
            host64 = Environment.Is64BitOperatingSystem;

            hostRam = (long)new Microsoft.VisualBasic.Devices.ComputerInfo().TotalPhysicalMemory / 1024 / 1024;
            hostVT = check_vt();

            l_Proc.Text = hostCores.ToString();
            l_RAM.Text = hostRam.ToString();
            l_S64.Text = host64.ToString();
            l_OS.Text = shortVersion;//hostOSversion.ToString();
            l_VT.Text = hostVT;

            tb_Info.Text = "* This value may need to be checked in BIOS. If installation fails, check if  hardware support for virtualization(VT-x/AMD-V) is allowed in BIOS. \n\nPress Check button to check if Subutai can be installed";
            checking();
        }
        private void checking()
        {
            Boolean res = true;
            if (hostCores < 2)
            {
                l_Proc.ForeColor = Color.Red;
                res = false;
             } else
            {
                l_Proc.ForeColor = Color.Green;
            }

            if ((long)hostRam < 2000) //4000
            {
                l_RAM.ForeColor = Color.Red;
                res =  false;
             } else
            {
                l_RAM.ForeColor = Color.Green;
            }
            if (!host64)
            {
                l_S64.ForeColor = Color.Red;
                res = false;
             } else
            {
                l_S64.ForeColor = Color.Green;
            }
            
            if ( !hostVT.ToLower().Contains("true") && shortVersion != "6.1")
            {
                l_VT.ForeColor = Color.Red;
                res = false;
            }

            if (res)
            {
                if (shortVersion != "6.1")
                {
                    label5.Text = "Subutai Social can be installed on Your system. Press Next button";
                    label5.ForeColor = Color.Green;
                    l_VT.ForeColor = Color.Green;
                    tb_Info.Text = "Please turn off SmartScreen, Antivirus/Firewall software for installation time!";
                    tb_Info.Text += Environment.NewLine;
                    tb_Info.Text += Environment.NewLine;
                    tb_Info.Text = "DHCP server need to be running on the local network.";
                }
                else {
                    label5.Text = "Impossible to check if VT-x is enabled.";
                    label5.ForeColor = Color.Blue;
                    l_VT.ForeColor = Color.DarkBlue;
                    tb_Info.Text = "Can not define if VT-x is enabled.";
                    tb_Info.Text += Environment.NewLine;
                    tb_Info.Text += "If not sure, press Next button, cancel installation and check in BIOS.";
                    tb_Info.Text += Environment.NewLine;
                    tb_Info.Text += Environment.NewLine;
                    tb_Info.Text += "If VT-x enabled, please turn off SmartScreen, Antivirus/Firewall software for installation time!";
                }
                tb_Info.Text += Environment.NewLine;
                tb_Info.Text += Environment.NewLine;
                tb_Info.Text += "If installation fails or interrupted, please properly uninstall Subutai from Control Panel.";
                tb_Info.Text += Environment.NewLine;
                tb_Info.Text += Environment.NewLine;
                tb_Info.Text += "Press Next button to proceed.";
            } else
            {
                label5.Text = "Sorry, Subutai Social can not be installed. Press Next button";
                label5.ForeColor = Color.Red;
                tb_Info.Text = "Please check Subutai system requirements.";
                tb_Info.Text += Environment.NewLine;
                tb_Info.Text += Environment.NewLine;
                tb_Info.Text += "Press Next button to exit.";
             }
            
        }
        private String check_vt()
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
                    //Console.WriteLine("Property Name: {0} Value: {1}", prop.Name, prop.Value);
                }
            }
          return "Not found";
        }

        private void button1_Click(object sender, EventArgs e) //Next
        {
            if (label5.Text.Contains("Sorry"))
            {
                //MessageBox.Show("Sorry", "No", MessageBoxButtons.OK);
                Environment.Exit(1);
            } else
            {
                //MessageBox.Show("Yes", "Yes", MessageBoxButtons.OK);
                //Environment.Exit(0);
                this.Close();
            }
              
        }

        private void Preinstall_check_Load(object sender, EventArgs e)
        {

        }

     }
}
