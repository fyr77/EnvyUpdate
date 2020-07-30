using System;
using System.Globalization;
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
        private readonly string appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\envyupdate\\";
        private readonly string startup = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
        private string gpuURL = null;
        private readonly string exeloc = System.Reflection.Assembly.GetEntryAssembly().Location;
        private readonly string exepath = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location) + "\\";
        private readonly string startmenu = Environment.GetFolderPath(Environment.SpecialFolder.StartMenu);
        private readonly string version = "2.0";
        private string argument = null;

        public MainWindow()
        {
            InitializeComponent();
            Title += " " + version;

            // Try to get command line arguments
            try
            {
                argument = Environment.GetCommandLineArgs()[1];
                Console.WriteLine("Starting in debug mode.");
            }
            catch (IndexOutOfRangeException)
            {
                // This is necessary, since .NET throws an exception if you check for a non-existant arg.
                Console.WriteLine("Starting in release mode.");
            }

            // Check if EnvyUpdate is already running
            if (Util.IsInstanceOpen("EnvyUpdate"))
            {
                MessageBox.Show("Application is already running.");
                Environment.Exit(1);
            }
            // Check if application is installed and update
            if (exepath == appdata)
            {
                try
                {
                    if (Util.GetNewVer() != version)
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
                if (File.Exists(startup + "\\EnvyUpdate.lnk"))
                    chkAutostart.IsChecked = true;
            }
            if (Util.GetLocDriv() != null)
            {
                localDriv = Util.GetLocDriv();
                textblockGPU.Text = localDriv;
            }
            else
            {
                switch (argument)
                {
                    case "/ignoregpu":
                        MessageBox.Show("Debug: GPU ignored.");
                        break;
                    default:
                        MessageBox.Show("No NVIDIA GPU found. Application will exit.");
                        Environment.Exit(255);
                        break;
                }
            }
            
            DispatcherTimer Dt = new DispatcherTimer();
            Dt.Tick += new EventHandler(Dt_Tick);
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
            //System.Diagnostics.Process.Start("https://github.com/fyr77/EnvyUpdate/");
        }

        private void Load()
        {
            int psid;
            int pfid;
            int osid;
            //int langid;

            psid = Util.GetIDs("psid");
            pfid = Util.GetIDs("pfid");
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
            if (exepath == appdata)
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
            if (exepath != appdata)
            {
                if (!Directory.Exists(appdata))
                {
                    Directory.CreateDirectory(appdata);
                }
                File.Copy(exeloc, appdata + "EnvyUpdate.exe", true);
                Util.CreateShortcut("EnvyUpdate", startmenu, appdata + "EnvyUpdate.exe", "Nvidia Updater Application.");
            }
        }

        private void chkInstall_Unchecked(object sender, RoutedEventArgs e)
        {
            if (chkAutostart != null)
            {
                chkAutostart.IsEnabled = false;
                chkAutostart.IsChecked = false;
            }
            if (Directory.Exists(appdata))
            {
                File.Delete(appdata + "EnvyUpdate.exe");
                File.Delete(startup + "\\EnvyUpdate.lnk");
                File.Delete(startmenu + "\\EnvyUpdate.lnk");
            }
        }

        private void chkAutostart_Checked(object sender, RoutedEventArgs e)
        {
            Util.CreateShortcut("EnvyUpdate", startup, appdata + "EnvyUpdate.exe", "Nvidia Updater Application.");
        }

        private void chkAutostart_Unchecked(object sender, RoutedEventArgs e)
        {
            File.Delete(startup + "\\EnvyUpdate.lnk");
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            var window = MessageBox.Show("Exit EnvyUpdate?", "", MessageBoxButton.YesNo);
            e.Cancel = (window == MessageBoxResult.No);
            Application.Current.Shutdown();
        }
    }
}