using Microsoft.Toolkit.Uwp.Notifications;

namespace EnvyUpdate
{
    internal class Notify
    {
        public static void ShowDrivUpdatePopup()
        {
            try
            {
                var toast = new ToastContentBuilder();
                toast.AddText(Properties.Resources.update_popup_message);
                toast.Show();
            }
            catch (System.Exception ex)
            {
                Debug.LogToFile("WARN Could not show notification. Error: " + ex.Message);
            }
        }
    }
}