using System;
using System.Collections.Generic;
using System.IO;
using NLog;

namespace Deployment
{
    class FD
    {
        private static NLog.Logger logger = LogManager.GetCurrentClassLogger();
        private int[] md5Sum;
        private string name;
        private static readonly string [] peerTypes = {"trial","rh-only","client-only"};
        private static readonly string[] instTypes = { "prod", "dev", "master" };
        //read description file and form download list for installation
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

        //define download list
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
