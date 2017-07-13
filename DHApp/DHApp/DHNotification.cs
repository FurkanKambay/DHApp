using ToastNotifications.Core;

namespace DHApp
{
    public class DHNotification : NotificationBase
    {
        public string Content { get; private set; }
        public string Url { get; private set; }
        public string Time { get; private set; }
        public string IconUrl { get; private set; }

        public DHNotification(string content, string url, string time, string iconUrl)
        {
            Content = content;
            Url = url;
            Time = time;
            IconUrl = iconUrl;
        }

        public DHNotification(string content, string url)
        {
            Content = content;
            Url = url;
        }

        private CustomDisplayPart displayPart;
        public override NotificationDisplayPart DisplayPart => displayPart ?? (displayPart = new CustomDisplayPart(this));
    }
}
