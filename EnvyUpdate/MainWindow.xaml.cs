using System;
using System.Windows;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows.Media;
using System.Windows.Threading;

namespace EnvyUpdate
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string localDriv = null;
        string onlineDriv = null;
        readonly string appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\envyupdate\\";
        readonly string startup = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
        string gpuURL = null;
        readonly string exeloc = System.Reflection.Assembly.GetEntryAssembly().Location;
        readonly string exepath = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location) + "\\";
        readonly string startmenu = Environment.GetFolderPath(Environment.SpecialFolder.StartMenu);
        readonly string version = "1.0";

        public MainWindow()
        {
            InitializeComponent();
            Title += " " + version;
            if (Util.IsInstanceOpen("EnvyUpdate"))
            {
                MessageBox.Show("Application is already running.");
                Environment.Exit(1);
            }
            if (!Directory.Exists(appdata))
            {
                Directory.CreateDirectory(appdata);
            }

            try
            {
                if (Util.GetNewVer() != version && exepath == appdata)
                {
                    Util.UpdateApp();
                }
            }
            catch (WebException)
            {
                //Silently fail.
            }

            if (Util.GetLocDriv() != null)
            {
                localDriv = Util.GetLocDriv();
                textblockGPU.Text = localDriv;
            }
            else
            {
                MessageBox.Show("No NVIDIA GPU found. Application will exit.");
                Environment.Exit(255);
            }
            if (File.Exists(appdata + "nvidia-update.txt"))
            {
                chkPortable.IsChecked = false;
                DispatcherTimer Dt = new DispatcherTimer();
                Dt.Tick += new EventHandler(Dt_Tick);
                Dt.Interval = new TimeSpan(5, 0, 0);
                Dt.Start();
                Load();
            }
            if (File.Exists(startup + "\\EnvyUpdate.lnk"))
            {
                chkAutostart.IsChecked = true;
            }
        }

        private void Dt_Tick(object sender, EventArgs e)
        {
            Load();
        }

        private void buttonHelp_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/fyr77/EnvyUpdate/");
        }

        private void Grid_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                try
                {
                    string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                    Load(files);
                }
                catch (WebException)
                {
                    MessageBox.Show("Network Error. Are you connected to the internet?", "Network Error");
                }
            }
        }
        private void Load()
        {
            FileInfo f = new FileInfo(appdata + "nvidia-update.txt");

            int psid;
            int pfid;
            int osid;
            int langid;

            chkPortable.Visibility = Visibility.Hidden;
            labelDrag.Content = "Drag nvidia.com-cookies.txt here if you have changed your graphics card.";
            psid = Util.GetData(f.FullName, "ProductSeries");
            pfid = Util.GetData(f.FullName, "ProductType");
            osid = Util.GetData(f.FullName, "OperatingSystem");
            langid = Util.GetData(f.FullName, "Language");
            gpuURL = "http://www.nvidia.com/Download/processDriver.aspx?psid=" + psid.ToString() + "&pfid=" + pfid.ToString() + "&rpf=1&osid=" + osid.ToString() + "&lid=" + langid.ToString() + "&ctk=0";
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
        private void Load(string[] files)
        {
            FileInfo f = new FileInfo(files[0]);

            int psid;
            int pfid;
            int osid;
            int langid;

            if (chkPortable.IsChecked == false)
            {
                File.Copy(f.FullName, appdata + "nvidia-update.txt", true);
                f = new FileInfo(appdata + "nvidia-update.txt");

                chkPortable.Visibility = Visibility.Hidden;
                labelDrag.Content = "Drag nvidia.com-cookies.txt here if you have changed your graphics card.";
            }
            psid = Util.GetData(f.FullName, "ProductSeries");
            pfid = Util.GetData(f.FullName, "ProductType");
            osid = Util.GetData(f.FullName, "OperatingSystem");
            langid = Util.GetData(f.FullName, "Language");
            gpuURL = "http://www.nvidia.com/Download/processDriver.aspx?psid=" + psid.ToString() + "&pfid=" + pfid.ToString() + "&rpf=1&osid=" + osid.ToString() + "&lid=" + langid.ToString() + "&ctk=0";
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
            }
            else
                textblockOnline.Foreground = Brushes.Green;
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

        private void chkPortable_Unchecked(object sender, RoutedEventArgs e)
        {
            if (chkAutostart != null)
            {
                chkAutostart.IsEnabled = true;
            }
        }

        private void chkPortable_Checked(object sender, RoutedEventArgs e)
        {
            if (chkAutostart != null)
            {
                chkAutostart.IsEnabled = false;
                chkAutostart.IsChecked = false;
            }
        }

        private void chkAutostart_Checked(object sender, RoutedEventArgs e)
        {
            if (exepath != appdata)
            {
                File.Copy(exeloc, appdata + "EnvyUpdate.exe", true);
                Util.CreateShortcut("EnvyUpdate", startup, appdata + "EnvyUpdate.exe", "Nvidia Updater Application.");
                Util.CreateShortcut("EnvyUpdate", startmenu, appdata + "EnvyUpdate.exe", "Nvidia Updater Application.");
            }
            else
            {
                chkAutostart.IsEnabled = false;
            }
        }

        private void chkAutostart_Unchecked(object sender, RoutedEventArgs e)
        {
            File.Delete(appdata + "EnvyUpdate.exe");
            File.Delete(startup + "\\EnvyUpdate.lnk");
            File.Delete(startmenu + "\\EnvyUpdate.lnk");
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            var window = MessageBox.Show("Exit EnvyUpdate?", "", MessageBoxButton.YesNo);
            e.Cancel = (window == MessageBoxResult.No);
            Application.Current.Shutdown();
        }
    }
}
