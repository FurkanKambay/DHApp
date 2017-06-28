using System;
using System.Diagnostics;
using System.Windows.Input;
using ToastNotifications.Core;

namespace DHApp
{
    public partial class CustomDisplayPart : NotificationDisplayPart
    {
        private CustomNotification _notification;

        public CustomDisplayPart(CustomNotification notification)
        {
            DataContext = _notification = notification;
            InitializeComponent();
        }

        private void NotificationClicked(object sender, MouseButtonEventArgs e)
        {
            if (_notification.Url != null)
                Process.Start(_notification.Url);
        }

        protected override void OnMouseEnter(MouseEventArgs e) { } //BUG
    }
}
