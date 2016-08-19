using System;
using System.Collections.Generic;
using System.IO;

namespace uninstall_clean
{
    class FD
    {
        public static string delete_dir(string dirName)
        {
            try
            {

                //label1.Text = "Deleting " + dirName + " folder";
                if (Directory.Exists(dirName))
                    Directory.Delete(dirName, true);

                //Thread.Sleep(5000);
                return "0";
            }
            catch (Exception ex)
            {

                //label1.Text = "Can not delete folder " + dirName + ". " + ex.Message.ToString();
                return "Can not delete folder " + dirName + ". " + ex.Message.ToString();
            }
        }//

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
                finally
                {
                    //MessageBox.Show(str, fullname, MessageBoxButtons.OK);
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
                finally
                {
                    //MessageBox.Show(str, fullname + " Folder", MessageBoxButtons.OK);
                }
            }
        }
        public static void delete_Shortcuts(string appName)
        {
            //label1.Text = "Deleting shortcuts";
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

        public static string remove_from_Path(string str2delete)
        {
            //logger.Info("Remove env");

            string strPath = AP.get_env_var("Path");
            //Environment.GetEnvironmentVariable("Path");
            if (str2delete == "Subutai")
            {
                str2delete = AP.get_env_var("Subutai");
                //Environment.GetEnvironmentVariable("Subutai");
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

            Environment.SetEnvironmentVariable("Path", newPath, EnvironmentVariableTarget.Machine);
            Environment.SetEnvironmentVariable("Path", newPath, EnvironmentVariableTarget.Process);

            return newPath;
        }

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
                    //MessageBox.Show($"File: {fi.Name}", "Removing file from drivers", MessageBoxButtons.OK);
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
    }
}
