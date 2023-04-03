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

            if (Debug.isVerbose)
            {
                File.AppendAllText(Debug.debugFile, "INFO Starting EnvyUpdate, version " + System.Diagnostics.FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly().Location).FileVersion);
            }

            // Check if EnvyUpdate is already running
            if (Util.IsInstanceOpen("EnvyUpdate"))
            {
                Debug.LogToFile("FATAL Found another instance, terminating.");

                MessageBox.Show(Properties.Resources.instance_already_running);
                Environment.Exit(1);
            }

            // Delete installed legacy versions
            if (Directory.Exists(GlobalVars.appdata))
            {
                Debug.LogToFile("INFO Found old appdata installation, uninstalling.");
                UninstallAll();
            }

            GlobalVars.isMobile = Util.IsMobile();
            Debug.LogToFile("INFO Mobile: " + GlobalVars.isMobile);

            localDriv = Util.GetLocDriv();

            Debug.LogToFile("INFO Local driver version: " + localDriv);

            if (localDriv != null)
            {
                Debug.LogToFile("INFO Local driver version already known, updating info without reloading.");
                UpdateLocalVer(false);
            }
            else
            {
                if (arguments.Contains("/fake"))
                {
                    Debug.isFake = true;
                    Debug.LogToFile("WARN Faking GPU with debug info.");
                }
                else
                {
                    Debug.LogToFile("FATAL No supported GPU found, terminating.");
                    MessageBox.Show(Properties.Resources.no_compatible_gpu);
                    Environment.Exit(255);
                }
            }

            if (Debug.isVerbose)
                File.AppendAllText(Debug.debugFile, "INFO Detecting driver type.");

            if (Util.IsDCH())
                textblockLocalType.Text = "DCH";
            else if (Debug.isFake)
                textblockLocalType.Text = "DCH (Debug)";
            else
                textblockLocalType.Text = "Standard";

            Debug.LogToFile("INFO Done detecting driver type.");

            // Check for startup shortcut
            if (File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "EnvyUpdate.lnk")))
            {
                Debug.LogToFile("INFO Autostart is enabled.");
                chkAutostart.IsChecked = true;
                chkAutostart_Click(null, null); //Automatically recreate shortcut to account for moved EXE.
            }

            //Check if launched as miminized with arg
            if (arguments.Contains("/minimize"))
            {
                Debug.LogToFile("INFO Launching minimized.");
                WindowState = WindowState.Minimized;
                Hide();
            }

            DispatcherTimer Dt = new DispatcherTimer();
            Dt.Tick += new EventHandler(Dt_Tick);
            // Check for new updates every 5 hours.
            Dt.Interval = new TimeSpan(5, 0, 0);
            Dt.Start();
            Debug.LogToFile("INFO Started check timer.");

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
                Debug.LogToFile("INFO Started update file system watcher.");
            }

            Load();
        }

        private void Dt_Tick(object sender, EventArgs e)
        {
            Load();
        }

        private void buttonHelp_Click(object sender, RoutedEventArgs e)
        {
            Debug.LogToFile("INFO Showing info window.");
            InfoWindow infoWin = new InfoWindow();
            infoWin.ShowDialog();
        }

        private void Load()
        {
            if (Util.GetDTID() == 18)
            {
                Debug.LogToFile("INFO Found studio driver.");
                radioSD.IsChecked = true;
            }
            else
            {
                Debug.LogToFile("INFO Found standard driver.");
                radioGRD.IsChecked = true;
            }

            if (File.Exists(GlobalVars.exedirectory + "skip.envy"))
            {
                Debug.LogToFile("INFO Found version skip config.");
                skippedVer = File.ReadLines(GlobalVars.exedirectory + "skip.envy").First();
            }

            // This little bool check is necessary for debug mode on systems without an Nvidia GPU. 
            if (Debug.isFake)
            {
                localDriv = Debug.LocalDriv();
                textblockGPU.Text = localDriv;
                textblockGPUName.Text = Debug.GPUname();
            }

            try
            {
                Debug.LogToFile("INFO Trying to get GPU update URL.");
                gpuURL = Util.GetGpuUrl();
            }
            catch (ArgumentException)
            {
                Debug.LogToFile("WARN Could not get GPU update URL, trying again with standard driver.");
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
                    Debug.LogToFile("FATAL Invalid API response from Nvidia. Attempted API call: " + e.Message);
                    MessageBox.Show("ERROR: Invalid API response from Nvidia. Please file an issue on GitHub.\nAttempted API call:\n" + e.Message);
                    Environment.Exit(10);
                }
            }

            using (var c = new WebClient())
            {
                Debug.LogToFile("INFO Trying to get newest driver version.");
                string pContent = c.DownloadString(gpuURL);
                var pattern = @"Windows\/\d{3}\.\d{2}";
                Regex rgx = new Regex(pattern);
                var matches = rgx.Matches(pContent);
                onlineDriv = Regex.Replace(Convert.ToString(matches[0]), "Windows/", "");
                textblockOnline.Text = onlineDriv;
                Debug.LogToFile("INFO Got online driver version: " + onlineDriv);
            }

            try
            {
                if (float.Parse(localDriv) < float.Parse(onlineDriv))
                {
                    Debug.LogToFile("INFO Local version is older than online. Setting UI...");
                    textblockOnline.Foreground = Brushes.Red;
                    buttonDL.IsEnabled = true;
                    if (skippedVer == null)
                    {
                        buttonSkip.Content = Properties.Resources.ui_skipversion;
                        buttonSkip.IsEnabled = true;
                    }
                    else
                        buttonSkip.Content = Properties.Resources.ui_skipped;

                    Debug.LogToFile("INFO UI set.");

                    if (skippedVer != onlineDriv)
                    {
                        Debug.LogToFile("INFO Showing update popup notification.");
                        Notify.ShowDrivUpdatePopup();
                    }
                }
                else
                {
                    Debug.LogToFile("INFO Local version is up to date.");
                    buttonSkip.IsEnabled = false;
                    textblockOnline.Foreground = Brushes.Green;
                }
            }
            catch (FormatException)
            {
                Debug.LogToFile("INFO Caught FormatException, assuming locale workaround is necessary.");
                //Thank you locales. Some languages need , instead of . for proper parsing
                string cLocalDriv = localDriv.Replace('.', ',');
                string cOnlineDriv = onlineDriv.Replace('.', ',');
                if (float.Parse(cLocalDriv) < float.Parse(cOnlineDriv))
                {
                    Debug.LogToFile("INFO Local version is older than online. Setting UI...");
                    textblockOnline.Foreground = Brushes.Red;
                    buttonDL.IsEnabled = true;
                    if (skippedVer == null)
                        buttonSkip.IsEnabled = true;
                    else
                        buttonSkip.Content = Properties.Resources.ui_skipped;
                    if (skippedVer != onlineDriv)
                    {
                        Debug.LogToFile("INFO Showing update popup notification.");
                        Notify.ShowDrivUpdatePopup();
                    }
                }
                else
                {
                    Debug.LogToFile("INFO Local version is up to date.");
                    buttonSkip.IsEnabled = false;
                    textblockOnline.Foreground = Brushes.Green;
                }
            }

            //Check for different version than skipped version
            if (skippedVer != onlineDriv)
            {
                Debug.LogToFile("INFO Skipped version is surpassed, deleting setting.");
                skippedVer = null;
                if (File.Exists(GlobalVars.exedirectory + "skip.envy"))
                    File.Delete(GlobalVars.exedirectory + "skip.envy");
                buttonSkip.Content = Properties.Resources.ui_skipversion;
                buttonSkip.IsEnabled = true;
            }
        }

        private void buttonDL_Click(object sender, RoutedEventArgs e)
        {
            Debug.LogToFile("INFO Opening download page.");
            Process.Start(gpuURL);
        }

        private void TaskbarIcon_TrayLeftMouseDown(object sender, RoutedEventArgs e)
        {
            Debug.LogToFile("INFO Tray was clicked, opening main window.");
            Util.ShowMain();
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                Debug.LogToFile("INFO Window was minimized, closing to tray.");
                Hide();
            }
        }

        public void UninstallAll()
        {
            if (File.Exists(GlobalVars.startup + "\\EnvyUpdate.lnk"))
            {
                Debug.LogToFile("INFO Deleted startup entry.");
                File.Delete(GlobalVars.startup + "\\EnvyUpdate.lnk");
            }

            if (File.Exists(GlobalVars.startmenu + "\\EnvyUpdate.lnk"))
            {
                Debug.LogToFile("INFO Deleted start menu entry.");
                File.Delete(GlobalVars.startmenu + "\\EnvyUpdate.lnk");
            }
            if ((GlobalVars.exedirectory == GlobalVars.appdata) && File.Exists(GlobalVars.appdata + "EnvyUpdate.exe"))
            {
                Debug.LogToFile("INFO Deleting EnvyUpdate appdata and self.");
                MessageBox.Show(Properties.Resources.uninstall_legacy_message);
                Util.SelfDelete();
            }
            else if (Directory.Exists(GlobalVars.appdata))
            {
                Debug.LogToFile("INFO Deleting EnvyUpdate appdata folder");
                Directory.Delete(GlobalVars.appdata, true);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (MessageBox.Show(Properties.Resources.exit_confirm, "", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                Debug.LogToFile("INFO Uninstalling notifications and shutting down.");
                ToastNotificationManagerCompat.Uninstall(); // Uninstall notifications to prevent issues with the app being portable.
                Application.Current.Shutdown();
            }
            else
            {
                Debug.LogToFile("INFO Application shutdown was cancelled.");
                e.Cancel = true;
            }
        }

        private void radioGRD_Checked(object sender, RoutedEventArgs e)
        {
            if (File.Exists(GlobalVars.exedirectory + "sd.envy"))
            {
                Debug.LogToFile("INFO Switching to game ready driver.");
                File.Delete(GlobalVars.exedirectory + "sd.envy");
                Load();
            }
        }

        private void radioSD_Checked(object sender, RoutedEventArgs e)
        {
            if (!File.Exists(GlobalVars.exedirectory + "sd.envy"))
            {
                Debug.LogToFile("INFO Switching to studio driver.");
                File.Create(GlobalVars.exedirectory + "sd.envy").Close();
                Load();
            }
        }

        private void chkAutostart_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "EnvyUpdate.lnk")))
            {
                Debug.LogToFile("INFO Removing autostart entry.");
                File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "EnvyUpdate.lnk"));
            }
            if (chkAutostart.IsChecked == true)
            {
                Debug.LogToFile("INFO Creating autostart entry.");
                Util.CreateShortcut("EnvyUpdate", Environment.GetFolderPath(Environment.SpecialFolder.Startup), GlobalVars.exeloc, "NVidia Update Checker", "/minimize");
            }
        }

        private void buttonSkip_Click(object sender, RoutedEventArgs e)
        {
            Debug.LogToFile("INFO Skipping version.");
            skippedVer = onlineDriv;
            File.WriteAllText(GlobalVars.exedirectory + "skip.envy", onlineDriv);
            buttonSkip.IsEnabled = false;
            buttonSkip.Content = Properties.Resources.ui_skipped;
            MessageBox.Show(Properties.Resources.skip_confirm);
        }

        private void UpdateLocalVer(bool reloadLocalDriv = true)
        {
            Debug.LogToFile("INFO Updating local driver version in UI.");
            if (reloadLocalDriv)
            {
                Debug.LogToFile("INFO Reloading local driver version.");
                localDriv = Util.GetLocDriv();
            }
            textblockGPU.Text = localDriv;
            if (GlobalVars.isMobile)
                textblockGPUName.Text = Util.GetGPUName(false) + " (mobile)";
            else
                textblockGPUName.Text = Util.GetGPUName(false);
        }

        void DriverFileChanged(object sender, FileSystemEventArgs e)
        {
            Debug.LogToFile("INFO Watched driver file changed! Reloading data.");
            System.Threading.Thread.Sleep(10000);
            Application.Current.Dispatcher.Invoke(delegate
            {
                UpdateLocalVer();
                Load();
            });
        }
    }
}