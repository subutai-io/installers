using System;
using System.IO;
using System.Windows.Forms;
using System.ServiceProcess;
using System.Diagnostics;
using System.Threading;
using Microsoft.Win32;
using System.Linq;
using System.Text.RegularExpressions;

namespace uninstall_clean
{
    class VBx
    {
        public void remove_app_vbox(string app_name)
        {
            DialogResult drs = MessageBox.Show($"Remove {app_name}?", $"Removing {app_name}",
                              MessageBoxButtons.YesNo,
                              MessageBoxIcon.Question,
                              MessageBoxDefaultButton.Button1);

            if (drs == DialogResult.No)
                return;
            //string res = "";
            //VirtualBox Manager, VirtualBox Interface
            //Stop VMs

            //Stop Processes/Services
            Process[] vboxProcesses = Process.GetProcesses();
            foreach (Process process in vboxProcesses)
            {
                if (process.ProcessName.Contains("VBox") || process.ProcessName.Contains("VirtualBox"))
                {
                    MessageBox.Show($"Process: {process.ProcessName}", "Removing processes", MessageBoxButtons.OK);
                    process.Kill();
                }
            }

            //Remove drivers C:\Windows\System32\drivers
            //C:\Windows\System32\drivers\VBoxDrv.sys, VBoxNetAdp6.sys, VBoxUSBMon.sys
            string dirStart = Path.Combine(clean.sysDrive, "Windows", "System32", "drivers");

            MessageBox.Show($"dirStart: {dirStart}", "Drivers directory", MessageBoxButtons.OK);

            var allFilenames = Directory.EnumerateFiles(dirStart).Select(p => Path.GetFileName(p));

            DirectoryInfo di = new DirectoryInfo(dirStart);
            foreach (FileInfo fi in di.GetFiles())
            {
                if (fi.Name.ToLower().Contains("vbox"))
                {
                    MessageBox.Show($"File: {fi.Name}", "Removing file from drivers", MessageBoxButtons.OK);
                    //fi.Delete();
                }
            }

            //string[] fArr = { };
            //try
            //{
            //    fArr = Directory.GetFiles(dirStart, "");
            //}
            //catch (Exception e)
            //{
            //    string tmp = e.Message;
            //}

            //foreach (string fileName in fArr)
            //{
            //    if (fileName.ToLower().Contains("vbox"))
            //    {

            //        MessageBox.Show($"File: {fileName}", "Removing file from drivers", MessageBoxButtons.OK);
            //        //string filePath = Path.Combine(dirStart, fileName);
            //        string filePath = fileName;
            //        if (File.Exists(filePath))
            //        {
            //            try
            //            {
            //                File.Delete(filePath);
            //            }
            //            catch (Exception e)
            //            {
            //                string res = e.Message;
            //                continue;
            //            }
            //        }
            //    }
            // }

            //Remove C:\Program Files\Oracle\VirtualBox
            //dirStart = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);

            dirStart = AP.get_env_var("VBOX_MSI_INSTALL_PATH");
            if (dirStart == null || dirStart == "")
            {
                dirStart = Path.Combine(clean.sysDrive, "Program Files", "Oracle", "VirtualBox");
            }

            MessageBox.Show($"Dir: {dirStart}", "Removing Oracle Dir", MessageBoxButtons.OK);
            if (Directory.Exists(dirStart))
            {
                Directory.Delete(dirStart, true);
            }

            //Clear Registry: VBoxDrv, VBoxNetAdp, VBoxUSBMon
            //HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\VBoxDrv
            //HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\VBoxNetAdp
            //HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\VBoxUSBMon
            var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            string subkey = "SYSTEM\\CurrentControlSet\\Services";
            RG.DeleteSubKeyFound(subkey, "VBox", RegistryHive.LocalMachine);
            //HKEY_LOCAL_MACHINE\SOFTWARE\Oracle\VirtualBox
            RG.DeleteSubKeyTree("VirtualBox", "SOFTWARE\\Oracle", RegistryHive.LocalMachine);

            //Remove Env VBOX_MSI_INSTALL_PATH
            Environment.SetEnvironmentVariable("VBOX_MSI_INSTALL_PATH", "", EnvironmentVariableTarget.Machine);
            Environment.SetEnvironmentVariable("VBOX_MSI_INSTALL_PATH", "", EnvironmentVariableTarget.User);
            Environment.SetEnvironmentVariable("VBOX_MSI_INSTALL_PATH", "", EnvironmentVariableTarget.Process);

            //Remove shortcuts
            var shcutPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory);
            //    Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            //delete_Shortcut(shcutPath, appName);
            string appName = "Oracle VM VirtualBox";
            FD.delete_Shortcut(shcutPath, appName, false);
            shcutPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory);

            var shcutStartPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu);
            //Folder files
            shcutPath = Path.Combine(shcutStartPath, "Programs");
            //Uninstall.lnk
            FD.delete_Shortcut(shcutPath, appName, false);
            //Remove folder
            FD.delete_Shortcut(shcutPath, appName, true);
        }

        public static void remove_vm()
        {
            //label1.Text = "Virtual Machines";
            string outputVms = SCP.LaunchCommandLineApp("vboxmanage", $"list vms", true, false);
            if (outputVms.Contains("Error"))
            {
                return;
            }
            //string outputVmsRunning = LaunchCommandLineApp("vboxmanage", $"list runningvms");
            string[] rows = Regex.Split(outputVms, "\n");
            foreach (string row in rows)
            {
                if (row.Contains("subutai") || row.Contains("snappy"))
                {
                    string[] wrds = row.Split(' ');
                    foreach (string wrd in wrds)
                    {
                        if (wrd.Contains("subutai") || wrd.Contains("snappy"))
                        {
                            string vmName = wrd.Replace("\"", "");
                            DialogResult drs = MessageBox.Show($"Remove virtual machine {wrd}?", "Subutai Virtual Machines",
                                MessageBoxButtons.YesNo,
                                MessageBoxIcon.Question,
                                MessageBoxDefaultButton.Button1);
                            if (drs == DialogResult.Yes)
                            {
                                string res1 = SCP.LaunchCommandLineApp("vboxmanage", $"controlvm {vmName} poweroff ", true, false);
                                Thread.Sleep(5000);
                                string res2 = SCP.LaunchCommandLineApp("vboxmanage", $"unregistervm  --delete {vmName}", true, false);
                                Thread.Sleep(5000);
                            }
                        }
                    }
                }
            }
        }

        //Find if registry cleaner exists 
        //string rclean_path = logDir();
        //if (rclean_path != "")
        //{
        //    rclean_path = Path.Combine(rclean_path, "subutai-clean-registry.reg");
        //LaunchCommandLineApp("regedit.exe", $"/s {rclean_path}", false, true);
        //}

    }

}
