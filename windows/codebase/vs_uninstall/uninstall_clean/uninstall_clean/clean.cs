using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.ServiceProcess;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Text.RegularExpressions;

namespace uninstall_clean
{
    public partial class clean : Form
    {
        public clean()
        {
            InitializeComponent();
            progressBar1.Visible = true;
            label2.Visible = false;
        }
        private void clean_all()
        {
            string mess = stop_service("Subutai Social P2P", 5000);
            //MessageBox.Show(mess + " Service was not running.", "Stopping P2P service", MessageBoxButtons.OK);

            mess = stop_process("SubutaiTray");
            mess = stop_process("SubutaiTray");
            //MessageBox.Show(mess + " Application was not running.", "Stopping SubutaiTray", MessageBoxButtons.OK);

            var SubutaiDir = Environment.GetEnvironmentVariable("Subutai");
            //var SubutaiDir = "c:\\4delete";
            //MessageBox.Show("Subutai Social P2P service stopped. Deleting " + SubutaiDir.ToString() + " folder", "Deleting Subutai folder", MessageBoxButtons.OK);
            if (SubutaiDir != "" && SubutaiDir != null && SubutaiDir != "C:\\" && SubutaiDir != "D:\\" && SubutaiDir != "E:\\")
            {
                mess = delete_dir(SubutaiDir);
                if (mess.Contains("Can not"))
                {
                    MessageBox.Show($"Folder {SubutaiDir} can not be removed. Please close all Subutai processes running and delete it manually", 
                        "Attention", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }
                Environment.SetEnvironmentVariable("Subutai", null);
            }
            delete_Shortcuts("Subutai");
            remove_vm();
            progressBar1.Visible = false;
            label2.Visible = true;
            label2.Text = " Please, close this window";
            
            Environment.Exit(0);
        }
        private string stop_service(string serviceName, int timeoutMilliseconds)
        {
            ServiceController service = new ServiceController(serviceName);
            try
            {
                timer1.Enabled = true;
                timer1.Start();
                timer1.Interval = 1000;
                progressBar1.Maximum = 10;
                TimeSpan timeout = TimeSpan.FromMilliseconds(timeoutMilliseconds);
                label1.Text = "Stopping " + serviceName + " service";
                service.Stop();
                service.WaitForStatus(ServiceControllerStatus.Stopped, timeout);
                //timer1.Stop();
                //timer1.Enabled = false;   
                Thread.Sleep(2000);
                return serviceName + " stopped";
            }
            catch (Exception ex)
            {
                label1.Text = "Can not stop service " + serviceName + "  " + ex.Message.ToString();
                return ex.Message.ToString();
            }

        }

        private string stop_process(string procName)
        {

            try
            {
                timer1.Enabled = true;
                timer1.Start();
                timer1.Interval = 2000;
                progressBar1.Maximum = 10;
                label1.Text = "Stopping " + procName + " process";
                Process[] proc = Process.GetProcessesByName(procName);
                proc[0].Kill();
                //timer1.Stop();
                //timer1.Enabled = false;
                Thread.Sleep(3000);
                return procName + " stopped";
            }
            catch (Exception ex)
            {
                label1.Text = "Can not stop process " + procName + ". " + ex.Message.ToString();
                return "Can not stop process " + procName + ". " + ex.Message.ToString();
            }

        }

        private string delete_dir(string dirName)
        {

            try
            {
                timer1.Enabled = true;
                timer1.Start();
                timer1.Interval = 1000;
                progressBar1.Maximum = 10;
                label1.Text = "Deleting " + dirName + " folder";
                Directory.Delete(dirName, true);
                //timer1.Stop();
                //timer1.Enabled = false;
                //deleteDirectory(dirName, true);
                Thread.Sleep(5000);
                return "Folder" + dirName + " deleted";
            }
            catch (Exception ex)
            {
                timer1.Enabled = true;
                timer1.Start();
                timer1.Interval = 1000;
                progressBar1.Maximum = 10;
                label1.Text = "Can not delete folder " + dirName + ". " + ex.Message.ToString();
                //timer1.Stop();
                //timer1.Enabled = false;
                return "Can not delete folder " + dirName + ". " + ex.Message.ToString();
            }

        }//

        private void deleteDirectory(string path, bool recursive)
        {
            // Delete all files and sub-folders?
            if (recursive)
            {
                // Yep... Let's do this
                var subfolders = Directory.GetDirectories(path);
                foreach (var s in subfolders)
                {
                    deleteDirectory(s, recursive);
                }
            }

            // Get all files of the folder
            var files = Directory.GetFiles(path);
            foreach (var f in files)
            {
                // Get the attributes of the file
                var attr = File.GetAttributes(f);

                // Is this file marked as 'read-only'?
                if ((attr & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                {
                    // Yes... Remove the 'read-only' attribute, then
                    File.SetAttributes(f, attr ^ FileAttributes.ReadOnly);
                }

                // Delete the file
                File.Delete(f);
            }

            // When we get here, all the files of the folder were
            // already deleted, so we just delete the empty folder
            Directory.Delete(path);
        }

        private void delete_Shortcut(string shPath, string aName, Boolean isDir)
        {
            var app = "";
            if (isDir)
            {
                app = aName;
            }
            else
            {
                app = aName + ".lnk";
            }

            string fullname = Path.Combine(shPath, app);
            string str = fullname;

            Boolean if_Exists;
            if (isDir)
            {
                if_Exists = Directory.Exists(fullname);
            }
            else
            {
                if_Exists = File.Exists(fullname);
            }

            if (if_Exists && !isDir)
            {
                try
                {
                    File.Delete(fullname);
                    str = "File " + fullname + " deleted"; ;
                }
                catch (Exception ex)
                {
                    str = ex.ToString();
                }
                finally
                {
                    //MessageBox.Show(str, fullname, MessageBoxButtons.OK);
                }
            }

            if (if_Exists && isDir)
            {
                try
                {
                    Directory.Delete(fullname);
                    str = fullname + " deleted";
                }
                catch (Exception ex)
                {
                    str = ex.ToString();
                }
                finally
                {
                    //MessageBox.Show(str, fullname + " Folder", MessageBoxButtons.OK);
                }
            }
        }
        private void delete_Shortcuts(string appName)
        {
            var shcutPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory);
            //    Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            //delete_Shortcut(shcutPath, appName);
            delete_Shortcut(shcutPath, appName, false);

            //shcutPath = Environment.GetFolderPath(Environment.SpecialFolder.StartMenu);
            //delete_Shortcut(shcutPath, appName);

            var shcutStartPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu);
            //Folder files
            shcutPath = Path.Combine(shcutStartPath, "Programs", appName);
            //Uninstall.lnk
            delete_Shortcut(shcutPath, "Uninstall", false);
            //Subutai.lnk
            delete_Shortcut(shcutPath, appName, false);

            //Start Menu/Programs/Subutai.lnk
            shcutPath = Path.Combine(shcutStartPath, "Programs");
            delete_Shortcut(shcutPath, appName, false);
            //Start Menu/Programs/Subutai
            delete_Shortcut(shcutPath, appName, true);
        }

        private void clean_Load(object sender, EventArgs e)
        {
            //timer1.Enabled = true;
            //timer1.Start();
            //timer1.Interval = 1000;
            //progressBar1.Maximum = 10;

            //timer1.Tick += new EventHandler(timer1_Tick);
            clean_all();

            //remove_vm();
        }

        void timer1_Tick(object sender, EventArgs e)
        {
            if (progressBar1.Value != 10)
            {
                progressBar1.Value++;
            }
            else
            {
                timer1.Stop();
            }
        }

        void remove_vm()
        {
            string outputVms = LaunchCommandLineApp("vboxmanage", $"list vms");
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
                            string vmName = wrd.Replace("\"","");
                            DialogResult drs = MessageBox.Show($"Remove virtual machine {wrd}?", "Subutai Virtual Machines",
                                MessageBoxButtons.YesNo,
                                MessageBoxIcon.Question,
                                MessageBoxDefaultButton.Button1);
                            if (drs == DialogResult.Yes)
                            {
                                //if (outputVmsRunning.Contains(wrd))
                                //{
                                string res1 = LaunchCommandLineApp("vboxmanage", $"controlvm {vmName} poweroff ");
                                Thread.Sleep(5000);
                                //}
                                string res2 = LaunchCommandLineApp("vboxmanage", $"unregistervm  --delete {vmName}");
                                Thread.Sleep(5000);
                            }
                        }
                    }
                }
            }
        }

        private string LaunchCommandLineApp(string filename, string arguments)
        {
            // Use ProcessStartInfo class
            var startInfo = new ProcessStartInfo
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                FileName = filename,
                WindowStyle = ProcessWindowStyle.Hidden,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            string output;
            string err;

            try
            {
                // Start the process with the info we specified.
                // Call WaitForExit and then the using statement will close.
                using (var exeProcess = Process.Start(startInfo))
                {
                    output = exeProcess.StandardOutput.ReadToEnd();
                    err = exeProcess.StandardError.ReadToEnd();
                    exeProcess.WaitForExit();
                    return ("executing " + filename + " \nstdout: " + output + " \nstderr: " + err);
                }
            }
            catch (Exception)
            {
                Thread.Sleep(2000);
                LaunchCommandLineApp(filename, arguments);
            }
            return (filename + " was not executed");
        }
    }
}
