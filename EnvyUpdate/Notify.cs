using Notifications.Wpf;

namespace EnvyUpdate
{
    internal class Notify
    {
        public static void ShowDrivUpdatePopup()
        {
            var notificationManager = new NotificationManager();

            notificationManager.Show(new NotificationContent
            {
                Title = "EnvyUpdate",
                Message = Properties.Resources.update_popup_message,
                Type = NotificationType.Information
            }, onClick: Util.ShowMain);
        }
    }
}