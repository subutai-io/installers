using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Deployment
{
    public partial class f_confirm : Form
    {
        public static Boolean res = true;
        public static int hostCores; //number of logical processors
        public static Boolean host64;
        public static string hostOSversion;
        public static string hostOSversion_user;
        private static long hostRam;
        public static string hostVT;
        public static string shortVersion;
        public static string vboxVersion;
        public static string vb_version2fit = "5.1.0";

        public f_confirm()
        {
            InitializeComponent();
            showing();
        }

        private void showing()
        {
            tbxAppDir.Text = Inst.subutai_path();
            hostOSversion = Environment.OSVersion.Version.ToString();
            hostOSversion_user = SysCheck.OS_name();
            shortVersion = hostOSversion.Substring(0, 3);
            hostCores = Environment.ProcessorCount; //number of logical processors
            host64 = Environment.Is64BitOperatingSystem;
            vboxVersion = SysCheck.vbox_version();

            hostRam = (long)new Microsoft.VisualBasic.Devices.ComputerInfo().TotalPhysicalMemory / 1024 / 1024;
            hostVT = SysCheck.check_vt();

            l_Proc.Text = SysCheck.hostCores.ToString();
            l_RAM.Text = hostRam.ToString();
            l_S64.Text = host64.ToString();
            l_OS.Text = hostOSversion_user;//shortVersion;//hostOSversion.ToString();
            l_VT.Text = hostVT;
            l_VB.Text = vboxVersion;

                   
            tb_Info.Text = "Subutai can be installed on Windows versions 7(Eng), 8, 8.1, 10.";// "* This value may need to be checked in BIOS. If installation fails, check if  hardware support for virtualization(VT-x/AMD-V) is allowed in BIOS.";
            tb_Info.Text += Environment.NewLine;
            tb_Info.Text += Environment.NewLine;
            checking();
        }

        private void checking()
        {
            if (hostCores < 2)
            {
                l_Proc.ForeColor = Color.Red;
                tb_Info.Text += Environment.NewLine;
                tb_Info.Text += Environment.NewLine;
                tb_Info.Text += "Subutai needs at least 2 cores!";

                res = false;
            }
            else
            {
                l_Proc.ForeColor = Color.Green;
            }

            if ((long)hostRam < 4000) //2000
            {
                l_RAM.ForeColor = Color.Red;
                tb_Info.Text += Environment.NewLine;
                tb_Info.Text += Environment.NewLine;
                tb_Info.Text += "Subutai needs more than 4000 MB of RAM.";
                res = false;
            }
            else
            {
                l_RAM.ForeColor = Color.Green;
            }
            if (!host64)
            {
                l_S64.ForeColor = Color.Red;
                tb_Info.Text += Environment.NewLine;
                tb_Info.Text += Environment.NewLine;
                tb_Info.Text += "Subutai needs x64 processor architecture.";
                res = false;
            }
            else
            {
                l_S64.ForeColor = Color.Green;
            }

            if (!hostVT.ToLower().Contains("true") && shortVersion != "6.1")//not Windows  7
            {
                l_VT.ForeColor = Color.Red;
                tb_Info.Text += Environment.NewLine;
                tb_Info.Text += Environment.NewLine;
                tb_Info.Text += "Please, enable hardware virtualization support (Vt-X/AMD-V) in BIOS!";
                res = false;
            }

            if (!SysCheck.vbox_version_fit(vb_version2fit, l_VB.Text))
            {
                l_VB.ForeColor = Color.Red;
                res = false;
            }
            else
            {
                l_VB.ForeColor = Color.Green;
            }

            //Checking Windows version
            if (!SysCheck.vbox_version_fit("6.1", shortVersion))
            {
                l_OS.ForeColor = Color.Red;
                res = false;
            }
            else
            {
                l_OS.ForeColor = Color.Green;
            }

            if (res)
            {
                btnInstall.Text = $"Install Subutai {Program.inst_type}";
                if (shortVersion != "6.1")
                {
                    
                    lblCheckResult.Text = "Subutai Social can be installed on Your system. Press Install button";
                    lblCheckResult.ForeColor = Color.Green;
                    l_VT.ForeColor = Color.Green;
                    //tb_Info.Text = " ";

                    tb_Info.Text += Environment.NewLine;
                    tb_Info.Text += "Please turn off SmartScreen and Antivirus software for installation time.";
                    tb_Info.Text += Environment.NewLine;
                    tb_Info.Text += Environment.NewLine;
                    tb_Info.Text += "DHCP server must to be running on the local network.";
                }
                else
                {
                    lblCheckResult.Text = "Impossible to check if VT-x is enabled.";
                    lblCheckResult.ForeColor = Color.Blue;
                    l_VT.ForeColor = Color.DarkBlue;
                    tb_Info.Text = "Can not define if VT-x is enabled.";
                    tb_Info.Text += Environment.NewLine;
                    tb_Info.Text += "If not sure, close form, cancel installation and check in BIOS.";
                    tb_Info.Text += Environment.NewLine;
                    tb_Info.Text += Environment.NewLine;
                    tb_Info.Text += "If VT-x enabled, please turn off Antivirus software for installation time!";
                }
                tb_Info.Text += Environment.NewLine;
                tb_Info.Text += Environment.NewLine;
                //tb_Info.Text += "If installation fails or interrupted, please run Start->All Applications->Subutai folder->Uninstall or uninstall from Control Panel.";
                //tb_Info.Text += Environment.NewLine;
                //tb_Info.Text += Environment.NewLine;
                //tb_Info.Text += "Press Next button to proceed.";
            }
            else
            {
                btnInstall.Text = "Exit";
                lblCheckResult.Text = "Sorry, Subutai Social can not be installed. Close form to cancel installation";
                lblCheckResult.ForeColor = Color.Red;
                tb_Info.Text = "Please check Subutai system requirements.";
                tb_Info.Text += Environment.NewLine;
                tb_Info.Text += Environment.NewLine;
                tb_Info.Text += $"Subutai needs Oracle VirtualBox version {vb_version2fit} or higher. Please update or uninstall old version and restart Windows!";
                tb_Info.Text += Environment.NewLine;
                tb_Info.Text += Environment.NewLine;
                tb_Info.Text += "Close form to exit.";
            }
        }

        private void instFolder()
        {
            FolderBrowserDialog folderBrowserDlg = new FolderBrowserDialog();
            folderBrowserDlg.ShowNewFolderButton = true;
            //Show FolderBrowserDialog
            DialogResult dlgResult = folderBrowserDlg.ShowDialog();
            if (dlgResult.Equals(DialogResult.OK))
            {
                //Show selected folder path in textbox1.
                tbxAppDir.Text = folderBrowserDlg.SelectedPath + "\\";
            }
        }

        RadioButton getCheckedRadio(Control container)
        {
            foreach (var control in container.Controls)
            {
                RadioButton radio = control as RadioButton;

                if (radio != null && radio.Checked)
                {
                    return radio;
                }
            }

            return null;
        }

        
        private string peerType(RadioButton btn_checked)
        {
            if (btn_checked.Text.Equals("RH only"))
            {
                return "rh-only";
            }
            if (btn_checked.Text.Equals("Client only"))
            {
                return "client-only";
            }
            return "trial";
            
        }

       
        private void clbTypeInst_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void gbxTypeInst_Enter(object sender, EventArgs e)
        {

        }

        private void btnInstall_Click(object sender, EventArgs e)
        {
            //params=deploy-redist,prepare-vbox,dev,prepare-rh,deploy-p2p 
            //network-installation=true kurjunUrl=https://cdn.subut.ai:8338 
            //repo_descriptor=repomd5-dev repo_tgt=repotgt appDir=[APPDIR] peer=[PEER_OPTION]
            if (btnInstall.Text.Equals("Exit"))
            {
                Program.stRun = false;
                this.Close();
            }
            string appDir = tbxAppDir.Text;//.Replace("/","//");
            string peerOption = peerType(getCheckedRadio(gbxTypeInst));
            
            if (appDir != null && appDir != "")
            {
                Program.inst_args = $"{Program.inst_args} appDir={appDir} peer={peerOption}";
                //MessageBox.Show(Program.inst_args, "", MessageBoxButtons.OK);
                Program.stRun = true;
                this.Close();
            }
        }

        private void linkManual_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            linkManual.LinkVisited = true;
            System.Diagnostics.Process.Start("https://github.com/subutai-io/installers/wiki/Windows-Installer:-Installation-Manual");
        }

        private void linkTutorials_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            linkTutorials.LinkVisited = true;
            System.Diagnostics.Process.Start("https://subutai.io/first-launch.html");
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            instFolder();
        }
    }
}
