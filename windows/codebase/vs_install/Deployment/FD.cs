using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Deployment
{
    class FD
    {
        private int[] md5Sum;
        private string name;

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

        public static string logDir()
        {
            string sysPath = Environment.GetFolderPath(Environment.SpecialFolder.System);
            string sysDrive = Path.GetPathRoot(sysPath);
            string logPath = Path.Combine(sysDrive, "temp");
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

        public string Name
        {
            get { return name; }
        }
        /*
                private void check_files()
                {
                    StageReporter("", "Performing file check");
                    download_file($"{ _arguments["appDir"]}{_arguments["repo_tgt"]}");
                    string pth = $"{_arguments["appDir"]}{_arguments["repo_tgt"]}";
                    try
                    {
                        var rows = File.ReadAllLines(pth);
                        foreach (var row in rows)
                        {
                            var folderFile = row.Split(new[] { "|" }, StringSplitOptions.None);
                            var folderpath = folderFile[0].Trim();
                            var filename = folderFile[1].Trim();
                            String fullFolderPath = $"{_arguments["appDir"]}/{folderpath.ToString()}";
                            String fullFileName = $"{_arguments["appDir"]}/{folderpath.ToString()}/{filename.ToString()}";
                            StageReporter("", folderpath.ToString() + "/" + filename.ToString());

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
        */

    }
}
