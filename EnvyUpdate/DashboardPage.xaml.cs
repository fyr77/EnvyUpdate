using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using MessageBox = System.Windows.MessageBox;

namespace EnvyUpdate
{
    /// <summary>
    /// Interaction logic for Dashboard.xaml
    /// </summary>
    public partial class DashboardPage
    {
        private string localDriv = null;
        private string onlineDriv = null;
        private string gpuURL = null;
        private string skippedVer = null;
        private DateTime lastFileChanged = DateTime.MinValue;

        public DashboardPage()
        {
            InitializeComponent();

            if (GlobalVars.startMinimized)
                Application.Current.MainWindow.Hide(); // Hide only AFTER initializing dashboard page, otherwise tray icon doesn't work

            if (Debug.isFake)
                localDriv = Debug.LocalDriv();
            else
                localDriv = Util.GetLocDriv();

            Debug.LogToFile("INFO Local driver version: " + localDriv);

            if (localDriv != null)
            {
                Debug.LogToFile("INFO Local driver version already known, updating info without reloading.");
                UpdateLocalVer(false);
            }

            Debug.LogToFile("INFO Detecting driver type.");

            if (Debug.isFake)
                textblockLocalType.Text = "DCH (Debug)";
            else if (Util.IsDCH())
                textblockLocalType.Text = "DCH";
            else
                textblockLocalType.Text = "Standard";

            Debug.LogToFile("INFO Done detecting driver type: " + textblockLocalType.Text);

            // Check for startup shortcut
            if (File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "EnvyUpdate.lnk")))
            {
                Debug.LogToFile("INFO Autostart is enabled.");
                switchAutostart.IsChecked = true;
                switchAutostart_Click(null, null); //Automatically recreate shortcut to account for moved EXE.
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
            else
                Debug.LogToFile("WARN Could not start update file system watcher. Path not found: " + watchDirPath);

            Load();
        }

        private void Dt_Tick(object sender, EventArgs e)
        {
            Load();
        }

        private void Load()
        {
            if (Util.GetDTID() == 18)
            {
                Debug.LogToFile("INFO Found studio driver.");
                switchStudioDriver.IsChecked = true;
            }
            else
            {
                Debug.LogToFile("INFO Found standard driver.");
                switchStudioDriver.IsChecked = false;
            }

            if (File.Exists(Path.Combine(GlobalVars.saveDirectory,"skip.envy")))
            {
                Debug.LogToFile("INFO Found version skip config.");
                skippedVer = File.ReadLines(Path.Combine(GlobalVars.saveDirectory, "skip.envy")).First();
            }

            // This little bool check is necessary for debug fake mode. 
            if (Debug.isFake)
            {
                localDriv = Debug.LocalDriv();
                cardLocal.Header = localDriv;
                textblockGPUName.Text = Debug.GPUname();
            }

            try
            {
                Debug.LogToFile("INFO Trying to get GPU update URL.");
                gpuURL = Util.GetGpuUrl();
            }
            catch (ArgumentException)
            {
                Debug.LogToFile("WARN Could not get GPU update URL, trying again with non-studio driver.");
                try
                {
                    // disable SD and try with GRD
                    if (File.Exists(Path.Combine(GlobalVars.saveDirectory, "sd.envy")))
                    {
                        File.Delete(Path.Combine(GlobalVars.saveDirectory, "sd.envy"));
                    }

                    gpuURL = Util.GetGpuUrl(); //try again with GRD
                    MessageBox.Show(Properties.Resources.ui_studionotsupported);
                    switchStudioDriver.IsChecked = false;
                }
                catch (ArgumentNullException)
                {
                    MessageBox.Show("ERROR: Could not get list of GPU models from Nvidia, please check your network connection.\nOtherwise, please report this issue on GitHub.");
                    Environment.Exit(11);
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
                cardOnline.Header = onlineDriv;
                Debug.LogToFile("INFO Got online driver version: " + onlineDriv);
            }

            string correctLocalDriv;
            string correctOnlineDriv;

            try
            {
                float.Parse(onlineDriv);
                correctLocalDriv = localDriv;
                correctOnlineDriv = onlineDriv;
            }
            catch (FormatException)
            {
                Debug.LogToFile("INFO Caught FormatException, assuming locale workaround is necessary.");
                //Thank you locales. Some languages need , instead of . for proper parsing
                correctLocalDriv = localDriv.Replace('.', ',');
                correctOnlineDriv = onlineDriv.Replace('.', ',');
            }

            if (float.Parse(correctLocalDriv) < float.Parse(correctOnlineDriv))
            {
                Debug.LogToFile("INFO Local version is older than online. Setting UI...");
                SetInfoBar(false);
                buttonDownload.Visibility = Visibility.Visible;
                buttonSkipVersion.Visibility = Visibility.Visible;
                if (skippedVer == null)
                {
                    buttonSkipVersion.ToolTip = Properties.Resources.ui_skipversion;
                    buttonSkipVersion.IsEnabled = true;
                }
                else
                {
                    buttonSkipVersion.IsEnabled = false;
                    buttonSkipVersion.ToolTip = Properties.Resources.ui_skipped;
                }

                Debug.LogToFile("INFO UI set.");

                if (skippedVer != onlineDriv)
                {
                    if (GlobalVars.autoDownload)
                    {
                        if (buttonDownload.IsVisible)
                        {
                            Debug.LogToFile("INFO Auto-Downloading driver.");
                            buttonDownload_Click(null, null);
                        }
                    }

                    Debug.LogToFile("INFO Showing update popup notification.");
                    Notify.ShowDrivUpdatePopup();
                }
            }
            else
            {
                Debug.LogToFile("INFO Local version is up to date.");
                buttonSkipVersion.Visibility = Visibility.Collapsed;
                SetInfoBar(true);
            }

            //Check for different version than skipped version
            if (skippedVer != null && skippedVer != onlineDriv)
            {
                Debug.LogToFile("INFO Skipped version is surpassed, deleting setting.");
                skippedVer = null;
                if (File.Exists(Path.Combine(GlobalVars.saveDirectory, "skip.envy")))
                    File.Delete(Path.Combine(GlobalVars.saveDirectory, "skip.envy"));
                buttonSkipVersion.ToolTip = Properties.Resources.ui_skipversion;
                buttonSkipVersion.IsEnabled = true;
                buttonSkipVersion.Visibility = Visibility.Visible;
            }

            // Check if update file already exists and display install button instead
            if (File.Exists(Path.Combine(GlobalVars.saveDirectory, onlineDriv + "-nvidia-installer.exe")))
            {
                Debug.LogToFile("INFO Found downloaded driver installer, no need to redownload.");
                buttonDownload.Visibility = Visibility.Collapsed;
                buttonInstall.Visibility = Visibility.Visible;
            }
        }

        private void switchStudioDriver_Unchecked(object sender, RoutedEventArgs e)
        {
            if (File.Exists(Path.Combine(GlobalVars.saveDirectory, "sd.envy")))
            {
                Debug.LogToFile("INFO Switching to game ready driver.");
                File.Delete(Path.Combine(GlobalVars.saveDirectory, "sd.envy"));
                Load();
            }
        }

        private void switchStudioDriver_Checked(object sender, RoutedEventArgs e)
        {
            if (!File.Exists(Path.Combine(GlobalVars.saveDirectory, "sd.envy")))
            {
                Debug.LogToFile("INFO Switching to studio driver.");
                File.Create(Path.Combine(GlobalVars.saveDirectory, "sd.envy")).Close();
                Load();
            }
        }

        private void switchAutostart_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "EnvyUpdate.lnk")))
            {
                Debug.LogToFile("INFO Removing autostart entry.");
                File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "EnvyUpdate.lnk"));
            }
            if (switchAutostart.IsChecked == true)
            {
                Debug.LogToFile("INFO Creating autostart entry.");
                Util.CreateShortcut("EnvyUpdate", Environment.GetFolderPath(Environment.SpecialFolder.Startup), GlobalVars.pathToAppExe, "NVidia Update Checker", "/minimize");
            }
        }

        private void buttonSkipVersion_Click(object sender, RoutedEventArgs e)
        {
            Debug.LogToFile("INFO Skipping version.");
            skippedVer = onlineDriv;
            File.WriteAllText(Path.Combine(GlobalVars.saveDirectory, "skip.envy"), onlineDriv);
            buttonSkipVersion.IsEnabled = false;
            buttonSkipVersion.ToolTip = Properties.Resources.ui_skipped;
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
            cardLocal.Header = localDriv;
            if (GlobalVars.isMobile)
                textblockGPUName.Text = Util.GetGPUName(false) + " (mobile)";
            else
                textblockGPUName.Text = Util.GetGPUName(false);
        }

        void DriverFileChanged(object sender, FileSystemEventArgs e)
        {
            if (!GlobalVars.isInstalling && (DateTime.UtcNow.Subtract(lastFileChanged).TotalMinutes > 1))
            {
                Debug.LogToFile("INFO Watched driver file changed! Reloading data.");
                System.Threading.Thread.Sleep(10000);
                lastFileChanged = DateTime.UtcNow;
                Application.Current.Dispatcher.Invoke(delegate
                {
                    UpdateLocalVer();
                    Load();
                });
            }
        }

        private void CardOnline_Click(object sender, RoutedEventArgs e)
        {
            Debug.LogToFile("INFO Opening download page.");
            Process.Start(gpuURL);
        }

        private void SetInfoBar (bool good)
        {
            if (good)
            {
                infoBarStatus.Severity = Wpf.Ui.Controls.InfoBarSeverity.Success;
                infoBarStatus.Title = Properties.Resources.ui_info_uptodate;
                infoBarStatus.Message = Properties.Resources.ui_message_good;
            }
            else
            {
                infoBarStatus.Severity = Wpf.Ui.Controls.InfoBarSeverity.Warning;
                infoBarStatus.Title = Properties.Resources.ui_info_outdated;
                infoBarStatus.Message = Properties.Resources.ui_message_update;
            }
        }

        private void buttonDownload_Click(object sender, RoutedEventArgs e)
        {
            if (GlobalVars.isDownloading)
            {
                Debug.LogToFile("WARN A download is already running.");
                ShowSnackbar(Wpf.Ui.Common.ControlAppearance.Danger, Wpf.Ui.Common.SymbolRegular.ErrorCircle24, Properties.Resources.info_download_running, Properties.Resources.info_download_running_title);
            }
            else
            {
                progressbarDownload.Visibility = Visibility.Visible;
                buttonDownload.IsEnabled = false;
                GlobalVars.isDownloading = true;

                if (File.Exists(Path.Combine(GlobalVars.saveDirectory, onlineDriv + "-nvidia-installer.exe.downloading")))
                {
                    Debug.LogToFile("WARN Found previous unfinished download, retrying.");
                    File.Delete(Path.Combine(GlobalVars.saveDirectory, onlineDriv + "-nvidia-installer.exe.downloading"));
                }
                Thread thread = new Thread(() => {
                    using (WebClient client = new WebClient())
                    {
                        client.Headers["User-Agent"] = GlobalVars.useragent;
                        client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(client_DownloadProgressChanged);
                        client.DownloadFileCompleted += new AsyncCompletedEventHandler(client_DownloadFileCompleted);
                        client.DownloadFileAsync(new Uri(Util.GetDirectDownload(gpuURL)), Path.Combine(GlobalVars.saveDirectory, onlineDriv + "-nvidia-installer.exe.downloading"));
                    }
                });
                thread.Start();
                Debug.LogToFile("INFO Started installer download.");
            }
        }

        void client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            double bytesIn = double.Parse(e.BytesReceived.ToString());
            double totalBytes = double.Parse(e.TotalBytesToReceive.ToString());
            double percentage = bytesIn / totalBytes * 100;
            Application.Current.Dispatcher.Invoke(new Action(() => {
                progressbarDownload.Value = int.Parse(Math.Truncate(percentage).ToString());
            }));
        }

        void client_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(new Action(() => {
                buttonDownload.IsEnabled = true;
                progressbarDownload.Visibility = Visibility.Collapsed;
                GlobalVars.isDownloading = false;
            }));
            if (e.Error == null)
            {
                Application.Current.Dispatcher.Invoke(new Action(() => {
                    ShowSnackbar(Wpf.Ui.Common.ControlAppearance.Success, Wpf.Ui.Common.SymbolRegular.CheckmarkCircle24, Properties.Resources.info_download_success, Properties.Resources.info_download_success_title);
                    buttonDownload.Visibility = Visibility.Collapsed;
                    buttonInstall.Visibility = Visibility.Visible;
                    Debug.LogToFile("INFO Download successful.");
                }));
                if (File.Exists(Path.Combine(GlobalVars.saveDirectory, onlineDriv + "-nvidia-installer.exe")))
                    File.Delete(Path.Combine(GlobalVars.saveDirectory, onlineDriv + "-nvidia-installer.exe"));
                File.Move(Path.Combine(GlobalVars.saveDirectory, onlineDriv + "-nvidia-installer.exe.downloading"), Path.Combine(GlobalVars.saveDirectory, onlineDriv + "-nvidia-installer.exe"));
            }
            else
            {
                File.Delete(Path.Combine(GlobalVars.saveDirectory, onlineDriv + "-nvidia-installer.exe.downloading"));
                Application.Current.Dispatcher.Invoke(new Action(() => {
                    ShowSnackbar(Wpf.Ui.Common.ControlAppearance.Danger, Wpf.Ui.Common.SymbolRegular.ErrorCircle24, Properties.Resources.info_download_error, Properties.Resources.info_download_error_title);
                    Debug.LogToFile("INFO Download NOT successful. Error: " + e.Error.ToString());
                }));
            }
        }
        private void buttonInstall_Click(object sender, RoutedEventArgs e)
        {
            buttonInstall.IsEnabled = false;
            GlobalVars.isInstalling = true;
            string sevenZipPath = Util.GetSevenZip();

            ShowSnackbar(Wpf.Ui.Common.ControlAppearance.Info, Wpf.Ui.Common.SymbolRegular.FolderZip24, Properties.Resources.info_extracting, Properties.Resources.info_extracting_title);

            string filePath = Path.Combine(GlobalVars.saveDirectory, onlineDriv + "-nvidia-installer.exe");
            string destinationDir = Path.Combine(GlobalVars.saveDirectory, onlineDriv + "-extracted");

            if (!Directory.Exists(destinationDir))
                Directory.CreateDirectory(destinationDir);

            Debug.LogToFile("INFO Starting extraction of driver files.");

            Process process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                WindowStyle = ProcessWindowStyle.Minimized,
                WorkingDirectory = destinationDir,
                FileName = sevenZipPath,
                Arguments = "x -aoa -y \"" + filePath + "\" Display.Driver Display.Nview Display.Optimus HDAudio MSVCR NVI2 NVPCF PhysX PPC ShieldWirelessController EULA.txt ListDevices.txt setup.cfg setup.exe"
            };
            process.EnableRaisingEvents = true;
            process.StartInfo = startInfo;
            process.Exited += new EventHandler(ExtractionFinished);
            process.Start();
        }

        private void ExtractionFinished(object sender, EventArgs e)
        {
            string extractedPath = Path.Combine(GlobalVars.saveDirectory, onlineDriv + "-extracted");
            Application.Current.Dispatcher.Invoke(new Action(() => {
                ShowSnackbar(Wpf.Ui.Common.ControlAppearance.Success, Wpf.Ui.Common.SymbolRegular.FolderZip24, Properties.Resources.info_extract_complete, Properties.Resources.info_extract_complete_title);
            }));
            Debug.LogToFile("INFO Extraction exited, deleting 7-zip executable.");
            
            File.Delete(Path.Combine(GlobalVars.saveDirectory, "7zr.exe"));

            Util.CleanInstallConfig(Path.Combine(extractedPath, "setup.cfg"));

            Debug.LogToFile("Starting driver setup.");

            Process process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                WindowStyle = ProcessWindowStyle.Normal,
                WorkingDirectory = extractedPath,
                FileName = "setup.exe",
                Arguments = "-passive -noreboot -noeula"
            };
            process.EnableRaisingEvents = true;
            process.StartInfo = startInfo;
            process.Exited += new EventHandler(InstallFinished);
            process.Start();
        }

        private void InstallFinished(object sender, EventArgs e)
        {
            Application.Current.Dispatcher.Invoke(new Action(() => {
                ShowSnackbar(Wpf.Ui.Common.ControlAppearance.Success, Wpf.Ui.Common.SymbolRegular.CheckmarkCircle24, Properties.Resources.info_install_complete, Properties.Resources.info_install_complete_title);
                buttonInstall.IsEnabled = true;
                buttonInstall.Visibility = Visibility.Collapsed;
                buttonDownload.IsEnabled = true;
                buttonDownload.Visibility = Visibility.Collapsed;
            }));

            Debug.LogToFile("INFO Driver setup complete. Cleaning up setup files.");

            File.Delete(Path.Combine(GlobalVars.saveDirectory, onlineDriv + "-nvidia-installer.exe"));
            Directory.Delete(Path.Combine(GlobalVars.saveDirectory, onlineDriv + "-extracted"), true);
            GlobalVars.isInstalling = false;
            Application.Current.Dispatcher.Invoke(delegate
            {
                UpdateLocalVer();
                Load();
            });
        }

        private void ShowSnackbar(Wpf.Ui.Common.ControlAppearance appearance, Wpf.Ui.Common.SymbolRegular icon, string message = "", string title = "")
        {
            snackbarInfo.Appearance = appearance;
            snackbarInfo.Icon = icon;
            snackbarInfo.Title = title;
            snackbarInfo.Message = message;
            snackbarInfo.Show();
        }
    }
}
