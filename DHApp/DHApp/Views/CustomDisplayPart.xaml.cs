using System.Diagnostics;
using System.Windows.Input;
using ToastNotifications.Core;

namespace DHApp.Views
{
    public partial class CustomDisplayPart : NotificationDisplayPart
    {
        public DHNotification _notification;

        public CustomDisplayPart(DHNotification notification)
        {
            InitializeComponent();
            DataContext = _notification = notification;

            if (!string.IsNullOrWhiteSpace(notification.Url))
                Cursor = Cursors.Hand;
        }

        private void NotificationClicked(object sender, MouseButtonEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(_notification.Url))
                Process.Start(_notification.Url);
        }

        protected override void OnMouseEnter(MouseEventArgs e) { } // ToastNotifications bug.
    }
}
