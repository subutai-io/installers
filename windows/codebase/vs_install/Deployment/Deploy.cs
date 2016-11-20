using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
//using System.Reflection;
using System.Threading;
using Deployment.items;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using IWshRuntimeLibrary;
using NLog;
//using MonoTorrent.Client;
//using MonoTorrent.Client.Encryption;
//using MonoTorrent.Common;
using Renci.SshNet;
using File = System.IO.File;
//using Microsoft.Win32;


namespace Deployment
{
    /// <summary>
    /// public class Deploy
    /// Contains different utilitites
    /// </summary>
    public class Deploy
    {
        private const string RestFileinfoURL = "/kurjun/rest/raw/info?name=";
        private const string RestFileURL = "/kurjun/rest/raw/get?id=";
        /// <summary>
        /// The subutai tray application name
        /// </summary>
        public static string SubutaiTrayName = "SubutaiTray.exe";
        private const string SubutaiIconName = "Subutai_logo_4_Light_70x70.ico";
        /// <summary>
        /// The subutai uninstall application name
        /// </summary>
        public static string SubutaiUninstallName = "uninstall-clean.exe";
        private const string SubutaiUninstallIconName = "uninstall.ico";
        private readonly Dictionary<string, string> _arguments;
        private static NLog.Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// public Deploy
        /// </summary>
        /// <param name="arguments">arguments dictionary</param>
        public Deploy(Dictionary<string, string> arguments)
        {
            this._arguments = arguments;
        }

        /// <summary>
        /// public void SetEnvironmentVariables()
        /// Set environment variables %Path% and %Subutai%
        /// </summary>
        public void SetEnvironmentVariables()
        {
            string sysDrive = FD.sysDrive();
            //string path_orig = Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.Machine);
            string path_orig = Environment.GetEnvironmentVariable("Path");
            logger.Info("Orig: {0}", path_orig);
            if (!path_orig.Contains("VirtualBox"))
            {
                path_orig += $";{sysDrive}Program Files\\Oracle\\VirtualBox";
                //logger.Info("VirtualBox: {0}", path_orig);
            }

            if (!path_orig.Contains("TAP-Windows"))
                {
                path_orig += $";{sysDrive}Program Files\\TAP-Windows\\bin";
                //logger.Info("TAP-Windows: {0}", path_orig);
            }

            if (!path_orig.Contains("Subutai"))
            {
                path_orig += $";{_arguments["appDir"]}bin";
                path_orig += $";{_arguments["appDir"]}bin\\tray";
               
            }

            //            logger.Info("Path changed: {0}", Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.Machine));
            path_orig = path_orig.Replace(";;", ";"); 
            Environment.SetEnvironmentVariable("Path", path_orig, EnvironmentVariableTarget.Machine);
            Environment.SetEnvironmentVariable("Path", path_orig, EnvironmentVariableTarget.Process);//comment to test Sirmen's issue
            logger.Info("Pat machine: {0}", Environment.GetEnvironmentVariable("Path"), EnvironmentVariableTarget.Machine);
            logger.Info("Path Process: {0}", Environment.GetEnvironmentVariable("Path"), EnvironmentVariableTarget.Process);

            Environment.SetEnvironmentVariable("Subutai", _arguments["appDir"], EnvironmentVariableTarget.Machine);
            Environment.SetEnvironmentVariable("Subutai", _arguments["appDir"], EnvironmentVariableTarget.Process);//comment to test Sirmen's issue

            logger.Info("Subutai machine: {0}", Environment.GetEnvironmentVariable("Subutai"), EnvironmentVariableTarget.Machine);
            logger.Info("Subutai Process: {0}", Environment.GetEnvironmentVariable("Subutai"), EnvironmentVariableTarget.Process);

        }

        #region HELPERS: Download
        /// <summary>
        /// public void DownloadFile(string url, string destination, AsyncCompletedEventHandler onComplete, 
        /// string report, bool async, bool kurjun)
        /// Performing file download according to download list
        /// </summary>
        /// <param name="url">URL to download from</param>
        /// <param name="destination">destination path\file</param>
        /// <param name="onComplete">What will be executed on complete</param>
        /// <param name="report">String to be written to installation form </param>
        /// <param name="async">If async download</param>
        /// <param name="kurjun">If download from kurjun</param>
        public void DownloadFile(string url, string destination, AsyncCompletedEventHandler onComplete, string report, bool async, bool kurjun)
        {
            var md5 = "";
            if (kurjun)
            {
                var filename = Path.GetFileName(destination);
                var info = request_kurjun_fileInfo(url, RestFileinfoURL, filename);
                if (info == null)
                {
                    Program.ShowError("File does not exist", "File error");
                    Program.form1.Visible = false;
                }
                url = url + RestFileURL + info.id;
                md5 = info.id.Replace("raw.", "");
             
                if (!Program.form1.PrerequisiteFilesInfo.ContainsKey(destination))
                {
                    Program.form1.PrerequisiteFilesInfo.Add(destination, info);
                }
                logger.Info("Getting file {0} from kurjun, md5sum:{1}", destination, md5);
            }

            var shouldWeDownload = true;//will download in any case now
            if (destination.Contains("tray-dev"))
            {
                destination = destination.Remove(destination.IndexOf('-'), 4);
            }

            if (destination.Contains("_dev"))
            {
                destination = destination.Remove(destination.IndexOf('_'), 4);
            }

            if (destination.Contains("-test") && !destination.Contains("repomd5"))
            {
                destination = destination.Remove(destination.IndexOf('-'), 5);
            }

            var fileInfo = new FileInfo(destination);
            if (fileInfo.Exists)
            {
                var calculatedMd5 = Calc_md5(destination, false);
                if (calculatedMd5 != md5)
                {
                    shouldWeDownload = true;
                }
                else
                {
                    shouldWeDownload = false;
                }
            }
   
            if (destination.Contains("chrome") && Inst.app_installed("Clients\\StartMenuInternet\\Google Chrome") == 1)
            {
                shouldWeDownload = false;
            }
            if (destination.Contains("virtualbox"))
            {
                //vbox already installed or peer optin is client-only
                if ( Inst.app_installed("Oracle\\VirtualBox") == 1 || _arguments["peer"] == "client-only")
                    shouldWeDownload = false;
            }
            
            //logger.Info("shouldWeDownload = {0}", shouldWeDownload.ToString());

            if (shouldWeDownload)
            {
                var dirInfo = new DirectoryInfo(path: Path.GetDirectoryName(destination));
                if (!dirInfo.Exists)
                {
                    dirInfo.Create();
                    logger.Info("Directory created: {0}", destination);
                }

                StageReporter("", report);
                var webClient = new WebClient();

                if (onComplete != null)
                {
                    webClient.DownloadFileCompleted += onComplete;
                }
                webClient.DownloadProgressChanged += ProgressChanged;
                try
                {
                    if (async)
                    {
                        webClient.DownloadFileAsync(new Uri(url), destination);
                    }
                    else
                    {
                        webClient.DownloadFile(new Uri(url), destination);
                    }
                    //logger.Info("Download {0}", destination);
                }
                catch (Exception ex)
                {
                    logger.Error(ex.Message, destination);
                    Program.ShowError("Subutai repository is not available for some reason. Please try again later.",
                        "Repository Error");
                }
            }
            else
            {
                onComplete?.Invoke(null, null);
                //onComplete?.Invoke(null, AsyncCompletedEventArgs.Empty);
            }
        }

        /// <summary>
        /// private void ProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        /// Updates progress on installation form
        /// </summary>
        /// <param name="sender">sender</param>
        /// <param name="e">DownloadProgressChangedEventArgs</param>
        private void ProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            //MessageBox.Show(e.ProgressPercentage.ToString());
            Program.form1.Invoke((MethodInvoker) delegate
            {
                //Program.form1.progressBarControl1.EditValue = e.ProgressPercentage;
                UpdateProgress(e.ProgressPercentage);
            });
        }
        #endregion

        #region HELPERS: Unzip files

        /// <summary>
        /// Unzips all files with .zip extention in folder.
        /// </summary>
        /// <param name="folderPath">The folder path.</param>
        public void unzip_files(string folderPath)
        {
            logger.Info("Unzipping files from {0}", folderPath);
            var filenames = Directory.GetFiles(folderPath, "*.zip", SearchOption.AllDirectories).Select(Path.GetFullPath).ToArray();
            foreach (var filename in filenames)
            {
                var fileinfo = new FileInfo(filename);
                logger.Info("Unzipping file {0}", filename);
                unzip_file(filename, fileinfo.DirectoryName, true);
            }
        }

        /// <summary>
        /// Unzips the file.
        /// </summary>
        /// <param name="source">The source file path.</param>
        /// <param name="dest">The destination path.</param>
        /// <param name="remove">if set to <c>true</c> [remove] source file.</param>
        public void unzip_file(string source, string dest, bool remove)
        {
            ZipFile zf = null;
            try
            {
                FileStream fs = File.OpenRead(source);
                zf = new ZipFile(fs);
                if (!String.IsNullOrEmpty(""))
                {
                    zf.Password = ""; // AES encrypted entries are handled automatically
                }
                foreach (ZipEntry zipEntry in zf)
                {
                    if (!zipEntry.IsFile)
                    {
                        continue; // Ignore directories
                    }
                    String entryFileName = zipEntry.Name;
                    // to remove the folder from the entry:- entryFileName = Path.GetFileName(entryFileName);
                    // Optionally match entrynames against a selection list here to skip as desired.
                    // The unpacked length is available in the zipEntry.Size property.

                    byte[] buffer = new byte[4096]; // 4K is optimum
                    Stream zipStream = zf.GetInputStream(zipEntry);

                    // Manipulate the output filename here as desired.
                    String fullZipToPath = Path.Combine(dest, entryFileName);
                    string directoryName = Path.GetDirectoryName(fullZipToPath);
                    if (directoryName.Length > 0)
                        Directory.CreateDirectory(directoryName);

                    // Unzip file in buffered chunks. This is just as fast as unpacking to a buffer the full size
                    // of the file, but does not waste memory.
                    // The "using" will close the stream even if an exception occurs.

                    using (FileStream streamWriter = File.Create(fullZipToPath))
                    {
                        var progressHandler = new ProgressHandler(
                            (object o, ICSharpCode.SharpZipLib.Core.ProgressEventArgs ex) =>
                            {
                                var percentage = ex.Processed * 100 / zipEntry.Size;

                                Program.form1.prBar_.Parent.Invoke((MethodInvoker)delegate
                                {
                                    //Program.form1.progressBarControl1.EditValue = percentage;
                                    Program.form1.prBar_.Value = (int)percentage;
                                });
                            });
                        StreamUtils.Copy(zipStream, streamWriter, buffer, progressHandler, new TimeSpan(), Program.form1, "none", 100);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                Program.ShowError("","Extracting zip");
            }
            finally
            {
                if (zf != null)
                {
                    zf.IsStreamOwner = true; // Makes close also shut the underlying stream
                    zf.Close(); // Ensure we release resources

                    if (remove)
                        new FileInfo(source).Delete();
                }
            }
        }

        #endregion

        #region HELPERS: retrieve fileinfo

        /// <summary>
        /// Requests the kurjun file information.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="restURL">The rest URL.</param>
        /// <param name="filename">The filename.</param>
        /// <returns>KurjunFileInfo structure</returns>
        private KurjunFileInfo request_kurjun_fileInfo(string url, string restURL, string filename)
        {
            var json = rest_api_request(url + restURL + filename);
            KurjunFileInfo kfi;
            try
            {
                kfi = new System.Web.Script.Serialization.JavaScriptSerializer().Deserialize<KurjunFileInfo>(json);
                return kfi;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "{0} info error", filename);
                Program.ShowError("File does not exist: " + filename, "File error");

                return null;
            }
        }
        #endregion

        #region UTILITIES: Launch commandline application
        /// <summary>
        /// Launches the command line application.
        /// </summary>
        /// <param name="filename">The filename.</param>
        /// <param name="arguments">The arguments.</param>
        /// <returns></returns>
        public static string LaunchCommandLineApp(string filename, string arguments)
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
            logger.Info("trying to exe {0} {1}", filename, arguments);
            try
            {
                // Start the process with the info we specified.
                // Call WaitForExit and then the using statement will close.
                using (var exeProcess = Process.Start(startInfo))
                {
                    output = exeProcess.StandardOutput.ReadToEnd();
                    err  = exeProcess.StandardError.ReadToEnd();
                    exeProcess?.WaitForExit();
                    return ($"executing: \"{filename} {arguments}\"|{output}|{err}");
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message, "can not run process {0}", filename);//try to repeat, counting 
                Thread.Sleep(10000); //uncomment if need repeated tries 
                //LaunchCommandLineApp(filename, arguments, 0);//will try 3 times
            }
            return ($"1|{filename} was not executed|Error");
        }

        /// <summary>
        /// Launches the command line application with repeats.
        /// </summary>
        /// <param name="filename">The filename.</param>
        /// <param name="arguments">The arguments.</param>
        /// <param name="try_counter">The try counter.</param>
        /// <returns></returns>
        public static string LaunchCommandLineApp(string filename, string arguments, int try_counter)
        {
            // try execute desktop commant 3 times
            int count = try_counter;
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
            count++; 
            logger.Info("trying to exe {0} {1} {2} time", filename, arguments, count);
            try
            {
                // Start the process with the info we specified.
                // Call WaitForExit and then the using statement will close.
                using (var exeProcess = Process.Start(startInfo))
                {
                    output = exeProcess.StandardOutput.ReadToEnd();
                    err = exeProcess.StandardError.ReadToEnd();
                    exeProcess?.WaitForExit();
                    return ($"executing: \"{filename} {arguments}\"|{output}|{err}");
                }
            }
            catch (Exception ex)
            {
                if (count > 3)
                     return ($"command \"{filename} {arguments}\" can not run {try_counter} times");
                logger.Error(ex.Message, "can not run process {0} {1} time(s)", filename, try_counter);
                Thread.Sleep(10000);
                LaunchCommandLineApp(filename, arguments, count); //try to execue again 
            }
            return ($"1|{filename} was not executed|Error");
        }
        #endregion

        #region UTILITIES: Send SSH command

        /// <summary>
        /// Sends the SSH command with username/password authentication.
        /// </summary>
        /// <param name="hostname">The hostname.</param>
        /// <param name="port">The port.</param>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <param name="command">The command.</param>
        /// <returns>exit code | output| error</returns>
        public static string SendSshCommand(string hostname, int port, string username, string password, string command)
        {
            using (var client = new SshClient(hostname, port, username, password))
            {
                client.Connect();
                SshCommand scmd = client.RunCommand(command);
                int exitstatus = scmd.ExitStatus;
                string sresult = scmd.Result;
                if (sresult == null || sresult == "" || sresult == " " )
                    sresult = "Empty";
                string serror = scmd.Error;
                if (serror == null || serror == "")
                    serror = "Empty";
                client.Disconnect();
                client.Dispose();
                return exitstatus.ToString() + "|" + sresult + "|" + serror;
             }
        }

        /// <summary>
        /// Sends the SSH command with private key authentication.
        /// </summary>
        /// <param name="hostname">The hostname.</param>
        /// <param name="port">The port.</param>
        /// <param name="username">The username.</param>
        /// <param name="keys">The private keys array.</param>
        /// <param name="command">The command.</param>
        /// <returns>exit code | output| error</returns>
        public static string SendSshCommand(string hostname, int port, string username, PrivateKeyFile[] keys, string command)
        {
            using (var client = new SshClient(hostname, port, username, keys))
            {
                client.Connect();
                SshCommand scmd = client.RunCommand(command);
                int exitstatus = scmd.ExitStatus;
                string sresult = scmd.Result;
                if (sresult == null || sresult == "")
                    sresult = "Empty";
                string serror = scmd.Error;
                if (serror == null || serror == "")
                    serror = "Empty";
                //Stream soutput = scmd.ExtendedOutputStream;
                client.Disconnect();
                client.Dispose();
                return exitstatus.ToString() + "|" + sresult + "|" + serror;
            }
        }

        /// <summary>
        /// Retrieves part of putput of Launch command (returning exit code | output| error)
        /// </summary>
        /// <param name="outstr">The outstr.</param>
        /// <param name="ind">The ind.</param>
        /// <returns></returns>
        public static string com_out(string outstr, int ind)
        {
            string[] sa = outstr.Split('|');
            return sa[ind];
        }

        /// <summary>
        /// Sends the file to virtual machine using SSH.NET SFTP.
        /// </summary>
        /// <param name="hostname">The hostname.</param>
        /// <param name="port">The port.</param>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <param name="localFilesPath">The local files path.</param>
        /// <param name="remotePath">The remote path.</param>
        /// <returns></returns>
        public static string SendFileSftp(string hostname, int port, string username, string password, List<string> localFilesPath, string remotePath)
        {
            SftpClient client;
            try
            {
                client = new SftpClient(hostname, port, username, password);
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                return "Cannot create client";
            }

            try
            {
                using (client)
                {
                    client.Connect();
                    client.BufferSize = 4 * 1024;
                    logger.Info("After client connected");
                    foreach (var filePath in localFilesPath)
                    {
                        var fileStream = new FileStream(filePath, FileMode.Open);
                        {
                            var destination =
                                $"{remotePath}/{new FileInfo(filePath).Name}";
                            client.UploadFile(fileStream, destination, true, null);
                            logger.Info("Uploaded: {0}", destination);
                        }
                    }

                    client.Disconnect();
                    client.Dispose();
                    return "Uploaded";
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                return "Cannot upload";
            }
        }

        /// <summary>
        /// Checks the SSH connection until connected
        /// If more than 300 tries unsuccessful (enough for VM start up)
        /// return false
        /// </summary>
        /// <param name="hostname">The hostname.</param>
        /// <param name="port">The port.</param>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <returns>true when connected, false if not connected more than 300 times</returns>
        public static bool  WaitSsh(string hostname, int port, string username, string password)
        {
            int cnt = 0;
            using (var client = new SshClient(hostname, port, username, password))
            {
                while (true)
                {
                    try
                    {
                        client.Connect();//takes ~30 seconds
                        break;
                    }
                    catch(Exception)
                    {
                        cnt++;
                        if (cnt > 6) // 40*8 = 240 seconds 4 minutes
                             return false;
                        Thread.Sleep(10000);//check every 5 seconds
                    }
                }
                client.Disconnect();
                return true;
            }
        }
        #endregion

        #region UTILITIES: Create shortcut
        /// <summary>
        /// Creates the shortcuts for VirtualBox.
        /// </summary>
        /// <param name="binPath">The path to binary.</param>
        /// <param name="destination">The path to shortcut.</param>
        /// <param name="arguments">Application arguments.</param>
        /// <param name="iconPath">The shortcut icon path.</param>
        /// <param name="runAsAdmin">if set to <c>true</c> [run as admin].</param>
        public static void CreateShortcut(string binPath, string destination, 
                                         string arguments, string iconPath, 
                                         bool runAsAdmin)
        {
            var shell = new WshShell();
            try
            {
                IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(destination);
                shortcut.TargetPath = binPath;
                shortcut.Arguments = arguments;
                if (!iconPath.Equals(""))
                {
                    //shortcut.IconLocation = "cmd.exe, 0";
                    shortcut.IconLocation = $"{iconPath}, 0";
                }
                
                //shortcut.Description = string.Format("Launches clrenv for {0} {1} {2}", arch, flavor, extra);
                shortcut.Save();
                using (var fs = new FileStream(destination, FileMode.Open, FileAccess.ReadWrite))
                {
                    fs.Seek(21, SeekOrigin.Begin);
                    fs.WriteByte(0x22);
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "Shortcut", MessageBoxButtons.OK);
            }
        }

        /// <summary>
        /// Creates the shortcut at the specified path.
        /// </summary>
        /// <param name="binPath">The path to binary.</param>
        /// <param name="destination">The path to shortcut.</param>
        /// <param name="arguments">Application arguments.</param>
        /// <param name="runAsAdmin">if set to <c>true</c> if application need to [run as admin].</param>
        private void CreateShortcut_(string binPath, string destination, 
                                    string arguments, 
                                    bool runAsAdmin)
            //(string shortcutPathName, bool create)
        {
            try
                {
                    string shortcutTarget = binPath;//System.IO.Path.Combine(Application.StartupPath, appname + ".exe");
                    WshShell myShell = new WshShell();
                    WshShortcut myShortcut = (WshShortcut)myShell.CreateShortcut(destination);
                    myShortcut.TargetPath = shortcutTarget; //The exe file this shortcut executes when double clicked
                    myShortcut.IconLocation = shortcutTarget + ",0"; //Sets the icon of the shortcut to the exe`s icon
                    myShortcut.WorkingDirectory = Application.StartupPath; //The working directory for the exe
                    myShortcut.Arguments = ""; //The arguments used when executing the exe
                    myShortcut.Save(); //Creates the shortcut
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }

        /// <summary>
        /// Creates all shortcuts needed.
        /// </summary>
        public void createAppShortCuts()
        {
            logger.Info("Creating shortcuts");
            var binPath = Path.Combine(_arguments["appDir"], "bin\\tray", SubutaiTrayName);
            var destPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory), 
                "Subutai.lnk");
             
            //Desktop
            string iconPath = Path.Combine(_arguments["appDir"], SubutaiIconName);
            Deploy.CreateShortcut(
                binPath,
                destPath,
                "",
                iconPath,
                false);

            //StartMenu/Programs
            destPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu), 
                "Programs",
                "Subutai.lnk");
            Deploy.CreateShortcut(
                binPath,
                destPath,
                "",
                iconPath,
                false);
            
            //StartMenu/Startup
            destPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup),
                "Subutai.lnk");
            Deploy.CreateShortcut(
                binPath,
                destPath,
                "",
                iconPath,
                false);
            //Create App folder in Programs
            string folderpath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms), "Subutai");
            destPath = Path.Combine(folderpath, "Subutai.lnk");
            try
            {
                if (!Directory.Exists(folderpath))
                {
                    Directory.CreateDirectory(folderpath);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
            }
            Deploy.CreateShortcut(
                binPath,
                destPath,
                "",
                iconPath,
                false);
            destPath = Path.Combine(folderpath, "Uninstall.lnk");
            binPath = Path.Combine(FD.logDir(), SubutaiUninstallName);
            iconPath = Path.Combine(_arguments["appDir"], SubutaiUninstallIconName);
            Deploy.CreateShortcut(
                binPath,
                destPath,
                "",
                iconPath,
                false);

        }

        #endregion

        #region UTILITIES: Request Kurjun REST API

        /// <summary>
        /// Rests the API request.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <returns></returns>
        private string rest_api_request(string url)
        {
            var request = WebRequest.Create(url) as HttpWebRequest;

            //request.Accept = "application/json";
            request.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
            ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls12;
            request.KeepAlive = false;

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            var responseBody = new StreamReader(stream: response.GetResponseStream()).ReadToEnd();

            return responseBody;
        }
        #endregion

        #region UTILITIES: Calc MD5

        /// <summary>
        /// Calculates the MD5 sum.
        /// </summary>
        /// <param name="filepath">The filepath.</param>
        /// <param name="upperCase">if set to <c>true</c> if need to convert to [upper case].</param>
        /// <returns>MD5 sun in string format</returns>
        public static string Calc_md5(string filepath, bool upperCase)
        {
            using (var md5 = MD5.Create())
            {
                if (!File.Exists(filepath))
                    return "-1";
                using (var stream = File.OpenRead(filepath))
                {
                    var bytes = md5.ComputeHash(stream);

                    StringBuilder result = new StringBuilder(bytes.Length*2);

                    foreach (var t in bytes)
                        result.Append(t.ToString(upperCase ? "X2" : "x2"));
                    return result.ToString();
                }
         }
        }
        #endregion

        #region FORM HELPERS: show / hide marquee bar

        /// <summary>
        /// Shows the marquee - sets the Progress bar to state showing that process is running without percentage.
        /// </summary>
        public static void ShowMarquee()
        {
            Program.form1.Invoke((MethodInvoker)delegate
            {
                   SetIndeterminate(true);
            });
        }

        /// <summary>
        /// Hides the marquee - sets the Progress bar to state showing percentage.
        /// </summary>
        public static void HideMarquee()
        {
            Program.form1.Invoke((MethodInvoker)delegate
            {
                SetIndeterminate(false);
            });
        }

        /// <summary>
        /// Shows wich stage is performing now
        /// </summary>
        /// <param name="stageName">Name of the stage.</param>
        /// <param name="subStageName">Name of the sub stage.</param>
        public static void StageReporter(string stageName, string subStageName)
        {
            Program.form1.Invoke((MethodInvoker)delegate
            {
                if (stageName != "")
                {
                    Program.form1.label_Stage.Text = stageName;
                }
                if (subStageName != "")
                {
                    //Program.form1.progressPanel1.Description = subStageName;
                    Program.form1.label_SubStage.Text = subStageName;
                }
            });
        }

        /// <summary>
        /// Sets the ProgressBar's indeterminate state. Progress bar wil show that process runs without exact percentage
        /// </summary>
        /// <param name="isIndeterminate">if set to <c>true</c> [is indeterminate].</param>
        public static void SetIndeterminate(bool isIndeterminate)
        {
            if (Program.form1.prBar_.InvokeRequired)
            {
                Program.form1.prBar_.BeginInvoke(
                    new Action(() =>
                    {
                        if (isIndeterminate)
                        {
                            Program.form1.prBar_.Style = ProgressBarStyle.Marquee;
                        }
                        else
                        {
                            Program.form1.prBar_.Style = ProgressBarStyle.Blocks;
                        }
                    }
                ));
            }
            else
            {
                if (isIndeterminate)
                {
                    Program.form1.prBar_.Style = ProgressBarStyle.Marquee;
                }
                else
                {
                    Program.form1.prBar_.Style = ProgressBarStyle.Blocks;
                }
            }
        }

        /// <summary>
        /// Updates the progress.
        /// </summary>
        /// <param name="progress">The progress.</param>
        public static void UpdateProgress(int progress)
        {
            if (Program.form1.prBar_.InvokeRequired)
            {
                Program.form1.prBar_.BeginInvoke(
                    new Action(() =>
                    {
                        Program.form1.prBar_.Value = progress;
                    }
                ));
            }
            else
            {
                Program.form1.prBar_.Value = progress;
            }
        }

        #endregion
    }
}
