using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Timers;
using System.Windows;
using Forms = System.Windows.Forms;

namespace DHApp
{
    public partial class App : Application
    {
        public new MainWindow MainWindow;

        private IEnumerable<DHNotification> lastNotifications;
        private Forms.NotifyIcon trayIcon;
        private Timer timer;

        public App()
        {
            timer = new Timer(1000 * 10 * 1);
            timer.Elapsed += async (s, a) => MainWindow.Notifications = await DHClient.GetNotificationsAsync();

            lastNotifications = Enumerable.Empty<DHNotification>();

            trayIcon = new Forms.NotifyIcon
            {
                Icon = System.Drawing.Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location),
                ContextMenu = new Forms.ContextMenu(new Forms.MenuItem[]
                {
                    new Forms.MenuItem("Çıkış", new EventHandler((s, a) =>
                    {
                        trayIcon.Visible = false;
                        Current.Shutdown();
                    }))
                })
            };

            trayIcon.DoubleClick += async (s, a) =>
            {
                StopBackgroundWorker();
                MainWindow.Show();
                MainWindow.Notifications = await DHClient.GetNotificationsAsync();
            };

            Logger.Log("Tray icon initialized");

            Exit += (s, a) => Logger.Log("Application exit");
        }

        #region Public Methods
        public void StartBackgroundWorker()
        {
            if (MainWindow == null)
                throw new InvalidOperationException("App.MainWindow must not be null.");

            lastNotifications = MainWindow.Notifications;

            MainWindow.PropertyChanged += NewNotificationArrived;
            trayIcon.Visible = true;
            timer.Start();

            Logger.Log("Background worker started");
        }

        public void StopBackgroundWorker()
        {
            timer.Stop();
            trayIcon.Visible = false;
            MainWindow.PropertyChanged -= NewNotificationArrived;

            Logger.Log("Background worker stopped");
        }

        public void ShowNotification(DHNotification notification)
        {
            if (!trayIcon.Visible)
                throw new InvalidOperationException("Tray icon is not visible");

            //BUG: doesn't happen per-notification. also happens when user clicks on tray icon
            //TODO: use another library. (NuGet)
            void clickAction(object s, EventArgs a)
            {
                trayIcon.BalloonTipClicked -= clickAction;
                System.Diagnostics.Process.Start(notification.Url);
            }

            trayIcon.BalloonTipClicked += clickAction;

            trayIcon.ShowBalloonTip(
                notificationTimeout,
                notification.Content,
                notification.Time ?? "Zaman hatalı",
                Forms.ToolTipIcon.None);

            Logger.Log("Showed notification");
        }

        public void ShowInformation(string title, string content)
        {
            if (!trayIcon.Visible)
                throw new InvalidOperationException();

            trayIcon.ShowBalloonTip(notificationTimeout, title, content, Forms.ToolTipIcon.Info);

            Logger.Log("Showed information");
        }
        #endregion Public Methods

        private void NewNotificationArrived(object sender, System.ComponentModel.PropertyChangedEventArgs args)
        {
            if (args.PropertyName != "Notifications")
                throw new InvalidOperationException();

            var currentNotifications = MainWindow.Notifications;
            var newlyAddedNotifications = currentNotifications.Except(lastNotifications);

            if (newlyAddedNotifications.Any(n => n.IsNew))
                Logger.Log("New notification(s) arrived");

            foreach (var notification in newlyAddedNotifications)
                ShowNotification(notification);

            lastNotifications = currentNotifications;
        }

        private const int notificationTimeout = 3000;
    }
}
