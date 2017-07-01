using ToastNotifications;

namespace DHApp
{
    public static class NotificationExtensions
    {
        public static void ShowMessage(this Notifier notifier, string message, string url = null) =>
            notifier.Notify<CustomNotification>(() => new CustomNotification(message, url));
    }
}
