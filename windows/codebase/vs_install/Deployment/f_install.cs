using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Forms;
using System.Net;
using NLog;
using Deployment.items;
using Renci.SshNet;

namespace Deployment
{
    public partial class f_install : Form
    {
        //private readonly string[] _args = Environment.GetCommandLineArgs();
        public readonly Dictionary<string, string> _arguments = new Dictionary<string, string>();
        public readonly Deploy _deploy;
        public readonly Dictionary<string, KurjunFileInfo> PrerequisiteFilesInfo = new Dictionary<string, KurjunFileInfo>();
        //public static string[] rows;
        //public static string installation_type = "";

        //private readonly string _cloneName = $"subutai-{DateTime.Now.ToString("yyyyMMddhhmm")}";
        //private readonly PrivateKeyFile[] _privateKeys = new PrivateKeyFile[] { };
        private static CancellationTokenSource tokenSource = new CancellationTokenSource();

        private static NLog.Logger logger = LogManager.GetCurrentClassLogger();

        private static int stage_counter = 0;
        public int finished = 0;
        private string st = " finished";
        //public static string snapFile = "";


        private void ParseArguments(string _args)
        {
            //"params=deploy-redist,prepare-vbox,dev,prepare-rh,deploy-p2p network-installation=true kurjunUrl=https://cdn.subut.ai:8338 repo_descriptor=repomd5-dev ";
            //var args_eq = _args.Select(argument => argument.Split(new[] { "=" }, StringSplitOptions.None)).Where(splitted => splitted.Length == 2);
            string[] s_args = _args.Split(' '); ;
            //foreach (var splitted in _args.Select(argument => argument.Split(new[] { "=" }, StringSplitOptions.None)).Where(splitted => splitted.Length == 2))
            foreach (string s_splitted in s_args)
            {
                string[] splitted = s_splitted.Split('=');
                if (splitted.Length != 2)
                    continue;
                _arguments[splitted[0]] = splitted[1];
                logger.Info("Arguments:  {0} =  {1}", splitted[0], splitted[1]);
            }
        }

        public f_install(string args)
        {
            logger.Info("date = {0}", $"{ DateTime.Now.ToString("yyyyMMddhhmm")}");
            InitializeComponent();
            ParseArguments(args);
            _deploy = new Deploy(_arguments);
            timer1.Start();
        }

        private void f_install_Load(object sender, EventArgs e)
        {
            _deploy.SetEnvironmentVariables();
            FD.copy_uninstall();

            if (_arguments["network-installation"].ToLower() == "true")
            {
                // DOWNLOAD REPO
                Deploy.StageReporter("Downloading prerequisites", "");
                Deploy.HideMarquee();
                TC.download_repo();
            }
            //TC.deploy_p2p();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            Invoke((MethodInvoker)Refresh);
        }

        public string installDir()
        {
            string sTmp = _arguments["appDir"].ToString();
            return sTmp;
        }

        #region TASKS FACTORY

        public void TaskFactory(object sender, AsyncCompletedEventArgs e)
        {
            //var token = tokenSource.Token;
            object state = "";

            Task.Factory.StartNew(() =>
            {
                logger.Info("Starting task factory");
            })
               .ContinueWith((prevTask) =>  
               {

                   //Checking if files downloaded without errors      
                   logger.Info("Stage: {0} {1}", _arguments["network-installation"].ToLower(), "checkmd5");
                   if (_arguments["network-installation"].ToLower() == "true")
                   {
                       TC.check_md5();
                   }
                   stage_counter++;
                   logger.Info("Stage checkmd5: {0}", stage_counter);
               }, TaskContinuationOptions.OnlyOnRanToCompletion)

               .ContinueWith((prevTask) =>
               {
                   //Installing prerequisites
                   Exception ex = prevTask.Exception;
                   if (prevTask.IsFaulted)
                   {
                       // faulted with exception
                       while (ex is AggregateException && ex.InnerException != null)
                           ex = ex.InnerException;
                       logger.Error(ex.Message, "checkmd5 faulted");
                       finished = 3;
                       //Program.ShowError(ex.Message, "checkmd5 faulted");
                       //we can continue
                   }
                    
                   if (_arguments["network-installation"].ToLower() == "true")
                   {
                       Deploy.StageReporter(" ", " ");
                       TC.unzip_extracted();
                       logger.Info("Stage unzip: {0}", "unzip-extracted");
                   }
                   stage_counter++;
                   logger.Info("Stage unzip: {0}", stage_counter);
               }, TaskContinuationOptions.OnlyOnRanToCompletion)

               .ContinueWith((prevTask) =>
               {
                   //Unzipping .zip
                   Exception ex = prevTask.Exception;
                    if (prevTask.IsFaulted)
                    {
                       //unzipping faulted with exception
                       while (ex is AggregateException && ex.InnerException != null)
                       {
                           ex = ex.InnerException;
                           logger.Error(ex.Message, "unzipping faulted");
                       }
                       finished = 3;
                       Program.ShowError(ex.Message, "unzipping faulted");
                       throw new InvalidOperationException();
                    }

                    if (_arguments["params"].Contains("deploy-redist"))
                    {
                        TC.deploy_redist();
                        logger.Info("Stage deploy-redist: {0}", "deploy-redist");
                    }
                    stage_counter++;
                    logger.Info("Stage deploy redistributables: {0}", stage_counter);
                }, TaskContinuationOptions.OnlyOnRanToCompletion)

                .ContinueWith((prevTask) =>
                {
                    //Prepare vbox
                    Exception ex = prevTask.Exception;
                    if (prevTask.IsFaulted)
                    {
                        //deploy redist faulted with exception
                        while (ex is AggregateException && ex.InnerException != null)
                        {
                            ex = ex.InnerException;
                            logger.Error(ex.Message, "deploy redist faulted");
                        }
                        finished = 3;
                        Program.ShowError(ex.Message, "deploy redist faulted");
                        throw new InvalidOperationException();
                    }

                    if (_arguments["params"].Contains("prepare-vbox") && _arguments["peer"] != "client-only")
                    {
                        TC.prepare_vbox();
                        logger.Info("Stage prepare vbox: {0}", "prepare-vbox");
                    }
                    stage_counter++;
                    logger.Info("Stage prepate-vbox: {0}", stage_counter);
                }, TaskContinuationOptions.OnlyOnRanToCompletion)

                .ContinueWith((prevTask) =>
                {
                    Exception ex = prevTask.Exception;
                    if (prevTask.IsFaulted)
                    {
                        //prepare-vbox faulted with exception
                        while (ex is AggregateException && ex.InnerException != null)
                        {
                            ex = ex.InnerException;
                            logger.Error(ex.Message, "prepare vbox faulted");
                        }
                        finished = 3;
                        Program.ShowError(ex.Message, "prepare vbox faulted");
                        throw new InvalidOperationException();
                    }

                    if (_arguments["params"].Contains("prepare-rh") && _arguments["peer"] != "client-only")
                    {
                        TC.prepare_rh();
                        
                    }
                    stage_counter++;
                    logger.Info("Stage prepare-rh: {0}", stage_counter);
                }, TaskContinuationOptions.OnlyOnRanToCompletion)


                .ContinueWith((prevTask) =>
                {
                    Exception ex = prevTask.Exception;
                    if (prevTask.IsFaulted)
                    {
                        //prepare rh faulted with exception
                        while (ex is AggregateException && ex.InnerException != null)
                        {
                            ex = ex.InnerException;
                            logger.Error(ex.Message, "prepare rh faulted");
                        }
                        finished = 3;
                        Program.ShowError(ex.Message, "prepare rh faulted");
                        throw new InvalidOperationException();
                    }

                    if (_arguments["params"].Contains("deploy-p2p") && _arguments["peer"] != "rh-only")
                    {
                        TC.deploy_p2p();
                        logger.Info("Stage: {0}", "deploy-p2p");
                    }

                    stage_counter++;
                    logger.Info("Stage deploy-p2p: {0}", stage_counter);
                }, TaskContinuationOptions.OnlyOnRanToCompletion)

                .ContinueWith((prevTask) =>
                {
                    Exception ex = prevTask.Exception;
                    if (prevTask.IsFaulted)
                    {
                        //deploy p2p faulted with exception
                        while (ex is AggregateException && ex.InnerException != null)
                        {
                            ex = ex.InnerException;
                            logger.Error(ex.Message, "deploy p2p faulted");
                        }
                        finished = 3;
                        //Program.ShowError(ex.Message, "deploy p2p faulted");
                    }

                    _deploy.createAppShortCuts();
                    stage_counter++;
                    logger.Info("Stage create shortcuts: {0}", stage_counter);
                }, TaskContinuationOptions.OnlyOnRanToCompletion)

              .ContinueWith((prevTask) =>
               {
                   Exception ex = prevTask.Exception;
                   if (prevTask.IsFaulted)
                   {
                       //create shortcuts faulted with exception
                       while (ex is AggregateException && ex.InnerException != null)
                       {
                           ex = ex.InnerException;
                           logger.Error(ex.Message, "create shortcuts faulted");
                       }
                       finished = 3;
                       Program.ShowError(ex.Message, "Create shortcuts faulted");
                   }

                   logger.Info("stage_counter = {0}", stage_counter);
                   Program.form1.Invoke((MethodInvoker)delegate
                   {
                       logger.Info("form1.invoke");
                       Program.form1.Visible = false;
                   });

                   Program.form2.Invoke((MethodInvoker)delegate
                   {
                       //logger.Info("show finished = {0}", finished);
                       InstallationFinished form2 = new InstallationFinished("complete", _arguments["appDir"]);
                       logger.Info("will show form2 from task factory");
                       form2.Show();
                       //show_finished();
                   });
               }, TaskContinuationOptions.OnlyOnRanToCompletion)
               .ContinueWith((prevTask) =>
               {
                   logger.Info("finished = {0}", finished);
                   if (finished == 11 && st == "complete" && _arguments["peer"] != "rh-only") //|| finished == 11)
                       Deploy.LaunchCommandLineApp($"{_arguments["appDir"]}bin\\tray\\{Deploy.SubutaiTrayName}", "");
                   //Environment.Exit(0);
               });
        }
        #endregion

        private void show_finished()
        {
            switch (finished)
            {
                case 0:
                    st = "failed";
                    break;
                case 1:
                    st = "complete";
                    break;
                case 2:
                    st = "cancelled";
                    tokenSource.Cancel();
                    break;
                case 3:
                    st = "failed";
                    break;
            }

            logger.Info("show finished = {0}", finished);
            Program.form1.Visible = false;
            InstallationFinished form2 = new InstallationFinished(st, _arguments["appDir"]);
            if (finished != 11)//&& finished !=1)
            {
                if (finished == 1)
                {
                    finished = 11;
                    logger.Info("will show form2 from sub");
                    form2.Show();
                }
                else
                {
                    finished = 11;
                    form2.ShowDialog();
                    Application.Exit();
                }
            }
        }

        private void f_install_VisibleChanged(object sender, EventArgs e)
        {
            int Vis = -1;
            if (((f_install)sender).Visible == false)
            {
                Vis = 0;
            }
            else
            {
                Vis = 1;
            }

            logger.Info("Visible changed - check changes, visible = {0}, finished = {1}", Vis, finished);
            if (((f_install)sender).Visible == false && (finished == 0 || finished == 1))
            {
                logger.Info("Visible false");
                show_finished();
            }
        }
        private void f_install_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                logger.Info("Closed by user");
                switch (finished)
                {
                    case 0:
                        {
                            finished = 2;
                            logger.Info("FormClosing: Installation cancelled");
                            e.Cancel = false;
                            //tokenSource.Cancel();
                            show_finished();
                        }
                        break;
                    case 1:
                        logger.Info("FormClosing: Installation finished");
                        break;
                    case 2:
                        logger.Info("FormClosing: Installation cancelled");
                        //tokenSource.Cancel();
                        break;
                    case 3:
                        logger.Info("FormClosing: Installation error");
                        break;
                    case 11:
                        logger.Info("FormClosing: Installation finished");
                        break;
                }
            }
        }
    }
}
