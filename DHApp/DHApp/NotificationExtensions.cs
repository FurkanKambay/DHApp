using ToastNotifications;

namespace DHApp
{
    public static class NotificationExtensions
    {
        public static void ShowNotification(this Notifier notifier, DHNotification notification) =>
            notifier.Notify<CustomNotification>(() => new CustomNotification(notification.Content, notification.Url));

        public static void ShowMessage(this Notifier notifier, string message) =>
            notifier.Notify<CustomNotification>(() => new CustomNotification(message));
    }
}
