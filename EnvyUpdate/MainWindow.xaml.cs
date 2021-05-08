using System;
using System.Diagnostics;
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

            // Delete installed legacy versions
            UninstallAll();

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
                if (argument == "/debug")
                {
                    MessageBox.Show("Debug mode!");
                    isDebug = true;
                }
                else
                {
                    MessageBox.Show(Properties.Resources.no_compatible_gpu);
                    Environment.Exit(255);
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
            int psid = 0;
            int pfid = 0;
            int osid = 0;
            int dtcid = 0;
            //int langid;

            // This little bool check is necessary for debug mode on systems without an Nvidia GPU. 
            if (!isDebug)
            {
                psid = Util.GetIDs("psid");
                pfid = Util.GetIDs("pfid");
                osid = Util.GetIDs("osid");
                dtcid = Util.GetDTCID();
            }
            else
            {
                psid = Debug.LoadFakeIDs("psid");
                pfid = Debug.LoadFakeIDs("pfid");
                osid = Debug.LoadFakeIDs("osid");
                dtcid = Debug.LoadFakeIDs("dtcid");
                localDriv = Debug.LocalDriv();
                textblockGPU.Text = localDriv;
            }

            gpuURL = "http://www.nvidia.com/Download/processDriver.aspx?psid=" + psid.ToString() + "&pfid=" + pfid.ToString() + "&osid=" + osid.ToString() + "&dtcid=" + dtcid.ToString(); // + "&lid=" + langid.ToString();
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

            if (float.Parse(localDriv) < float.Parse(onlineDriv))
            {
                textblockOnline.Foreground = Brushes.Red;
                buttonDL.Visibility = Visibility.Visible;
                Notify.ShowDrivUpdatePopup();
            }
            else
                textblockOnline.Foreground = Brushes.Green;

            if (GlobalVars.exepath == GlobalVars.appdata)
            {
                WindowState = WindowState.Minimized;
                Hide();
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
            if ((GlobalVars.exepath == GlobalVars.appdata) && File.Exists(GlobalVars.appdata + "EnvyUpdate.exe"))
            {
                MessageBox.Show("EnvyUpdate must be uninstalled because of an application bug. Please download the most recent version again.");
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
                Application.Current.Shutdown();
            else
                e.Cancel = true;
        }
    }
}