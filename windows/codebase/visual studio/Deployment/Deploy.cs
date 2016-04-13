﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using Deployment.items;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using IWshRuntimeLibrary;
using MonoTorrent.Client;
using MonoTorrent.Client.Encryption;
using MonoTorrent.Common;
using Renci.SshNet;
using File = System.IO.File;

namespace Deployment
{
    public class Deploy
    {
        private const string RestFileinfoURL = "/kurjun/rest/file/info?name=";
        private const string RestFileURL = "/kurjun/rest/file/get?id=";

        private readonly Dictionary<string, string> _arguments;

        public Deploy(Dictionary<string, string> arguments)
        {
            this._arguments = arguments;
        }

        public void SetEnvironmentVariables()
        {
            Environment.SetEnvironmentVariable("PATH",
                string.Format("{0};{1}\\Program Files\\Oracle\\VirtualBox;{2}\\bin", 
          Environment.GetEnvironmentVariable("PATH"), 
          Environment.GetEnvironmentVariable("SystemDrive"),
          _arguments["appDir"]));
        }

        #region HELPERS: Download
        public void DownloadFile(string url, string destination, AsyncCompletedEventHandler onComplete, string report, bool async, bool kurjun)
        {
            var md5 = "";

            if (kurjun)
            {
                var filename = Path.GetFileName(destination);
                var info = request_kurjun_fileInfo(url, RestFileinfoURL, filename);
                url = url + RestFileURL + info.id;

                md5 = info.id.Split(new[] {"."}, StringSplitOptions.None)[1];

                if (!Program.form1.PrerequisiteFilesInfo.ContainsKey(destination))
                    Program.form1.PrerequisiteFilesInfo.Add(destination, info);
            }

            var shouldWeDownload = false;

            var fileInfo = new FileInfo(destination);
            if (fileInfo.Exists)
            {
                var calculatedMd5 = Calc_md5(destination, false);

                if (calculatedMd5 != md5) { shouldWeDownload = true; }
            }
            else
            {
                shouldWeDownload = true;
            }

            if (shouldWeDownload)
            {

                var dirInfo = new DirectoryInfo(path: Path.GetDirectoryName(destination));
                if (!dirInfo.Exists)
                {
                    dirInfo.Create();
                }

                Program.form1.StageReporter("", report);

                var webClient = new WebClient();

                if (onComplete != null)
                {
                    webClient.DownloadFileCompleted += onComplete;
                }
                webClient.DownloadProgressChanged += ProgressChanged;
                try
                {
                    if (async)
                        webClient.DownloadFileAsync(new Uri(url), destination);
                    else
                        webClient.DownloadFile(new Uri(url), destination);
                }
                catch (Exception)
                {
                    Program.ShowError("Subutai repository is not available for some reason. Please try again later.",
                        "Repository Error");
                }
            }
            else
            {
              if (onComplete != null) onComplete.Invoke(null, null);
            }
        }

        private void ProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            //MessageBox.Show(e.ProgressPercentage.ToString());
            Program.form1.Invoke((MethodInvoker) delegate
            {
                Program.form1.progressBarControl1.EditValue =
                    e.ProgressPercentage;
            });
        }
        #endregion

        #region HELPERS: Download file via P2P

        public void DownloadViaP2P(string torrentFilePath, string destinationPath)
        {
            EngineSettings settings = new EngineSettings();
            settings.AllowedEncryption = EncryptionTypes.All;
            settings.SavePath = destinationPath;

            if (!Directory.Exists(settings.SavePath))
                Directory.CreateDirectory(settings.SavePath);

            var engine = new ClientEngine(settings);

            engine.ChangeListenEndpoint(new IPEndPoint(IPAddress.Any, 6969));

            Torrent torrent = Torrent.Load(torrentFilePath);

            TorrentManager manager = new TorrentManager(torrent, engine.Settings.SavePath, new TorrentSettings());

            engine.Register(manager);

            manager.Start();
        }
        #endregion

        #region HELPERS: Unzip files

        public void unzip_files(string folderPath)
        {
            var filenames = Directory.GetFiles(folderPath, "*.zip", SearchOption.AllDirectories).Select(Path.GetFullPath).ToArray();
            foreach (var filename in filenames)
            {
                var fileinfo = new FileInfo(filename);
                unzip_file(filename, fileinfo.DirectoryName, true);
            }

        }

        public void unzip_file(string source, string dest, bool remove)
        {
            Program.form1.progressPanel1.Parent.Invoke((MethodInvoker) delegate
            {
                Program.form1.progressPanel1.Description = "Extracting: " + new FileInfo(source).Name;
            });

            ZipFile zf = null;
            try
            {
                long maxProcessed = 0;

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

                                Program.form1.progressBarControl1.Parent.Invoke((MethodInvoker)delegate
                                {
                                    Program.form1.progressBarControl1.EditValue = percentage;
                                });
                            });
                        StreamUtils.Copy(zipStream, streamWriter, buffer, progressHandler, new TimeSpan(), Program.form1, "none", 100);
                    }
                }

                //new FileInfo(source).Delete();
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

        private KurjunFileInfo request_kurjun_fileInfo(string url, string restURL, string filename)
        {
            var json = rest_api_request(url + restURL + filename);

            return new System.Web.Script.Serialization.JavaScriptSerializer().Deserialize<KurjunFileInfo>(json);
        }
        #endregion


        #region UTILITIES: Launch commandline application
        public static void LaunchCommandLineApp(string filename, string arguments)
        {
            // Use ProcessStartInfo class
            var startInfo = new ProcessStartInfo
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                FileName = filename,
                WindowStyle = ProcessWindowStyle.Hidden,
                Arguments = arguments
            };

            try
            {
                // Start the process with the info we specified.
                // Call WaitForExit and then the using statement will close.
                using (var exeProcess = Process.Start(startInfo))
                {
                  if (exeProcess != null) exeProcess.WaitForExit();
                }
            }
            catch (Exception)
            {
                Thread.Sleep(2000);
                LaunchCommandLineApp(filename, arguments);
            }
        }
        #endregion

        #region UTILITIES: Send SSH command

        public static void SendSshCommand(string hostname, int port, string username, string password, string command)
        {
            using (var client = new SshClient(hostname, port, username, password))
            {
                client.Connect();
                client.RunCommand(command);
                client.Disconnect();
                client.Dispose();
            }
        }

        public static void SendSshCommand(string hostname, int port, string username, PrivateKeyFile[] keys, string command)
        {
            using (var client = new SshClient(hostname, port, username, keys))
            {
                client.Connect();
                client.RunCommand(command);
                client.Disconnect();
                client.Dispose();
            }
        }

        public static void SendFileSftp(string hostname, int port, string username, string password, List<string> localFilesPath, string remotePath)
        {
            using (var client = new SftpClient(hostname, port, username, password))
            {
                client.Connect();
                client.BufferSize = 4 * 1024;

                foreach (var filePath in localFilesPath)
                {
                    var fileStream = new FileStream(filePath, FileMode.Open);
                    {
                        var destination =
                            string.Format("{}/{1}", remotePath, new FileInfo(filePath).Name);
                        client.UploadFile(fileStream, destination, true, null);
                    }
                }

                client.Disconnect();
                client.Dispose();
            }
        }

        public static void WaitSsh(string hostname, int port, string username, string password)
        {
            using (var client = new SshClient(hostname, port, username, password))
            {
                while (true)
                {
                    try
                    {
                        client.Connect();
                        break;
                    }
                    catch(Exception)
                    {
                        Thread.Sleep(2000);
                    }
                }
                client.Disconnect();
            }
        }
        #endregion

        #region UTILITIES: Create shortcut

        public static void CreateShortcut(string binPath, string destination, string arguments, bool runAsAdmin)
        {
            //string shortcutPath = Path.Combine(shortCutFolder, string.Format("{0} {1}{2}.lnk", arch, flavor, extra));
            //string arguments = string.Format("{0} {1} {2} {3}{4} {5}", "/k", clrEnvPath, arch, flavor, extra, precmd);

            var shell = new WshShell();


            IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(destination);

            shortcut.TargetPath = binPath;
            shortcut.Arguments = arguments;
            //shortcut.IconLocation = "cmd.exe, 0";
            //shortcut.Description = string.Format("Launches clrenv for {0} {1} {2}", arch, flavor, extra);
            shortcut.Save();

            using (var fs = new FileStream(destination, FileMode.Open, FileAccess.ReadWrite))
            {
                fs.Seek(21, SeekOrigin.Begin);
                fs.WriteByte(0x22);
            }

        }
        #endregion

        #region UTILITIES: Request Kurjun REST API

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

        public static string Calc_md5(string filepath, bool upperCase)
        {
            using (var md5 = MD5.Create())
            {
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

        public static void ShowMarquee()
        {
            Program.form1.Invoke((MethodInvoker)delegate
            {
                Program.form1.marqueeProgressBarControl1.Visible = true;
            });
        }

        public static void HideMarquee()
        {
            Program.form1.Invoke((MethodInvoker)delegate
            {
                Program.form1.marqueeProgressBarControl1.Visible = false;
            });
        }
        #endregion
    }
}
