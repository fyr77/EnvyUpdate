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
        }

        private void ButtonWeb_Click(object sender, RoutedEventArgs e)
        {
            Debug.LogToFile("INFO Launching website.");
            System.Diagnostics.Process.Start("https://github.com/fyr77/EnvyUpdate/");
        }
    }
}
