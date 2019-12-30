using Notifications.Wpf;

namespace EnvyUpdate
{
    class Notify
    {
        public static void ShowDrivUpdatePopup()
        {
            var notificationManager = new NotificationManager();

            notificationManager.Show(new NotificationContent
            {
                Title = "EnvyUpdate",
                Message = "A new driver update is available for your graphics card. Click for more info.",
                Type = NotificationType.Information
            }, onClick: Util.ShowMain);
        }
    }
}
