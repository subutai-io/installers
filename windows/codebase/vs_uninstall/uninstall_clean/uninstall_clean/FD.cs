using System;
using System.Collections.Generic;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Windows.Forms;

namespace uninstall_clean
{
    /// <summary>
    /// Working with files and Directories
    /// </summary>
    class FD
    {
        /// <summary>
        /// The current user
        /// </summary>
        public static SecurityIdentifier cu = WindowsIdentity.GetCurrent().User;
        /// <summary>
        /// The current user name
        /// </summary>
        public static string cu_name = System.Security.Principal.WindowsIdentity.GetCurrent().Name;

        /// <summary>
        /// Deletes the directory.
        /// </summary>
        /// <param name="dirName">Name of the dir.</param>
        /// <returns>"0" if deleted, error message if not</returns>
        public static string delete_dir(string dirName)
        {
            try
            {
                if (Directory.Exists(dirName))
                    Directory.Delete(dirName, true);
                return "0";
            }
            catch (Exception ex)
            {
                return "Can not delete folder " + dirName + ". " + ex.Message.ToString();
            }
        }

        /// <summary>
        /// Deletes the Subutai bin directory.
        /// </summary>
        /// <param name="dirName">Name of the dir.</param>
        /// <returns>"0" if deleted, eror message with exception message if not</returns>
        public static string delete_dir_bin(string dirName)
        {
            string mesg = "";
            string[] sfiles = Directory.GetFiles(dirName, "*", SearchOption.TopDirectoryOnly);
            if (sfiles.Length > 0)
            {
                foreach (string sfl in sfiles)
                {
                    try
                    {
                        File.Delete(sfl);
                    }
                    catch (Exception ex)
                    {
                        mesg = string.Format("Can not delete file {0}.\nPlease, check and close running applications (ssh/cmd sessions, file explorer) that can lock files \nand press OK after closing.\n\n{1}",
                            sfl, ex.Message.ToString());
                    }
                }
            }

            string redistDir = Path.Combine(dirName, "redist\\subutai");
            if (Directory.Exists(redistDir))
            {
                try
                {
                    Directory.Delete(redistDir, true);
                }
                catch (Exception)
                {
                    string[] files = Directory.GetFiles(redistDir, "*", SearchOption.TopDirectoryOnly);
                    foreach (string fl in files)
                    {
                        try
                        {
                            File.Delete(fl);
                        }
                        catch (Exception ex)
                        {
                            mesg = "Can not delete folder " + redistDir + " and files. Close running applications that can lock files and delete manually." + ex.Message.ToString();
                        }
                    }
                }
            }

            string binDir = Path.Combine(dirName, "bin");
            if (Directory.Exists(binDir))
            {
                try
                {
                    Directory.Delete(binDir, true);
                }
                catch (Exception)
                {
                    string[] files = Directory.GetFiles(binDir, "*", SearchOption.TopDirectoryOnly);
                    foreach (string fl in files)
                    {
                        try
                        {
                            File.Delete(fl);
                        }
                        catch (Exception ex)
                        {
                            return "Can not delete folder " + binDir + " and files. Close running applications that can lock files and delete manually." + ex.Message.ToString();
                        }
                    }
                }
            }
           return "0";
        }

        /// <summary>
        /// Deletes the directory with changing file attributes
        /// in case if directory contains read only files.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="recursive">if set to <c>true</c> [recursive].</param>
        public static void deleteDirectory(string path, bool recursive)
        {
            // Delete all files and sub-folders?
            if (recursive)
            {
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
                    //Remove the 'read-only' attribute, then
                    File.SetAttributes(f, attr ^ FileAttributes.ReadOnly);
                }

                // Delete the file
                File.Delete(f);
            }
            //Delete the empty folder
            Directory.Delete(path);
        }

        /// <summary>
        /// Deletes  shortcut.
        /// </summary>
        /// <param name="shPath">The shoertcut path.</param>
        /// <param name="aName">Application name</param>
        /// <param name="isDir">if shortcut is directory: <c>true</c> [is dir].</param>
        public static void delete_Shortcut(string shPath, string aName, Boolean isDir)
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
            }

            if (if_Exists && isDir)
            {
                try
                {
                    Directory.Delete(fullname, true);
                    str = fullname + " deleted";
                }
                catch (Exception ex)
                {
                    str = ex.ToString();
                }
            }
        }

        /// <summary>
        /// Deletes common and user's shortcuts for application.
        /// </summary>
        /// <param name="appName">Name of the application.</param>
        public static void delete_Shortcuts(string appName)
        {
            //Commom Desktop
            var shcutPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory);
            delete_Shortcut(shcutPath, appName, false);

            //Common StartMenu/Programs
            //This path to Common Start Menu will be used later
            var shcutStartPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu);
            shcutPath = Path.Combine(shcutStartPath, "Programs");
            delete_Shortcut(shcutPath, appName, false);

            //Common StartMenu/Startup
            shcutPath = Path.Combine(shcutStartPath, "Programs", "Startup");
            delete_Shortcut(shcutPath, appName, false);

            //Common Subutai Folder files
            shcutStartPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms);
            shcutPath = Path.Combine(shcutStartPath, appName);
            //Subutai.lnk
            delete_Shortcut(shcutPath, appName, false);
            //Uninstall.lnk
            delete_Shortcut(shcutPath, "Uninstall", false);

            //Common Subutai folder
            delete_Shortcut(shcutStartPath, appName, true);

            //The same if we have shortcuts only for user 
            //User StartMenu/Programs
            shcutStartPath = Environment.GetFolderPath(Environment.SpecialFolder.StartMenu);
            shcutPath = Path.Combine(shcutStartPath, "Programs");
            delete_Shortcut(shcutPath, appName, false);

            //Common StartMenu/Startup
            shcutPath = Path.Combine(shcutStartPath, "Programs", "Startup");
            delete_Shortcut(shcutPath, appName, false);

            //User Subutai Folder files
            shcutStartPath = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
            shcutPath = Path.Combine(shcutStartPath, appName);
            //Subutai.lnk
            delete_Shortcut(shcutPath, appName, false);
            //Uninstall.lnk
            delete_Shortcut(shcutPath, "Uninstall", false);
            delete_Shortcut(shcutStartPath, appName, true);
        }

        /// <summary>
        /// Removes Subutai from Path environment variable
        /// </summary>
        /// <param name="str2delete">The str2delete.</param>
        /// <returns></returns>
        public static string remove_from_Path(string str2delete)
        {
            //logger.Info("Remove env");

            string strPath = AP.get_env_var("Path");
            if (str2delete == "Subutai")
            {
                str2delete = AP.get_env_var("Subutai");
            }

            if (strPath == null || strPath == "")
                return "Path Empty";

            if (str2delete == null || str2delete == "")
                return $"{str2delete} Empty";

            string[] strP = strPath.Split(';');
            List<string> lPath = new List<string>();

            foreach (string sP in strP)
            {
                if (!lPath.Contains(sP) && !sP.Contains(str2delete))
                {
                    lPath.Add(sP);
                }
            }

            string[] slPath = lPath.ToArray();
            string newPath = string.Join(";", slPath);
            newPath = newPath.Replace(";;", ";");

            Environment.SetEnvironmentVariable("Path", newPath, EnvironmentVariableTarget.Machine);
            Environment.SetEnvironmentVariable("Path", newPath, EnvironmentVariableTarget.Process);

            return newPath;
        }

        /// <summary>
        /// Define directory name for logs: <SystemDrive>\temp\Subutai_Log
        /// </summary>
        /// <returns></returns>
        public static string logDir()
        {

            string logPath = Path.Combine(clean.sysDrive, "temp");
            if (!Directory.Exists(logPath))
            {
                Directory.CreateDirectory(logPath);
            }
            logPath = Path.Combine(logPath, "Subutai_Log");
            if (!Directory.Exists(logPath))
            {
                return "";
            }
            return logPath;
        }

        /// <summary>
        /// Removes old logs and uninstall-clean from log dir
        /// </summary>
        public static void remove_log_dir()
        {
            //Find if log directory exists 
            string lDir = logDir();
            if (lDir == "")
                return;

            string today = $"{ DateTime.Now.ToString("yyyy-MM-dd")}";
            DirectoryInfo di = new DirectoryInfo(lDir);
            foreach (FileInfo fi in di.GetFiles())
            {
                if (!fi.Name.ToLower().Contains(today) && !fi.Name.ToLower().Contains(".reg"))
                {
                    try
                    {
                        fi.Delete();
                    }
                    catch (Exception e)
                    {
                        string res = e.Message;
                    }
                }
            }
        }

        /// <summary>
        /// Removes <InstallDir>\home link (created for ssh to access user's home\.ssh\)
        /// </summary>
        /// <param name="instDir">The installation directory name.</param>
        /// <returns></returns>
        public static string remove_home(string instDir)
        {
            if (instDir == "")
            {
                return "Not exists";
            }
            string path_l = Path.Combine(instDir, "home");

            if (Directory.Exists(path_l))
            {
                try
                {
                    Directory.Delete(path_l);
                    return "OK";
                }
                catch (Exception ex)
                {
                    return ("Error: " + ex.Message);
                }
            }
            return path_l;
        }

        //Remove known_hosts file
        /// <summary>
        /// Removes files from /home folder.
        /// </summary>
        /// <param name="instDir">The installation directory.</param>
        /// <returns></returns>
        public static string remove_from_home(string instDir)
        {
            string res = "1";
            if (instDir == "")
            {
                return "Not exists";
            }
            
            string uname = Environment.GetEnvironmentVariable("Username");
            if (uname == "" || uname == null)
            {
                return "Not exists";
            }
            string path_l = Path.Combine(instDir, "home", uname, ".ssh", "known_hosts");
            if (File.Exists(path_l))
            {
                try
                {
                    File.Delete(path_l);
                    res = "0";
                }
                catch (Exception e)
                {
                    res = e.Message;
                 
                }
            }
            return res;
        }

        public static bool del_sysfile(string drvPath)
        {
            //take ownership
            //Get Currently Applied Access Control
            FileSecurity fileS = new FileSecurity();//File.GetAccessControl(drvPath);
            string tmp = "";
            try
            {
                fileS = File.GetAccessControl(drvPath);
            }
            catch (Exception e)
            {
                tmp = e.Message;
                return false;
            }
            //Update it, Grant Current User Full Control
            
            try
            {
                fileS.SetOwner(cu);
            }
            catch (Exception e)
            {
                tmp = e.Message;
                return false;
            }
            try
            {
                fileS.SetAccessRule(new FileSystemAccessRule(cu,
                    FileSystemRights.FullControl, AccessControlType.Allow));
            }
            catch (Exception e)
            {
                tmp = e.Message;
                return false;
            }
            //Update the Access Control on the File
            try
            {
                File.SetAccessControl(drvPath, fileS);
            }
            catch (Exception e)
            {
                tmp = e.Message;
                return false;
            }
            //Delete the file
            try
            {
                File.Delete(drvPath);
                return true;
            }
            catch (Exception e)
            {
                tmp = e.Message;
                return false;
            }
         }

        /// <summary>
        /// Removes Chrome directories and shortcuts.
        /// </summary>
        public static void fd_clean_chrome()
        {
            //var dirApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            //var dirPath = Path.Combine(dirApp, "Google", "Chrome");
            ////Deleting user's app folder
            //try
            //{
            //    if (Directory.Exists(dirPath))
            //        Directory.Delete(dirPath, true);

            //    //Thread.Sleep(5000);
            //    //return "0";
            //}
            //catch (Exception ex)
            //{
            //    MessageBox.Show("Can not delete folder " + dirPath + ". " + ex.Message.ToString());
            //}

            //Deleting from Program Files
            var dirApp = Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFiles);
            var dirPath = Path.Combine(dirApp, "Google", "Chrome");
            //Deleting user's app folder
            try
            {
                if (Directory.Exists(dirPath))
                    Directory.Delete(dirPath, true);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Can not delete folder " + dirPath + ". " + ex.Message.ToString());
            }

            //Deleting from Program Files
            dirApp = Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFilesX86);
            dirPath = Path.Combine(dirApp, "Google", "Chrome");
            //Deleting user's app folder
            try
            {
                if (Directory.Exists(dirPath))
                    Directory.Delete(dirPath, true);
            }
            catch (Exception ex)
            {

                MessageBox.Show("Can not delete folder " + dirPath + ". " + ex.Message.ToString());
            }

            //C:\ProgramData\Microsoft\Windows\Start Menu\Programs
            //C: \Users\Public\Desktop
            //Commom Desktop
            var shcutPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory);
            delete_Shortcut(shcutPath, "Google Chrome", false);
            //MessageBox.Show($"shcut {shcutPath} cleaned", "Chrome shortcuts", MessageBoxButtons.OK);

            //Common StartMenu/Programs
            //This path to Common Start Menu will be used later
            shcutPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu);
            shcutPath = Path.Combine(shcutPath, "Programs");
            delete_Shortcut(shcutPath, "Google Chrome", false);
            //MessageBox.Show($"shcut {shcutPath} cleaned", "Chrome shortcuts", MessageBoxButtons.OK);
        }
    }
}
