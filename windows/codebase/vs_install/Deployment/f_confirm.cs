using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using NLog;

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
        /// The result showing if client-only can be installed
        /// </summary>
        public static Boolean res_client = true; //if false - client-only can not be installed
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
        /// <summary>
        /// The host RAM in MB
        /// </summary>
        private static ulong hostRam;
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
        /// ContextMenuStrip for linkLabels
        /// </summary>
        private ContextMenuStrip cms = new ContextMenuStrip();

        /// <summary>
        /// The link URL for Manual and Tutorials
        /// </summary>
        private string linkURL = "";

        /// <summary>
        /// Coordinates to show right-click menu
        /// </summary>
        private int X;
        private int Y;

        /// <summary>
        /// The logger, will log system information
        /// </summary>
        private static NLog.Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Initializes a new instance of the <see cref="f_confirm"/> class.
        /// Form shows if Subutai can be installed and allows to choose installation type
        /// </summary>
        public f_confirm()
        {
            InitializeComponent();

            rbTrial.CheckedChanged += new EventHandler(radioButtons_CheckedChanged);
            rbRHonly.CheckedChanged += new EventHandler(radioButtons_CheckedChanged);
            rbTrial.CheckedChanged += new EventHandler(radioButtons_CheckedChanged);
        }

        /// <summary>
        /// Handles the Load event of the f_confirm control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void f_confirm_Load(object sender, EventArgs e)
        {
            cms.Items.Add("Copy URL");
            cms.ItemClicked += new ToolStripItemClickedEventHandler(cms_ItemClicked);

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
            hostOSversion_user = SysCheck.OS_name().Replace("Windows", "Win"); //OS name in human readable format
            shortVersion = hostOSversion.Substring(0, 3);
            hostCores = Environment.ProcessorCount; //number of logical processors
            host64 = Environment.Is64BitOperatingSystem; //Processor architecture - x86 or x64
            vboxVersion = SysCheck.vbox_version(); //Oracle VirtualBox version 
            hostRam = (ulong)new Microsoft.VisualBasic.Devices.ComputerInfo().TotalPhysicalMemory / 1024 / 1024;//RAM in MB
            hostVT = SysCheck.check_vt(); //Checking if VT-x is enabled

            //Filling textboxes
            l_Proc.Text = hostCores.ToString();//SysCheck.hostCores.ToString();
            l_RAM.Text = hostRam.ToString();
            l_S64.Text = host64.ToString();
            l_OS.Text = hostOSversion_user;
            l_VT.Text = hostVT;
            l_VB.Text = vboxVersion;

            tb_Info.Text = "Subutai can be installed on Windows versions 7, 8, 8.1, 10.";
            // "* This value may need to be checked in BIOS. If installation fails, check if 
            //hardware support for virtualization(VT-x/AMD-V) is allowed in BIOS.";
            tb_Info.Text += Environment.NewLine;
            tb_Info.Text += Environment.NewLine;

            tb_Proc_VM.Text = VMs.vm_CPUs().ToString();
            tb_RAM_VM.Text = VMs.vm_RAM().ToString();

            //Log system info:
            logger.Info("OS: {0}, {1}", hostOSversion, hostOSversion_user);
            logger.Info("CPUs: {0}", hostCores);
            logger.Info("Is 64x: {0}", host64);
            logger.Info("RAM: {0}MB", hostRam);
            logger.Info("VT/x enabled: {0}", hostVT);
            logger.Info("Oracle VBox version: {0}", vboxVersion);
            //Check if can install
            checking(peerType(getCheckedRadio(gbxTypeInst)));
        }

        /// <summary>
        /// checking()
        /// Check if Subutai can be installed - system parameters meet Subutai requirements
        /// If parameter does not system requirements, it will be shown in red
        /// For Windows 7 can not define if VT/x is enabled in BIOS
        /// </summary>
        private void checking(string pType)
        {
            string msg = "";
            string msg_res = "";
            //2 or more processor cores
            if (hostCores < 2)
            {
                l_Proc.ForeColor = Color.Red;
                tb_Info.Text += Environment.NewLine;
                tb_Info.Text += Environment.NewLine;
                tb_Info.Text += "Subutai needs at least 2 cores!";
                res = false;
                msg_res += "Number of cores on Your machine should be 2 or more\n";
            }
            else
            {
                l_Proc.ForeColor = Color.Green;
            }

            //RAM > 4000 KB
            //here: change
            if ((long)hostRam < 3800)
            {
                l_RAM.ForeColor = Color.Red;
                tb_Info.Text += Environment.NewLine;
                tb_Info.Text += Environment.NewLine;
                tb_Info.Text += "Subutai needs more than 3000 MB of RAM.";
                res = false;
                msg_res += "RAM should be 3800 MB or more\n";
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
                res_client = false; // for now may be later can change
                msg_res += "Processor architecture should be x64";
            }
            else
            {
                l_S64.ForeColor = Color.Green;
            }

            //VT-x enabled
            if (!hostVT.ToLower().Contains("true"))
            {
                if (shortVersion != "6.1")//not Windows  7 
                {
                    l_VT.ForeColor = Color.Red;
                    tb_Info.Text += Environment.NewLine;
                    tb_Info.Text += Environment.NewLine;
                    tb_Info.Text += "Please, enable hardware virtualization support (Vt-X/AMD-V) in BIOS!";
                    res = false;
                    msg_res += "Processor virtualisation should be enabled\n";
                } else
                {
                    l_VT.ForeColor = Color.DarkBlue;

                }
            } else
            {
                l_VT.ForeColor = Color.Green;
            }

            //Oracle Virtual Box version >5.0
            if (!SysCheck.vbox_version_fit(vb_version2fit, l_VB.Text))
            {
                l_VB.ForeColor = Color.Red;
                res = false;
                msg_res += "Oracle VirtualBox version should be 5.1.0 or higher";
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
                res_client = false;
                msg_res += "Windows version should 7 or hegher";
            }
            else
            {
                l_OS.ForeColor = Color.Green;
            }

            //If meet requirements
            if (res)
            {
                btnInstall.Text = $"Install Subutai {Program.inst_type}";
                if (shortVersion != "6.1")
                {
                    //Can install
                    //Win 8, 10
                    lblCheckResult.Text = "Subutai Social can be installed on Your system. Press Install button";
                    lblCheckResult.ForeColor = Color.Green;

                    tb_Info.Text += Environment.NewLine;
                    tb_Info.Text += "Please turn off SmartScreen and Antivirus software for installation time.";
                    tb_Info.Text += Environment.NewLine;
                    tb_Info.Text += Environment.NewLine;
                    tb_Info.Text += "DHCP server must to be running on the local network.";
                }
                else
                {
                    //Windows 7
                    if (pType != "client-only")
                    {
                        lblCheckResult.Text = "Impossible to check if VT-x is enabled.";
                        lblCheckResult.ForeColor = Color.Blue;
                        l_VT.ForeColor = Color.DarkBlue;
                        tb_Info.Text = $"Can not define if VT-x is enabled. \nPlease find in {tbxAppDir.Text}bin and run (As Administrator) Microsoft Hardware-assisted virtualization (HAV) DETECTION TOOL: havdetectiontool.exe";
                        tb_Info.Text += Environment.NewLine;
                        tb_Info.Text += "If VT-x is not enabled, close form, cancel installation and enable in BIOS.";
                        tb_Info.Text += Environment.NewLine;
                        tb_Info.Text += "If VT-x enabled, please turn off Antivirus software for installation time!";
                        tb_Info.Text += Environment.NewLine;
                        tb_Info.Text += "DHCP server must to be running on the local network.";
                    } else
                    {
                        lblCheckResult.Text = "Impossible to check if VT-x is enabled, but Client-Only version can be installed.";
                        lblCheckResult.ForeColor = Color.Green;
                        tb_Info.Text += Environment.NewLine;
                        tb_Info.Text += "Please turn off SmartScreen and Antivirus software for installation time.";
                        tb_Info.Text += Environment.NewLine;
                        tb_Info.Text += Environment.NewLine;
                        tb_Info.Text += "DHCP server must to be running on the local network.";
                    }
                }
                tb_Info.Text += Environment.NewLine;
                tb_Info.Text += Environment.NewLine;
            }
            else //res = false
            {
                if (!res_client) //can not install at all
                {
                    btnInstall.Text = "Exit";
                    lblCheckResult.Text = "Sorry, Subutai Social can not be installed. ";
                    lblCheckResult.Text += "Close form to cancel installation";
                    lblCheckResult.ForeColor = Color.Red;
                    tb_Info.Text = "Please check Subutai system requirements.";
                    tb_Info.Text += Environment.NewLine;
                    tb_Info.Text += Environment.NewLine;
                    tb_Info.Text += $"Subutai needs Oracle VirtualBox version {vb_version2fit} or higher. Please update or uninstall old version and restart Windows!";
                    tb_Info.Text += Environment.NewLine;
                    tb_Info.Text += Environment.NewLine;
                    tb_Info.Text += "Close form to exit.";
                } else if (pType != "client-only")
                {
                    tb_Proc_VM.Enabled = false;
                    tb_RAM_VM.Enabled = false;

                    btnInstall.Text = "Exit";
                    msg = string.Format("Sorry, Subutai Social can not be installed on Your machine but \nYou still can try out Client Only version!");
                    lblCheckResult.Text = msg;
                    lblCheckResult.ForeColor = Color.DarkBlue;
                    tb_Info.Text = "Please check Subutai system requirements.";
                    tb_Info.Text += Environment.NewLine;
                    tb_Info.Text += Environment.NewLine;
                    tb_Info.Text += $"Subutai needs Oracle VirtualBox version {vb_version2fit} or higher. Please update or uninstall old version and restart Windows!";
                    tb_Info.Text += Environment.NewLine;
                    tb_Info.Text += Environment.NewLine;
                    tb_Info.Text += "You still can install Client-Only version to work with our product.";

                    rbTrial.Enabled = false;
                    rbRHonly.Enabled = false;

                    msg = string.Format("Sorry, Subutai Social cannot be installed on your machine but You still can try out Client Only version. \n\n{0}\n\nDo you want to install Client Only version ?",
                            msg_res);
                    DialogResult drs = MessageBox.Show(msg, "Client Only Installation",
                                            MessageBoxButtons.YesNo,
                                            MessageBoxIcon.Question,
                                            MessageBoxDefaultButton.Button1);

                    if (drs == DialogResult.Yes)
                    {
                        rbClientOnly.Checked = true;
                    }
                    else
                    {
                        btnInstall.Text = "Exit";
                    }
                }
                else //client-only
                {
                    btnInstall.Text = "Install Client Only";
                    msg = string.Format("Subutai Social Client Only can be installed on Your system. \nPress Install button to proceed");
                    lblCheckResult.Text = msg;
                    lblCheckResult.ForeColor = Color.Green;

                    tb_Info.Text += Environment.NewLine;
                    tb_Info.Text += "Please turn off SmartScreen and Antivirus software for installation time.";
                    tb_Info.Text += Environment.NewLine;
                    tb_Info.Text += Environment.NewLine;
                    tb_Info.Text += "DHCP server must to be running on the local network.";
                }
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
            //Program.inst_Dir = $"\"{appDir}\"";

            if (appDir != null && appDir != "" && !appDir.Contains("NA"))
            {
                Program.inst_args = $"{Program.inst_args} appDir={appDir} peer={peerOption}";
                Program.stRun = true;
                //Program.form_.Close();
            } else
            {
                MessageBox.Show("Cannot define application folder for Subutai Social, please uninstall from Control Panel",
                    "Installation Folder error",
                    MessageBoxButtons.OK);
                Environment.Exit(1);
            }

            if (btnInstall.Text.Contains("Exit"))
            {
                Program.stRun = false;
            }

            Program.form_.Close();
        }

        /// <summary>
        /// Handles the ItemClicked event of the control (created for linklabel).
        /// Allows to copy URL to clipboard.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="ToolStripItemClickedEventArgs"/> instance containing the event data.</param>
        void cms_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            Thread thread = new Thread(() => Clipboard.SetText(linkURL));
            thread.SetApartmentState(ApartmentState.STA); //Set the thread to STA
            thread.Start();
            thread.Join();
        }

        /// <summary>
        /// private void linkManual_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        /// private void linkTutorials_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        /// Opens Installation Manual and Tutorials pages
        /// </summary>
        private void linkManual_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            LinkLabel ll = (LinkLabel)sender;
            linkURL = "https://github.com/subutai-io/installers/wiki/Windows-Installer:-Installation-Manual";

            if (e.Button == MouseButtons.Right)
            {
                //Show Copy
                cms.Show(this, new Point(panelRight.Bounds.X + ll.Bounds.X, ll.Bounds.Y));
            }
            else
            {
                linkManual.LinkVisited = true;
                try
                {
                    System.Diagnostics.Process.Start(linkURL);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"{ex.Message}. Sorry, can not open link with default browser. Please, right click on the link to copy URL and open it manually", "Installation manual", MessageBoxButtons.OK);
                }
            }
        }

        /// <summary>
        /// Handles the LinkClicked event of the linkTutorials control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="LinkLabelLinkClickedEventArgs"/> instance containing the event data.</param>
        private void linkTutorials_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            LinkLabel ll = (LinkLabel)sender;
            linkURL = "https://subutai.io/first-launch.html";
            if (e.Button == MouseButtons.Right)
            {
                //Show Copy
                cms.Show(this, new Point(panelRight.Bounds.X + ll.Bounds.X, ll.Bounds.Y));
            }
            else
            {
                linkTutorials.LinkVisited = true;
                try
                {
                    System.Diagnostics.Process.Start(linkURL);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"{ex.Message}. Sorry, can not open link with default browser. Please right click on the link to copy URL and open it manually", "Installation tutorials", MessageBoxButtons.OK);
                }
            }
        }

        /// <summary>
        /// Handles the MouseDown event of the linkTutorials control.
        /// Stores control coordinates 
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="MouseEventArgs"/> instance containing the event data.</param>
        private void linkTutorials_MouseDown(object sender, MouseEventArgs e)
        {
            X = e.X;
            Y = e.Y;
        }

        /// <summary>
        /// Handles the MouseDown event of the linkManual control.
        /// Stores control coordinates
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="MouseEventArgs"/> instance containing the event data.</param>
        private void linkManual_MouseDown(object sender, MouseEventArgs e)
        {
            X = e.X;
            Y = e.Y;
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

        /// <summary>
        /// Handles the CheckedChanged event of the radioButtons control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void radioButtons_CheckedChanged(object sender, EventArgs e)
        {
            RadioButton radioButton = sender as RadioButton;
            string name = radioButton.Name;
            checking(peerType(getCheckedRadio(gbxTypeInst)));
        }

        private void tb_Proc_VM_Validating(object sender, System.ComponentModel.CancelEventArgs e)
        {
            int uProc = Int32.Parse(tb_Proc_VM.Text);
            int uMinProc = 2;
            int uMaxProc = maxProc();
            if (!(uProc >= uMinProc && uProc <= uMaxProc))
            {
                string msg = string.Format("Number of CPU should be more than 1 and less than {0}\nChanges are not recommended", uMaxProc + 1);
                epCPUs.SetError(tb_Proc_VM, msg);
                e.Cancel = true;
                return;
            }
            epCPUs.SetError(tb_Proc_VM, "");
            Program.vmCPUs = (ulong)uProc;
        }

        private void tb_RAM_VM_Validating(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ulong uRAM = (ulong)Int32.Parse(tb_RAM_VM.Text);
            ulong uMinRAM = 2048;
            ulong uMaxRAM = maxRAM();
            if (!(uRAM >= uMinRAM && uRAM <= uMaxRAM))
            {
                string msg = string.Format("RAM should be more than 2047 and less than {0}\nChanges are not recommended", uMaxRAM + 1);
                epRAM.SetError(tb_RAM_VM, msg);
                e.Cancel = true;
                return;
            }
            epRAM.SetError(tb_RAM_VM, "");
            Program.vmRAM = uRAM;
        }

        private void tb_RAM_VM_MouseHover(object sender, EventArgs e)
        {
            TextBox TB = (TextBox)sender;
            int VisibleTime = 4000;  //in milliseconds
            ToolTip tt = new ToolTip();
            string msg = string.Format("RAM should be more that 2047 and less than {0}\nChanges are not recommended", maxRAM() + 1);
            tt.Show(msg, TB, 40, -20, VisibleTime);
        }

        private void tb_Proc_VM_MouseHover(object sender, EventArgs e)
        {
            TextBox TB = (TextBox)sender;
            int VisibleTime = 4000;  //in milliseconds
            ToolTip tt = new ToolTip();
            string msg = string.Format("Number of CPU should be more that 1 and less than {0}\nChanges are not recommended", maxProc() + 1);
            
            tt.Show(msg, TB, 40, -20, VisibleTime);
        }

        private ulong maxRAM()
        {
            if (hostRam <= 4096)
            {
                return 2048;
            }
            else
            {
                return hostRam / 2;
            }
        }

        private int maxProc()
        {
            if (hostCores <= 3)
            {
                return 2;
            }
            else
            {
                return hostCores / 2;
            }
        }
    }
}
