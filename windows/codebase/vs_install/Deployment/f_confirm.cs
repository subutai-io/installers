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
    /// <summary>
    /// public partial class f_confirm : Form
    /// Form showing system settings and installation directory (chosen in installer)
    /// Definition of peer type (trial (RH + MH + Client), RH only or Client only
    /// </summary>
   
    public partial class f_confirm : Form
    {
        /// <summary>
        /// If false - can not install, will be set in checking()
        /// </summary>
        public static Boolean res = true; //if false - can not install, will be set in checking()
        /// <summary>
        /// The number of logical processors
        /// </summary>
        public static int hostCores;
        /// <summary>
        /// If processor architecture is 64
        /// </summary>
        public static Boolean host64;
        /// <summary>
        /// The host OS sversion
        /// </summary>
        public static string hostOSversion;
        /// <summary>
        /// The host OS sversion in human-readable format (like Windows 8.1)
        /// </summary>
        public static string hostOSversion_user;
        private static long hostRam;
        /// <summary>
        /// The VT-x is enamled in BIOS
        /// </summary>
        public static string hostVT;
        /// <summary>
        /// The short version (first 2 numbers) of OS like 6.1
        /// </summary>
        public static string shortVersion;
        /// <summary>
        /// The Oracle VirtualBox version
        /// </summary>
        public static string vboxVersion;
        /// <summary>
        /// The minimal Oracle VirtualBox version needed 
        /// </summary>
        public static string vb_version2fit = "5.1.0";

        /// <summary>
        /// Initializes a new instance of the <see cref="f_confirm"/> class.
        /// Form shows if Subutai can be installed and allows to choose installation type
        /// </summary>
        public f_confirm()
        {
            InitializeComponent();
            
            showing();
        }

        /// <summary>
        /// private void showing()
        /// Showing system and installation parameters form 
        /// Installation is not possible if one of system parameters is red
        /// For Windows 7 can not define if VT/x is enabled in BIOS
        /// </summary>
        private void showing()
        {
            tbxAppDir.Text = Inst.subutai_path(); //Show Installation path, defined in installer
            tbxAppDir.Enabled = false; //Change of installation type not allowed
            tbxAppDir.ReadOnly = true;
            //Defining system parameters
            hostOSversion = Environment.OSVersion.Version.ToString(); //OS version (6.1, 6.2, 10)
            hostOSversion_user = SysCheck.OS_name(); //OS name in human readable format
            shortVersion = hostOSversion.Substring(0, 3); 
            hostCores = Environment.ProcessorCount; //number of logical processors
            host64 = Environment.Is64BitOperatingSystem; //Processor architecture - x86 or x64
            vboxVersion = SysCheck.vbox_version(); //Oracle VirtualBox version 
            hostRam = (long)new Microsoft.VisualBasic.Devices.ComputerInfo().TotalPhysicalMemory / 1024 / 1024;//RAM in MB
            hostVT = SysCheck.check_vt(); //Checking if VT-x is enabled

            //Filling textboxes
            l_Proc.Text = hostCores.ToString();//SysCheck.hostCores.ToString();
            l_RAM.Text = hostRam.ToString();
            l_S64.Text = host64.ToString();
            l_OS.Text = hostOSversion_user;
            l_VT.Text = hostVT;
            l_VB.Text = vboxVersion;
                   
            tb_Info.Text = "Subutai can be installed on Windows versions 7(Eng), 8, 8.1, 10.";
            // "* This value may need to be checked in BIOS. If installation fails, check if 
            //hardware support for virtualization(VT-x/AMD-V) is allowed in BIOS.";
            tb_Info.Text += Environment.NewLine;
            tb_Info.Text += Environment.NewLine;
            //Check if can install
            checking();
        }

        /// <summary>
        /// checking()
        /// Check if Subutai can be installed - system parameters meet Subutai requirements
        /// If parameter does not system requirements, it will be shown in red
        /// For Windows 7 can not define if VT/x is enabled in BIOS
        /// </summary>
        private void checking()
        {
            //2 or more processor cores
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

            //RAM > 4000 KB
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
            //Processor architecture x64
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
            //VT-x enabled
            if (!hostVT.ToLower().Contains("true") && shortVersion != "6.1")//not Windows  7
            {
                l_VT.ForeColor = Color.Red;
                tb_Info.Text += Environment.NewLine;
                tb_Info.Text += Environment.NewLine;
                tb_Info.Text += "Please, enable hardware virtualization support (Vt-X/AMD-V) in BIOS!";
                res = false;
            }
            //Oracle Virtual Box version >5.0
            if (!SysCheck.vbox_version_fit(vb_version2fit, l_VB.Text))
            {
                l_VB.ForeColor = Color.Red;
                res = false;
            }
            else
            {
                l_VB.ForeColor = Color.Green;
            }
            //Checking Windows version >= 6.1, 6.1 is Windows 7
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
                    //Can install
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
                    //WE have Windows 7
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
                //Can not install
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

        /// <summary>
        /// private void instFolder()
        /// not used
        /// Define installation folder with folder dialog, this function can be used if we will decline 
        /// using of third-part installer
        /// </summary>
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

        /// <summary>
        /// RadioButton getCheckedRadio(Control container)
        /// Defines which radio is checked in RadioButtonGrop
        /// for peer type
        /// </summary>
        /// <returns>Returns radio checked. If nothing checked, returns null</returns>
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


        /// <summary>
        /// private string peerType(RadioButton btn_checked)
        /// Defines installation parameter PeerType by checked radio button
        /// for peer type
        /// </summary>
        /// <returns>Returns peer type. By default peer type is "trial" - RH + Management + Client will be installed</returns>
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

        /// <summary>
        /// private void btnInstall_Click(object sender, EventArgs e)
        /// If can install, starts installation. If not - exit
        /// Forms parameters string (adds installation director
        /// </summary>
        private void btnInstall_Click(object sender, EventArgs e)
        {
            string appDir = tbxAppDir.Text;//.Replace("/","//");
            string peerOption = peerType(getCheckedRadio(gbxTypeInst));
            Program.inst_Dir = appDir;


            if (appDir != null && appDir != "" && !appDir.Contains("NA"))
            {
                Program.inst_args = $"{Program.inst_args} appDir={appDir} peer={peerOption}";
                Program.stRun = true;
                //Program.form_.Close();
            } else
            {
                MessageBox.Show("Cannot define application folder for Subutai Social, please uinstall from Control Panel", 
                    "Installation Folder error",
                    MessageBoxButtons.OK);
                Environment.Exit(1);
            }

            if (btnInstall.Text.Contains("Exit"))
            {
                Program.stRun = false;
                //Program.form_.Close();
            }

            Program.form_.Close();
        }

        /// <summary>
        /// private void linkManual_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        /// private void linkTutorials_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        /// Opens Installation Manual and Tutorials pages
        /// </summary>
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

        /// <summary>
        /// private void btnBrowse_Click(object sender, EventArgs e)
        /// Runs instFolder() method to open folder dialog and choose installation folder
        /// Not enabled now as we choose installation folder in installer
        /// </summary>
        private void btnBrowse_Click(object sender, EventArgs e)
        {
            //instFolder();
        }
    }
}
