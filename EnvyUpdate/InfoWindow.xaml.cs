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
        bool defaultIsMobile = false;
        bool isOverride = false;
        public InfoWindow()
        {
            InitializeComponent();

            if (GlobalVars.isMobile)
                chkMobile.IsChecked = true;

            if (Util.IsMobile())
                defaultIsMobile = true;

            if (defaultIsMobile != GlobalVars.isMobile)
                isOverride = true;
        }

        private void ButtonWeb_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/fyr77/EnvyUpdate/");
        }

        private void text_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            this.Cursor = Cursors.Hand;
        }

        private void text_MouseLeave(object sender, MouseEventArgs e)
        {
            this.Cursor = Cursors.Arrow;
        }

        private void textEnvyUpdate_MouseDown(object sender, MouseButtonEventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/fyr77/EnvyUpdate/blob/master/LICENSE");
        }

        private void textFody_MouseDown(object sender, MouseButtonEventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/Fody/Fody/blob/master/License.txt");
        }

        private void textCostura_MouseDown(object sender, MouseButtonEventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/Fody/Costura/blob/master/license.txt");
        }

        private void textNotifyIcon_MouseDown(object sender, MouseButtonEventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/hardcodet/wpf-notifyicon/blob/master/LICENSE");
        }

        private void textNotifications_MouseDown(object sender, MouseButtonEventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/Federerer/Notifications.Wpf/blob/master/LICENSE");
        }
        private void textNewtonsoft_MouseDown(object sender, MouseButtonEventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/JamesNK/Newtonsoft.Json/blob/master/LICENSE.md");
        }

        private void chkMobile_Checked(object sender, RoutedEventArgs e)
        {
            if (isOverride)
            {
                // If an override was present, delete it.
                bool deleteSuccess = false;
                while (!deleteSuccess)
                {
                    try
                    {
                        File.Delete(GlobalVars.desktopOverride);
                        deleteSuccess = true;
                    }
                    catch (IOException)
                    {
                        // This is necessary in case someone ticks and unticks the option quickly, as the File.Create Method has sometimes yet to close the file.
                    }
                }
                isOverride = false;
            }
            else
            {
                File.Create(GlobalVars.mobileOverride).Close();
                GlobalVars.isMobile = true;
                isOverride = true;
            }
        }

        private void chkMobile_Unchecked(object sender, RoutedEventArgs e)
        {
            if (isOverride)
            {
                // If an override was present, delete it.
                bool deleteSuccess = false;
                while (!deleteSuccess)
                {
                    try
                    {
                        File.Delete(GlobalVars.mobileOverride);
                        deleteSuccess = true;
                    }
                    catch (IOException)
                    {
                        // This is necessary in case someone ticks and unticks the option quickly, as the File.Create Method has sometimes yet to close the file.
                    }
                }
                
                isOverride = false;
            }
            else
            {
                File.Create(GlobalVars.desktopOverride).Close();
                GlobalVars.isMobile = false;
                isOverride = true;
            }
        }
    }
}
