using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

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

            if (File.Exists(Path.Combine(GlobalVars.exedirectory, "envyupdate.log")))
                chkLog.IsChecked = true;

            textBoxLicEnvyupdate.Text = Properties.Licenses.EnvyUpdate;
            textBoxLicFody.Text = Properties.Licenses.Fody;
            textBoxLicCostura.Text = Properties.Licenses.CosturaFody;
            textBoxLicResourceembedder.Text = Properties.Licenses.ResourceEmbedder;
            textBoxLicWindowscommunitytoolkit.Text = Properties.Licenses.WindowsCommunityToolkit;
            textBoxLicWpfui.Text = Properties.Licenses.wpfui;
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
                if (File.Exists(Path.Combine(GlobalVars.exedirectory, "envyupdate.log")))
                    File.Move(Path.Combine(GlobalVars.exedirectory, "envyupdate.log"), Path.Combine(GlobalVars.exedirectory, "envyupdate." + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".log"));
                Debug.isVerbose = false;
            }
        }
    }
}
