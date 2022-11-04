using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
        private bool isDebug = false;
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

            if (Util.IsDCH())
                textblockLocalType.Text = "DCH";
            else
                textblockLocalType.Text = "Standard";

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
                if (arguments.Contains("/debug"))
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

        private async void Load()
        {
            int psid = 0;
            int pfid = 0;
            int osid = 0;
            int dtcid = 0;
            int dtid = 0;
            //TODO: Make a list of languages and match OS language to driver
            //int langid;

            if (File.Exists(GlobalVars.exepath + "sd.envy"))
                radioSD.IsChecked = true;
            else
                radioGRD.IsChecked = true;

            if (File.Exists(GlobalVars.exepath + "skip.envy"))
                skippedVer = File.ReadLines(GlobalVars.exepath + "skip.envy").First();

            // This little bool check is necessary for debug mode on systems without an Nvidia GPU. 
            if (!isDebug)
            {
                psid = Util.GetIDs("psid");
                pfid = Util.GetIDs("pfid");
                osid = Util.GetIDs("osid");
                dtcid = Util.GetDTCID();
                //dtid = Util.GetDTID();
            }
            else
            {
                psid = Debug.LoadFakeIDs("psid");
                pfid = Debug.LoadFakeIDs("pfid");
                osid = Debug.LoadFakeIDs("osid");
                dtcid = Debug.LoadFakeIDs("dtcid");
                dtid = Debug.LoadFakeIDs("dtid");
                localDriv = Debug.LocalDriv();
                textblockGPU.Text = localDriv;
                textblockGPUName.Text = Debug.GPUname();
            }

            //Temporary Studio Driver override logic until I have figured out how to detect it automatically
            //TODO
            try
            {
                if (radioSD.IsChecked == true)
                    dtid = 18;
                else
                    dtid = 1;
            }
            catch (NullReferenceException)
            { }

            gpuURL = "http://www.nvidia.com/Download/processDriver.aspx?psid=" + psid.ToString() + "&pfid=" + pfid.ToString() + "&osid=" + osid.ToString() + "&dtcid=" + dtcid.ToString() + "&dtid=" + dtid.ToString(); // + "&lid=" + langid.ToString();
            WebClient c = new WebClient();
            gpuURL = c.DownloadString(gpuURL);
            if (!gpuURL.Contains("://"))
            {
                //This means a relative link was used.
                gpuURL = "https://www.nvidia.com/Download/" + gpuURL;
            }
            string pContent = c.DownloadString(gpuURL);
            var pattern = @"Windows\/\d{3}\.\d{2}";
            Regex rgx = new Regex(pattern);
            var matches = rgx.Matches(pContent);
            onlineDriv = Regex.Replace(Convert.ToString(matches[0]), "Windows/", "");
            textblockOnline.Text = onlineDriv;
            c.Dispose();

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
                if (File.Exists(GlobalVars.exepath + "skip.envy"))
                    File.Delete(GlobalVars.exepath + "skip.envy");
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
            if ((GlobalVars.exepath == GlobalVars.appdata) && File.Exists(GlobalVars.appdata + "EnvyUpdate.exe"))
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
                Application.Current.Shutdown();
            else
                e.Cancel = true;
        }

        private void radioGRD_Checked(object sender, RoutedEventArgs e)
        {
            if (File.Exists(GlobalVars.exepath + "sd.envy"))
            {
                File.Delete(GlobalVars.exepath + "sd.envy");
                Load();
            }
        }

        private void radioSD_Checked(object sender, RoutedEventArgs e)
        {
            if (!File.Exists(GlobalVars.exepath + "sd.envy"))
            {
                File.Create(GlobalVars.exepath + "sd.envy").Close();
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
            File.WriteAllText(GlobalVars.exepath + "skip.envy", onlineDriv);
            buttonSkip.IsEnabled = false;
            buttonSkip.Content = Properties.Resources.ui_skipped;
            MessageBox.Show(Properties.Resources.skip_confirm);
        }
    }
}