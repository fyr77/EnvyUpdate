using System;
using System.Windows;
using System.Windows.Input;
using System.IO;

namespace EnvyUpdate
{
    /// <summary>
    /// Interaction logic for InfoWindow.xaml
    /// </summary>
    public partial class InfoWindow : Window
    {
        public InfoWindow()
        {
            InitializeComponent();

            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            System.Diagnostics.FileVersionInfo fvi = System.Diagnostics.FileVersionInfo.GetVersionInfo(assembly.Location);
            string version = fvi.FileVersion;

            labelVer.Content += " " + version;
            if (GlobalVars.monitoringInstall)
                labelVer.FontStyle = FontStyles.Italic;

            if (File.Exists(Path.Combine(GlobalVars.exedirectory, "envyupdate.log")))
                chkLog.IsChecked = true;
        }

        private void ButtonWeb_Click(object sender, RoutedEventArgs e)
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
                if (File.Exists(Path.Combine(GlobalVars.exedirectory, "envyupdate.log")))
                    File.Move(Path.Combine(GlobalVars.exedirectory, "envyupdate.log"), Path.Combine(GlobalVars.exedirectory, "envyupdate." + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".log"));
                Debug.isVerbose = false;
            }
        }
    }
}
