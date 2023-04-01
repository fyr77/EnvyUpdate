using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace EnvyUpdate
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string localDriv = null;
        private string onlineDriv = null;
        private string gpuURL = null;
        private string[] arguments = null;
        private string skippedVer = null;

        public MainWindow()
        {
            InitializeComponent();

            // Try to get command line arguments
            try
            {
                arguments = Environment.GetCommandLineArgs();
            }
            catch (IndexOutOfRangeException)
            {
                // This is necessary, since .NET throws an exception if you check for a non-existant arg.
            }

            // Check if EnvyUpdate is already running
            if (Util.IsInstanceOpen("EnvyUpdate"))
            {
                MessageBox.Show(Properties.Resources.instance_already_running);
                Environment.Exit(1);
            }

            // Delete installed legacy versions
            if (Directory.Exists(GlobalVars.appdata))
                UninstallAll();

            GlobalVars.isMobile = Util.IsMobile();

            localDriv = Util.GetLocDriv();
            if (localDriv != null)
            {
                UpdateLocalVer(false);
            }
            else
            {
                if (arguments.Contains("/debug"))
                {
                    Debug.isDebug = true;
                }
                else
                {
                    MessageBox.Show(Properties.Resources.no_compatible_gpu);
                    Environment.Exit(255);
                }
            }

            if (Util.IsDCH())
                textblockLocalType.Text = "DCH";
            else if (Debug.isDebug)
                textblockLocalType.Text = "DCH (Debug)";
            else
                textblockLocalType.Text = "Standard";

            // Check for startup shortcut
            if (File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "EnvyUpdate.lnk")))
            {
                chkAutostart.IsChecked = true;
                chkAutostart_Click(null, null); //Automatically recreate shortcut to account for moved EXE.
            }

            //Check if launched as miminized with arg
            if (arguments.Contains("/minimize"))
            {
                WindowState = WindowState.Minimized;
                Hide();
            }

            DispatcherTimer Dt = new DispatcherTimer();
            Dt.Tick += new EventHandler(Dt_Tick);
            // Check for new updates every 5 hours.
            Dt.Interval = new TimeSpan(5, 0, 0);
            Dt.Start();

            string watchDirPath = Path.Combine(Environment.ExpandEnvironmentVariables("%ProgramW6432%"), "NVIDIA Corporation\\Installer2\\InstallerCore");
            if (Directory.Exists(watchDirPath))
            {
                GlobalVars.monitoringInstall = true;

                var driverFileChangedWatcher = new FileSystemWatcher(watchDirPath);
                driverFileChangedWatcher.NotifyFilter = NotifyFilters.Attributes
                                 | NotifyFilters.CreationTime
                                 | NotifyFilters.FileName
                                 | NotifyFilters.LastAccess
                                 | NotifyFilters.LastWrite
                                 | NotifyFilters.Size;
                driverFileChangedWatcher.Changed += DriverFileChanged;

                driverFileChangedWatcher.Filter = "*.dll";
                driverFileChangedWatcher.IncludeSubdirectories = false;
                driverFileChangedWatcher.EnableRaisingEvents = true;
            }

            Load();
        }

        private void Dt_Tick(object sender, EventArgs e)
        {
            Load();
        }

        private void buttonHelp_Click(object sender, RoutedEventArgs e)
        {
            InfoWindow infoWin = new InfoWindow();
            infoWin.ShowDialog();
        }

        private void Load()
        {
            if (Util.GetDTID() == 18)
                radioSD.IsChecked = true;
            else
                radioGRD.IsChecked = true;

            if (File.Exists(GlobalVars.exedirectory + "skip.envy"))
                skippedVer = File.ReadLines(GlobalVars.exedirectory + "skip.envy").First();

            // This little bool check is necessary for debug mode on systems without an Nvidia GPU. 
            if (Debug.isDebug)
            {
                localDriv = Debug.LocalDriv();
                textblockGPU.Text = localDriv;
                textblockGPUName.Text = Debug.GPUname();
            }

            try
            {
                gpuURL = Util.GetGpuUrl();
            }
            catch (ArgumentException)
            {
                try
                {
                    // disable SD and try with GRD
                    if (File.Exists(GlobalVars.exedirectory + "sd.envy"))
                    {
                        File.Delete(GlobalVars.exedirectory + "sd.envy");
                    }

                    gpuURL = Util.GetGpuUrl(); //try again with GRD
                    MessageBox.Show(Properties.Resources.ui_studionotsupported);
                    radioGRD.IsChecked = true;
                }
                catch (ArgumentException e)
                {
                    // Now we have a problem.
                    MessageBox.Show("ERROR: Invalid API response from Nvidia. Please file an issue on GitHub.\nAttempted API call:\n" + e.Message);
                    Environment.Exit(10);
                }
            }

            using (var c = new WebClient())
            {
                string pContent = c.DownloadString(gpuURL);
                var pattern = @"Windows\/\d{3}\.\d{2}";
                Regex rgx = new Regex(pattern);
                var matches = rgx.Matches(pContent);
                onlineDriv = Regex.Replace(Convert.ToString(matches[0]), "Windows/", "");
                textblockOnline.Text = onlineDriv;
            }

            try
            {
                if (float.Parse(localDriv) < float.Parse(onlineDriv))
                {
                    textblockOnline.Foreground = Brushes.Red;
                    buttonDL.IsEnabled = true;
                    if (skippedVer == null)
                    {
                        buttonSkip.Content = Properties.Resources.ui_skipversion;
                        buttonSkip.IsEnabled = true;
                    }
                    else
                        buttonSkip.Content = Properties.Resources.ui_skipped;
                    if (skippedVer != onlineDriv)
                        Notify.ShowDrivUpdatePopup();
                }
                else
                {
                    buttonSkip.IsEnabled = false;
                    textblockOnline.Foreground = Brushes.Green;
                }
            }
            catch (FormatException)
            {
                //Thank you locales. Some languages need , instead of . for proper parsing
                string cLocalDriv = localDriv.Replace('.', ',');
                string cOnlineDriv = onlineDriv.Replace('.', ',');
                if (float.Parse(cLocalDriv) < float.Parse(cOnlineDriv))
                {
                    textblockOnline.Foreground = Brushes.Red;
                    buttonDL.IsEnabled = true;
                    if (skippedVer == null)
                        buttonSkip.IsEnabled = true;
                    else
                        buttonSkip.Content = Properties.Resources.ui_skipped;
                    if (skippedVer != onlineDriv)
                        Notify.ShowDrivUpdatePopup();
                }
                else
                {
                    buttonSkip.IsEnabled = false;
                    textblockOnline.Foreground = Brushes.Green;
                }
            }

            //Check for different version than skipped version
            if (skippedVer != onlineDriv)
            {
                skippedVer = null;
                if (File.Exists(GlobalVars.exedirectory + "skip.envy"))
                    File.Delete(GlobalVars.exedirectory + "skip.envy");
                buttonSkip.Content = Properties.Resources.ui_skipversion;
                buttonSkip.IsEnabled = true;
            }
        }

        private void buttonDL_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(gpuURL);
        }

        private void TaskbarIcon_TrayLeftMouseDown(object sender, RoutedEventArgs e)
        {
            Util.ShowMain();
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                Hide();
            }
        }

        public void UninstallAll()
        {
            if (File.Exists(GlobalVars.startup + "\\EnvyUpdate.lnk"))
            {
                File.Delete(GlobalVars.startup + "\\EnvyUpdate.lnk");
            }

            if (File.Exists(GlobalVars.startmenu + "\\EnvyUpdate.lnk"))
            {
                File.Delete(GlobalVars.startmenu + "\\EnvyUpdate.lnk");
            }
            if ((GlobalVars.exedirectory == GlobalVars.appdata) && File.Exists(GlobalVars.appdata + "EnvyUpdate.exe"))
            {
                MessageBox.Show(Properties.Resources.uninstall_legacy_message);
                Util.SelfDelete();
            }
            else if (Directory.Exists(GlobalVars.appdata))
            {
                Directory.Delete(GlobalVars.appdata, true);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (MessageBox.Show(Properties.Resources.exit_confirm, "", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                ToastNotificationManagerCompat.Uninstall(); // Uninstall notifications to prevent issues with the app being portable.
                Application.Current.Shutdown();
            }
            else
                e.Cancel = true;
        }

        private void radioGRD_Checked(object sender, RoutedEventArgs e)
        {
            if (File.Exists(GlobalVars.exedirectory + "sd.envy"))
            {
                File.Delete(GlobalVars.exedirectory + "sd.envy");
                Load();
            }
        }

        private void radioSD_Checked(object sender, RoutedEventArgs e)
        {
            if (!File.Exists(GlobalVars.exedirectory + "sd.envy"))
            {
                File.Create(GlobalVars.exedirectory + "sd.envy").Close();
                Load();
            }
        }

        private void chkAutostart_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "EnvyUpdate.lnk")))
            {
                File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "EnvyUpdate.lnk"));
            }
            if (chkAutostart.IsChecked == true)
            {
                Util.CreateShortcut("EnvyUpdate", Environment.GetFolderPath(Environment.SpecialFolder.Startup), GlobalVars.exeloc, "NVidia Update Checker", "/minimize");
            }
        }

        private void buttonSkip_Click(object sender, RoutedEventArgs e)
        {
            skippedVer = onlineDriv;
            File.WriteAllText(GlobalVars.exedirectory + "skip.envy", onlineDriv);
            buttonSkip.IsEnabled = false;
            buttonSkip.Content = Properties.Resources.ui_skipped;
            MessageBox.Show(Properties.Resources.skip_confirm);
        }

        private void UpdateLocalVer(bool reloadLocalDriv = true)
        {
            if (reloadLocalDriv)
                localDriv = Util.GetLocDriv();
            textblockGPU.Text = localDriv;
            if (GlobalVars.isMobile)
                textblockGPUName.Text = Util.GetGPUName(false) + " (mobile)";
            else
                textblockGPUName.Text = Util.GetGPUName(false);
        }

        void DriverFileChanged(object sender, FileSystemEventArgs e)
        {
            System.Threading.Thread.Sleep(10000);
            Application.Current.Dispatcher.Invoke(delegate
            {
                UpdateLocalVer();
                Load();
            });
        }
    }
}