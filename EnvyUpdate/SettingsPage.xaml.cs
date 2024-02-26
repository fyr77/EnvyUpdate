using System;
using System.IO;
using System.Windows;

namespace EnvyUpdate
{
    /// <summary>
    /// Interaction logic for SettingsPage.xaml
    /// </summary>
    public partial class SettingsPage
    {
        public SettingsPage()
        {
            InitializeComponent();

            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            System.Diagnostics.FileVersionInfo fvi = System.Diagnostics.FileVersionInfo.GetVersionInfo(assembly.Location);
            string version = fvi.FileVersion;

            textBlockVer.Text = version;
            if (GlobalVars.monitoringInstall)
                textBlockVer.FontStyle = FontStyles.Italic;

            if (File.Exists(Path.Combine(GlobalVars.saveDirectory, "envyupdate.log")) || File.Exists(Path.Combine(GlobalVars.appdata, "envyupdate.log")))
                chkLog.IsChecked = true;

            if (GlobalVars.useAppdata)
                chkAppdata.IsChecked = true;

            if (!GlobalVars.hasWrite)
                chkAppdata.IsEnabled = false;

            textBoxLicEnvyupdate.Text = Properties.Licenses.EnvyUpdate;
            textBoxLicFody.Text = Properties.Licenses.Fody;
            textBoxLicCostura.Text = Properties.Licenses.CosturaFody;
            textBoxLicResourceembedder.Text = Properties.Licenses.ResourceEmbedder;
            textBoxLicWindowscommunitytoolkit.Text = Properties.Licenses.WindowsCommunityToolkit;
            textBoxLicWpfui.Text = Properties.Licenses.wpfui;
            textBoxLic7zip.Text = Properties.Licenses._7zip;
        }

        private void CardWeb_Click(object sender, RoutedEventArgs e)
        {
            Debug.LogToFile("INFO Launching website.");
            System.Diagnostics.Process.Start("https://github.com/fyr77/EnvyUpdate/");
        }

        private void chkLog_Checked(object sender, RoutedEventArgs e)
        {
            if (!Debug.isVerbose)
            {
                Debug.isVerbose = true;
                Debug.LogToFile("------");
                Debug.LogToFile("INFO Enabled logging to file. Restart Application to see full startup log.");
            }
        }

        private void chkLog_Unchecked(object sender, RoutedEventArgs e)
        {
            if (Debug.isVerbose)
            {
                Debug.LogToFile("INFO Disabled logging to file.");
                if (File.Exists(Path.Combine(GlobalVars.saveDirectory, "envyupdate.log")))
                    File.Move(Path.Combine(GlobalVars.saveDirectory, "envyupdate.log"), Path.Combine(GlobalVars.saveDirectory, "envyupdate." + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".log"));
                Debug.isVerbose = false;
            }
        }

        private void chkAppdata_Checked(object sender, RoutedEventArgs e)
        {
            if (!Directory.Exists(GlobalVars.appdata))
                Directory.CreateDirectory(GlobalVars.appdata);

            GlobalVars.useAppdata = true;
            GlobalVars.saveDirectory = GlobalVars.appdata;
            Util.MoveFilesToAppdata();

            Debug.LogToFile("INFO Switched to AppData directory.");
        }

        private void chkAppdata_Unchecked(object sender, RoutedEventArgs e)
        {
            GlobalVars.useAppdata = false;
            GlobalVars.saveDirectory = GlobalVars.directoryOfExe;

            if (Directory.Exists(GlobalVars.appdata))
            {
                Util.MoveFilesToExe();
                Directory.Delete(GlobalVars.appdata, true);
            }

            Debug.LogToFile("INFO Switched to EXE directory.");
        }

        private void chkAutodl_Checked(object sender, RoutedEventArgs e)
        {
            GlobalVars.autoDownload = true;
            if (!File.Exists(Path.Combine(GlobalVars.saveDirectory, "autodl.envy")))
            {
                File.Create(Path.Combine(GlobalVars.saveDirectory, "autodl.envy"));
            }
        }

        private void chkAutodl_Unchecked(object sender, RoutedEventArgs e)
        {
            GlobalVars.autoDownload = false;
            if (File.Exists(Path.Combine(GlobalVars.saveDirectory, "autodl.envy")))
            {
                File.Delete(Path.Combine(GlobalVars.saveDirectory, "autodl.envy"));
            }
        }
    }
}
