using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Forms;
using NLog;
using Deployment.items;
//using Renci.SshNet;

namespace Deployment
{
    /// <summary>
    /// Form perform and reflects installation process
    /// </summary>
    /// <seealso cref="System.Windows.Forms.Form" />
    public partial class f_install : Form
    {
        /// <summary>
        /// The arguments string 
        /// </summary>
        public readonly Dictionary<string, string> _arguments = new Dictionary<string, string>();
        /// <summary>
        /// The Deploy instance
        /// </summary>
        public readonly Deploy _deploy;
        /// <summary>
        /// The prerequisite files information in dictionary
        /// </summary>
        public readonly Dictionary<string, KurjunFileInfo> PrerequisiteFilesInfo = new Dictionary<string, KurjunFileInfo>();
        //private readonly string _cloneName = $"subutai-{DateTime.Now.ToString("yyyyMMddhhmm")}";
        //private readonly PrivateKeyFile[] _privateKeys = new PrivateKeyFile[] { };
        private static CancellationTokenSource tokenSource = new CancellationTokenSource(); //token sourse to cancel all threads

        private static NLog.Logger logger = LogManager.GetCurrentClassLogger();

        private static int stage_counter = 0;
        /// <summary>
        /// The finished - shows the installation state for Form.Closed - 3 - failed, 1, 11 - success, 2 - cancelled
        /// </summary>
        public int finished = 0;
        private string st = " finished";

        /// <summary>
        /// public f_install(string args)
        /// Performs all installation steps
        /// </summary>
        /// <param name="args">Parameter string formed from command line arguments and confirmation form</param>
        
        public f_install(string args)
        {
            logger.Info("date = {0}", $"{ DateTime.Now.ToString("yyyyMMddhhmm")}");
            InitializeComponent();
            ParseArguments(args);
            _deploy = new Deploy(_arguments);
            timer1.Start();
        }

        /// <summary>
        /// private void f_install_Load(object sender, EventArgs e)
        /// On form load: Changes environment variables - %Path% and %Subutai%
        /// updates uninstall string (in Windows Registry)
        /// and starts downloading
        /// </summary>
        private void f_install_Load(object sender, EventArgs e)
        {
            _deploy.SetEnvironmentVariables();
            Inst.remove_repo_desc(_arguments["appDir"], _arguments["repo_descriptor"]);

            string strUninstall = "";
            if (!FD.copy_uninstall())
            {
                strUninstall = Path.Combine(_arguments["appDir"], "bin", "uninstall-clean.exe");
            }
            else
            {
                strUninstall = Path.Combine(FD.logDir(), "uninstall-clean.exe");
            };

            Inst.update_uninstallString(strUninstall);
            if (_arguments["network-installation"].ToLower() == "true")
            {
                // DOWNLOAD REPO
                Deploy.StageReporter("Downloading prerequisites", "");
                Deploy.HideMarquee();
                TC.download_repo();
            }
        }

        /// <summary>
        /// private void ParseArguments(string _args)
        /// Creates Dictionary to keep arguments
        /// Dictionary _arguments
        /// </summary>
        /// <param name="_args">Parameter string formed from command line arguments and confirmation form</param>
        private void ParseArguments(string _args)
        {
            //"params=deploy-redist,prepare-vbox,dev,prepare-rh,deploy-p2p network-installation=true kurjunUrl=https://cdn.subut.ai:8338 repo_descriptor=repomd5-dev ";
            string[] s_args = _args.Split(' '); ;
            foreach (string s_splitted in s_args)
            {
                string[] splitted = s_splitted.Split('=');
                if (splitted.Length != 2)
                    continue;
                _arguments[splitted[0]] = splitted[1];
                logger.Info("Arguments:  {0} =  {1}", splitted[0], splitted[1]);
            }
        }

        /// <summary>
        /// private void timer1_Tick(object sender, EventArgs e)
        /// Timer tick
        /// </summary>
        private void timer1_Tick(object sender, EventArgs e)
        {
            Invoke((MethodInvoker)Refresh);
        }

        /// <summary>
        /// public string installDir()
        /// Define installation directory
        /// </summary>
        public string installDir()
        {
            string sTmp = _arguments["appDir"].ToString();
            return sTmp;
        }

        #region TASKS FACTORY
        /// <summary>
        /// Task factory to perform all installation steps 
        /// with TaskContinuationOptions.OnlyOnRanToCompletion for each step 
        /// so tasks are chained. On complete (failed or successful) runs InstallationFinished form
        /// </summary>
        /// <param name="sender">object sender</param>
        /// <param name="e">AsyncCompletedEventArgs e</param>
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
                   //Unzipping .zip
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
                       TC.unzip_extracted();
                       logger.Info("Stage unzip: {0}", "unzip-extracted");
                   }
                   stage_counter++;
                   logger.Info("Stage unzip: {0}", stage_counter);
               }, TaskContinuationOptions.OnlyOnRanToCompletion)

               .ContinueWith((prevTask) =>
               {
                   //Installing prerequisites
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
                    //Prepare RH
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
                    //Install and configure P2P
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
                        if (Inst.imported)
                        {
                            TC.deploy_p2p();
                            logger.Info("Stage: {0}", "deploy-p2p");
                        } else
                        {
                            finished = 3;
                            Program.ShowError("Import was not completed, please check network and VM state and reinstall", "prepare rh faulted");
                        }
                    }

                    stage_counter++;
                    logger.Info("Stage deploy-p2p: {0}", stage_counter);
                }, TaskContinuationOptions.OnlyOnRanToCompletion)

                .ContinueWith((prevTask) =>
                {
                    //Create shortcuts
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

                   //Program.form2.Invoke((MethodInvoker)delegate
                   //{
                       //logger.Info("show finished = {0}", finished);
                       InstallationFinished form2 = new InstallationFinished("complete", _arguments["appDir"]);
                       logger.Info("will show form2 from task factory");
                       form2.Show();
                   //});
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

        /// <summary>
        /// private void show_finished()
        /// Calls Finished form with parameter defined by "finished" value to show
        /// Finished form with proper message
        /// </summary>
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

        /// <summary>
        /// private void f_install_VisibleChanged(object sender, EventArgs e)
        /// For this form if visible == false calls show_finished()
        /// </summary>
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

        /// <summary>
        /// f_install_FormClosing(object sender, FormClosingEventArgs e)
        /// On Close defines reason of closing and logs message to log
        /// </summary>
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
