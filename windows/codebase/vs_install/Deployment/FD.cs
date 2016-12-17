using System;
using System.Collections.Generic;
using System.IO;
using NLog;
using System.Windows.Forms;

namespace Deployment
{

    /// <summary>
    ///  class FD
    /// Files and Directories Class
    /// Working with Files and Directories
    /// Download list definition based on peer type ("trial","rh-only","client-only") and installation type ("prod", "dev", "master" )
    /// sysdrive and logdir definition
    /// </summary>
    class FD
    {
        private static NLog.Logger logger = LogManager.GetCurrentClassLogger();
        private int[] md5Sum;
        private string name;
        private static readonly string [] peerTypes = {"trial","rh-only","client-only"};
        private static readonly string[] instTypes = { "prod", "dev", "master" };

        public string Name
        {
            get { return name; }
        }

        /// <summary>
        /// Reads description file and form download list for installation
        /// </summary>
        /// <param name="fpath">Path to repo descriptor file</param>
        /// <param name="peer_type">Type of the peer - Trial(full - RH+MH+Client), RH only, Client only</param>
        /// <param name="inst_type">Type of the installation - prod, dev, master</param>
        /// <returns>String array of lines read from file</returns>
        public static string[] repo_rows(string fpath, string peer_type, string inst_type)
        {
            if (!File.Exists(fpath))
            {
                Program.ShowError("Repo descriptor file does not exist, cancelling","File does not exist");
                Program.form1.Visible = false;
                //Environment.Exit(1);
            }
            var rows = File.ReadAllLines(fpath);
            string[] rows1 = rows2download(ref rows, peer_type, inst_type);
            return rows1;
        }

        /// <summary>
        /// Copy uninstall program to temp
        /// We need it to be able to delete installation folder if user answered Yes
        /// </summary>
        /// <returns></returns>
        public static bool copy_uninstall()
        {
            string binName = Deploy.SubutaiUninstallName;
            string fpath = Path.Combine(Program.form1._arguments["appDir"], "bin", binName);
            string fpath_dest = Path.Combine(logDir(), binName);
            if (!File.Exists(fpath))
            {
                return false;
            }

            if (File.Exists(fpath_dest))
            {
                try
                {
                    File.Delete(fpath_dest);
                }
                catch (Exception ex)
                {
                    logger.Error(ex.Message + " deleting uninstall");
                    return false;
                }
            }
            
            try
            {
                File.Copy(fpath, fpath_dest, true);
                logger.Info("File uninstall-clean.exe copied");
                return true;
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message + " copying uninstall");
                return false;
            }
            
        }

        /// <summary>
        /// Define download list
        /// </summary>
        /// <param name="rows1">String array containig rows read from reo descriptor file</param>
        /// <param name="peer_type">Type of the peer (Trial (Full), RH only, Client only - </param>
        /// <param name="inst_type">Type of the insallation - prod, dev, master.</param>
        /// <returns></returns>
        public static string[] rows2download(ref string[] rows1, string peer_type, string inst_type)
        {
            List<string> rows = new List<string>();
            foreach (string row in rows1)
            {
                string [] folderFile = row.Split(new[] { "|" }, StringSplitOptions.None);

                string folder = folderFile[0].Trim();
                string file = folderFile[1].Trim();
                string need_mh = folderFile[2].Trim();
                string need_rh = folderFile[3].Trim();
                string need_client = folderFile[4].Trim();
                string need_prod = folderFile[5].Trim();
                string need_dev = folderFile[6].Trim();
                string need_master = folderFile[7].Trim();

                int pLen = peerTypes.Length;
                int iLen = instTypes.Length;
                int start_i = 2;
                int start_j = start_i + pLen;

                for (int i = start_i; i < start_i + pLen; i++) //find i for peer type
                {
                    if (peer_type.Contains(peerTypes[i - (pLen - 1)])) //found i
                    {
                        if (folderFile[i].Contains("1")) //need file
                        {
                            for (int j = start_j; j < start_j + iLen; j++)
                            {
                                if (inst_type.Contains(instTypes[j - (iLen + pLen - 1)]))
                                {
                                    if (folderFile[j].Contains("1"))
                                    {
                                        rows.Add(row);
                                        logger.Info("file to download:{0}\\{1}", folder, file);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            string[] arows = rows.ToArray();
            return arows;
        }

        public string Md5Sum
        {
            get
            {
                string md5 = "";
                foreach (var el in md5Sum)
                {
                    md5 += string.Format("{0:x}", el);
                }
                return md5;
            }
        }

        /// <summary>
        /// Define system drive
        /// </summary>
        /// <returns>System drive for example C:</returns>
        public static string sysDrive()
        {
            string sysPath = Environment.GetFolderPath(Environment.SpecialFolder.System);
            string sysDrive = Path.GetPathRoot(sysPath);
            return sysDrive;
        }

        /// <summary>
        /// Define log directory (System_drive\temp\Subutai_Log)
        /// </summary>
        /// <returns>Log directory</returns>
        public static string logDir()
        {
            string logPath = Path.Combine(sysDrive(), "temp");
            if (!Directory.Exists(logPath))
            {
                Directory.CreateDirectory(logPath);
            }
            logPath = Path.Combine(logPath, "Subutai_Log");
            if (!Directory.Exists(logPath))
            {
                Directory.CreateDirectory(logPath);
            }
            return logPath;
        }

        /// <summary>
        /// Checks if all files persist in installation directory according to target file.
        /// Not used now
        /// </summary>
        /// <param name="appDir">The application dir</param>
        /// <param name="tgt">target file name</param>
        private void check_files(string appDir, string tgt)
        {
            Deploy.StageReporter("", "Performing file check");
            string pth = $"{appDir}{tgt}";
            //Form1.download_file($"{pth}", null);
                   
            try
            {
                var rows = File.ReadAllLines(pth);
                foreach (var row in rows)
                {
                    var folderFile = row.Split(new[] { "|" }, StringSplitOptions.None);
                    var folderpath = folderFile[0].Trim();
                    var filename = folderFile[1].Trim();
                    String fullFolderPath = $"{appDir}/{folderpath.ToString()}";
                    String fullFileName = $"{appDir}/{folderpath.ToString()}/{filename.ToString()}";
                    Deploy.StageReporter("", folderpath.ToString() + "/" + filename.ToString());

                    if (!Directory.Exists(fullFolderPath))
                    {
                        logger.Info("Directory {0} not found.", fullFolderPath);
                        Program.ShowError("We are sorry, but something was wrong with Subutai installation. \nFolder" + fullFolderPath + "does not exist. \n Please uninstall Subutai, turn off all antivirus software, firewalls and SmartScreen and try again.", "Folder not exist");
                        Program.form1.Visible = false;
                    }
                    if (!File.Exists(fullFileName))
                    {
                        logger.Info("file {0}/{1} not found.", fullFolderPath, filename);
                        Program.ShowError("We are sorry, but something was wrong with Subutai installation. \nFile " + fullFileName + " does not exist. \n\nPlease uninstall Subutai, turn off all antivirus software, firewalls and SmartScreen and try again.", "File not exist");
                        Program.form1.Visible = false;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
            }
        }

        /// <summary>
        /// Edits the known hosts - removes [local] line.
        /// </summary>
        /// <param name="fpath">The path to known_hosts file</param>
        /// <returns>True on success, false on fault</returns>
        public static bool edit_known_hosts(string fpath)
        {
            string tempFile = $"{ fpath}_temp";
            try
            {
                using (var sr = new StreamReader(fpath))
                using (var sw = new StreamWriter(tempFile))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (!line.Contains("[localhost]:4567"))
                            sw.WriteLine(line);
                    }
                }
                File.Delete(fpath);
                File.Move(tempFile, fpath);
                return true;
            }
            catch (Exception ex)
            {
                logger.Error("Editing known_hosts: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Checks the bin dir - if it was not properly cleaned on previous uninstall.
        /// It can mean that some files were locked on uninstall and can be locked now.
        /// It can prevent proper unxipping.
        /// </summary>
        /// <param name="dirName">Path to Subutai bin directory</param>
        /// <returns></returns>
        public static string delete_dir_bin(string dirName)
        {
            string mesg = "";
            string binDir = Path.Combine(dirName, "bin");
            if (Directory.Exists(binDir))
            {
                string[] filenames = Directory.GetFiles(binDir, "*.lib", SearchOption.AllDirectories);
                if (filenames.Length > 0)
                {
                    foreach (string fl in filenames)
                    {
                        try
                        {
                            File.Delete(fl);
                        }
                        catch (Exception ex)
                        {
                            mesg = string.Format("Can not delete file {0}.\nPlease, check and close running applications (ssh/cmd sessions, file explorer) that can lock files \nand press OK after closing.\n\n{1}", fl, ex.Message.ToString());
                            MessageBox.Show(mesg, "Delete bin folder", MessageBoxButtons.OK);
                        }
                    }
                }

                //Deleting repo* files
                string[] repofiles = Directory.GetFiles(dirName, "repomd5*", SearchOption.TopDirectoryOnly);
                if (repofiles.Length > 0)
                {
                    foreach (string rfl in repofiles)
                    {
                        try
                        {
                            File.Delete(rfl);
                        }
                        catch (Exception ex)
                        {
                            mesg = string.Format("Can not delete file {0}.\nPlease, check and close running applications (ssh/cmd sessions, file explorer) that can lock files \nand press OK after closing.\n\n{1}", rfl, ex.Message.ToString());
                            logger.Error(mesg);
                        }
                    }

                }

                string trayDir = Path.Combine(binDir, "tray");
                if (Directory.Exists(trayDir))
                {
                    try
                    {
                        Directory.Delete(trayDir, true);
                    }
                    catch (Exception ex)
                    {
                        mesg = string.Format("Can not delete folder {0}.\nPlease, close running applications (Subutay tray, cmd sessions, file explorer) that can lock files \nand press OK after closing.\n\n{1}", trayDir, ex.Message.ToString());
                        return mesg;
                    }
                }
            }
            return "Deleted";
        }
             
    }
}
