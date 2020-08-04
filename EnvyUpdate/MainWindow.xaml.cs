using System;
using System.IO;
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
        private string argument = null;
        private bool isDebug = false;

        public MainWindow()
        {
            InitializeComponent();
            Title += " " + GlobalVars.version;

            if (Util.GetNewVer() != GlobalVars.version)
            {
                MessageBox.Show("yes");
            }
            MessageBox.Show(Util.GetNewVer());

            // Try to get command line arguments
            try
            {
                argument = Environment.GetCommandLineArgs()[1];
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
            // Set correct ticks
            if (File.Exists(GlobalVars.startup + "\\EnvyUpdate.lnk"))
                chkAutostart.IsChecked = true;
            if (File.Exists(GlobalVars.appdata + "EnvyUpdate.exe"))
                chkInstall.IsChecked = true;

            // Check if application is installed and update
            if (GlobalVars.exepath == GlobalVars.appdata)
            {
                try
                {
                    if (Util.GetNewVer() != GlobalVars.version)
                    {
                        Util.UpdateApp();
                    }
                }
                catch (WebException)
                {
                    // Silently fail.
                }
                // Also set correct ticks.
                chkInstall.IsChecked = true;
            }

            // Check for overrides
            if (File.Exists(GlobalVars.desktopOverride))
                GlobalVars.isMobile = false;
            else if (File.Exists(GlobalVars.mobileOverride))
                GlobalVars.isMobile = true;
            // Check if mobile, if no override is present
            else
                GlobalVars.isMobile = Util.IsMobile();

            string locDriv = Util.GetLocDriv();
            if (locDriv != null)
            {
                localDriv = locDriv;
                textblockGPU.Text = locDriv;
                if (GlobalVars.isMobile)
                    textblockGPUName.Text = Util.GetGPUName(false) + " (mobile)";
                else
                    textblockGPUName.Text = Util.GetGPUName(false);
            }
            else
            {
                switch (argument)
                {
                    case "/ignoregpu":
                        MessageBox.Show("Debug: GPU ignored.");
                        isDebug = true;
                        break;
                    default:
                        MessageBox.Show(Properties.Resources.no_compatible_gpu);
                        Environment.Exit(255);
                        break;
                }
            }

            DispatcherTimer Dt = new DispatcherTimer();
            Dt.Tick += new EventHandler(Dt_Tick);
            // Check for new updates every 5 hours.
            Dt.Interval = new TimeSpan(5, 0, 0);
            Dt.Start();
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
            int psid;
            int pfid;
            int osid;
            //int langid;

            // This little bool check is necessary for debug mode on systems without an Nvidia GPU. 
            if (!isDebug)
            {
                psid = Util.GetIDs("pfid");
                pfid = Util.GetIDs("psid");
                osid = Util.GetIDs("osid");
                gpuURL = "http://www.nvidia.com/Download/processDriver.aspx?psid=" + psid.ToString() + "&pfid=" + pfid.ToString() + "&osid=" + osid.ToString(); // + "&lid=" + langid.ToString();
                WebClient c = new WebClient();
                gpuURL = c.DownloadString(gpuURL);
                string pContent = c.DownloadString(gpuURL);
                var pattern = @"\d{3}\.\d{2}&nbsp";
                Regex rgx = new Regex(pattern);
                var matches = rgx.Matches(pContent);
                onlineDriv = Convert.ToString(matches[0]);
                onlineDriv = onlineDriv.Remove(onlineDriv.Length - 5);
                textblockOnline.Text = onlineDriv;
                c.Dispose();

                if (localDriv != onlineDriv)
                {
                    textblockOnline.Foreground = Brushes.Red;
                    buttonDL.Visibility = Visibility.Visible;
                    Notify.ShowDrivUpdatePopup();
                }
                else
                    textblockOnline.Foreground = Brushes.Green;
            }

            if (GlobalVars.exepath == GlobalVars.appdata)
            {
                WindowState = WindowState.Minimized;
                Hide();
            }
        }

        private void buttonDL_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(gpuURL);
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

        private void chkInstall_Checked(object sender, RoutedEventArgs e)
        {
            if (chkAutostart != null)
            {
                chkAutostart.IsEnabled = true;
            }
            if (GlobalVars.exepath != GlobalVars.appdata)
            {
                if (!Directory.Exists(GlobalVars.appdata))
                {
                    Directory.CreateDirectory(GlobalVars.appdata);
                }
                File.Copy(GlobalVars.exeloc, GlobalVars.appdata + "EnvyUpdate.exe", true);

                if (File.Exists(GlobalVars.mobileOverride))
                    File.Copy(GlobalVars.mobileOverride, GlobalVars.appdata + "mobile.envy", true);
                if (File.Exists(GlobalVars.desktopOverride))
                    File.Copy(GlobalVars.desktopOverride, GlobalVars.appdata + "desktop.envy", true);

                Util.CreateShortcut("EnvyUpdate", GlobalVars.startmenu, GlobalVars.appdata + "EnvyUpdate.exe", Properties.Resources.app_description);
            }
        }

        private void chkInstall_Unchecked(object sender, RoutedEventArgs e)
        {
            // Only uninstall if user confirms. Prevents accidental uninstalls.
            if (MessageBox.Show(Properties.Resources.uninstall_confirm, Properties.Resources.uninstall_heading, MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                if (chkAutostart != null)
                {
                    chkAutostart.IsEnabled = false;
                    chkAutostart.IsChecked = false;
                }

                File.Delete(GlobalVars.appdata + "desktop.envy");
                File.Delete(GlobalVars.appdata + "mobile.envy");

                File.Delete(GlobalVars.startup + "\\EnvyUpdate.lnk");
                File.Delete(GlobalVars.startmenu + "\\EnvyUpdate.lnk");

                if ((GlobalVars.exepath == GlobalVars.appdata) && File.Exists(GlobalVars.appdata + "EnvyUpdate.exe"))
                    Util.SelfDelete();
                else if (File.Exists(GlobalVars.appdata + "EnvyUpdate.exe"))
                    File.Delete(GlobalVars.appdata + "EnvyUpdate.exe");
            }
            else
                chkInstall.IsChecked = true;
        }

        private void chkAutostart_Checked(object sender, RoutedEventArgs e)
        {
            Util.CreateShortcut("EnvyUpdate", GlobalVars.startup, GlobalVars.appdata + "EnvyUpdate.exe", Properties.Resources.app_description);
        }

        private void chkAutostart_Unchecked(object sender, RoutedEventArgs e)
        {
            File.Delete(GlobalVars.startup + "\\EnvyUpdate.lnk");
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (MessageBox.Show(Properties.Resources.exit_confirm, "", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                Application.Current.Shutdown();
            else
                e.Cancel = true;
        }
    }
}