using System.Windows;
using System.Windows.Input;

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
        }

        private void Button_Click(object sender, RoutedEventArgs e)
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
    }
}
