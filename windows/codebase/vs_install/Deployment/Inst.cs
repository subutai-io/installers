using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Win32;
using NLog;

namespace Deployment
{
    class Inst
    {
        private static NLog.Logger logger = LogManager.GetCurrentClassLogger();

        //Define if application is installed
        public static int app_installed(string appName)
        {
            string subkey = Path.Combine("SOFTWARE\\Wow6432Node", appName);
            string subkey86 = Path.Combine("SOFTWARE\\", appName);
            RegistryKey rk = Registry.LocalMachine.OpenSubKey(subkey);
            RegistryKey rk86 = Registry.LocalMachine.OpenSubKey(subkey86);
            if (rk == null && rk86 == null)
            {
                return 0;
            }
            return 1;
        }

        
        //Install TAP driver and utilities
        public static void inst_TAP(string instDir)
        {
            string res = "";
            Form1.StageReporter("", "TAP driver");
            if (app_installed("TAP-Windows") == 0)
            {
                res = Deploy.LaunchCommandLineApp($"{instDir}\\redist\\tap-driver.exe", "/S");
                logger.Info("TAP driver: {0}", res);
            } else
            {
                Form1.StageReporter("", "TAP driver already installed");
                logger.Info("TAP driver is already installed: {0}", res);
            }

            if (app_installed("TAP-Windows") == 1)
            {
                var pathTAPin = Path.Combine(instDir, "redist");
                var pathTAPout = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "TAP-Windows", "bin");
           
                try
                {
                    File.Copy(Path.Combine(pathTAPin, "addtap.bat"), Path.Combine(pathTAPout, "addtap.bat"), true);
                    logger.Info("Copying {0}\\addtap.bat to {1}\\addtap.bat", pathTAPin.ToString(), pathTAPout.ToString());
                }
                catch (Exception ex)
                {
                    logger.Error(ex.Message + " copying utility addtap");
                }
                try
                {
                    File.Copy(Path.Combine(pathTAPin, "deltapall.bat"), Path.Combine(pathTAPout, "deltapall.bat"), true);
                    logger.Info("Copying {0}\\deltapall.bat to {1}", pathTAPin.ToString(), pathTAPout.ToString());
                }
                catch (Exception ex)
                {
                    logger.Error(ex.Message + " copying utility deltapall");
                }
            }
         }

        //Install Chrome
        public static void inst_Chrome(string instDir)
        {
            string res = "";
            //HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Clients\StartMenuInternet\Google Chrome
            if (app_installed("Clients\\StartMenuInternet\\Google Chrome") == 0)
            {
                Form1.StageReporter("", "Chrome");
                res = Deploy.LaunchCommandLineApp("msiexec", $"/qn /i \"{instDir}\\redist\\chrome.msi\"");
                logger.Info("Chrome: {0}", res);
            }
            else
            {
                Form1.StageReporter("", "Google\\Chrome is already installed");
                logger.Info("Google\\Chrome is already installed");
            }
        }

        //Install E2E extension - create subkey in Registry
        public static void install_ext()
        {
            string keyPath = "SOFTWARE\\Wow6432Node\\Google\\Chrome\\Extensions";
            string extName = "kpmiofpmlciacjblommkcinncmneeoaa";
            RegistryKey kPath = Registry.LocalMachine.OpenSubKey(keyPath, true);
            if (kPath != null)
            {
                kPath.CreateSubKey(extName);
                kPath.Close();
                extName = Path.Combine(keyPath, extName);
                kPath = Registry.LocalMachine.OpenSubKey(extName, true);
                if (kPath != null)
                {
                    kPath.SetValue("update_url", "http://clients2.google.com/service/update2/crx", RegistryValueKind.String);
                    logger.Info("E2E plugin value added");
                    kPath.Close();
                }
            }
            else
            {
                kPath = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Wow6432Node\\Google\\Chrome", true);
                if (kPath != null)
                {
                    kPath.CreateSubKey("Extensions");
                    kPath.Close();
                    logger.Info("Extensions key added");
                    install_ext();
                }
            }
        }

        public static void inst_E2E()
        {
            logger.Info("E2E extension");
            if (app_installed("Google\\Chrome\\Extensions\\kpmiofpmlciacjblommkcinncmneeoaa") == 0)
            {
                Form1.StageReporter("", "Chrome E2E extension");
                install_ext();
            }
        }

        public static void inst_VBox(string instDir)
        {
            string res = "";
            if (app_installed("Oracle\\VirtualBox") == 0)
            {
                res = Deploy.LaunchCommandLineApp($"{instDir}\\redist\\virtualbox.exe", "--silent");
                Deploy.CreateShortcut(
                    $"{Environment.GetEnvironmentVariable("ProgramFiles")}\\Oracle\\VirtualBox\\VirtualBox.exe",
                    $"{Environment.GetEnvironmentVariable("Public")}\\Desktop\\Oracle VM VirtualBox.lnk",
                    "", true);
                Deploy.CreateShortcut(
                    $"{Environment.GetEnvironmentVariable("ProgramFiles")}\\Oracle\\VirtualBox\\VirtualBox.exe",
                    $"{Environment.GetEnvironmentVariable("Public")}\\Desktop\\Oracle VM VirtualBox.lnk",
                    "", true);
                Deploy.CreateShortcut(
                    $"{Environment.GetEnvironmentVariable("ProgramFiles")}\\Oracle\\VirtualBox\\VirtualBox.exe",
                    $"{Environment.GetEnvironmentVariable("ProgramData")}\\Microsoft\\Windows\\Start Menu\\Programs\\Oracle VM VirtualBox\\Oracle VM VirtualBox.lnk",
                    "", true);
                logger.Info("Virtual Box: {0} ", res);
            }
            else
            {
                Form1.StageReporter("", "Oracle\\VirtualBox is already installed");
                logger.Info("Oracle\\VirtualBox is already installed");
            }

            //Adding windows firewall rules for vboxheadless.exe and virtualbox.exe
            string VBoxDir = "";
            VBoxDir = Environment.GetEnvironmentVariable("VBOX_MSI_INSTALL_PATH");
            if (VBoxDir == "" || VBoxDir == null)
            {
                VBoxDir = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                VBoxDir = Path.Combine(VBoxDir, "Oracle", "VirtualBox");
            }
            string VBoxPath = Path.Combine(VBoxDir, "VBoxHeadless.exe");
            //VBoxPath = VBoxDir.ToLower();
            Net.set_fw_rules(VBoxPath.ToLower(), "vboxheadless", false);

            VBoxPath = Path.Combine(VBoxDir, "VirtualBox.exe");
            VBoxDir = VBoxDir.ToLower();
            Net.set_fw_rules(VBoxPath.ToLower(), "virtualbox", false);
        }
    }
}
