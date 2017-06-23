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
        private MainWindow mainWindow;
        private IEnumerable<DHNotification> lastNotifications;

        private Forms.NotifyIcon trayIcon;
        private Timer timer;

        public App()
        {
            timer = new Timer(1000 * 10 * 1);
            timer.Elapsed += TimerElapsed;
            lastNotifications = new List<DHNotification>();

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

            trayIcon.MouseDoubleClick += async (s, a) =>
            {
                mainWindow.Show();
                mainWindow.Notifications = await DHClient.GetNotificationsAsync();
            };
        }

        internal void StartBackgroundWorker()
        {
            if (MainWindow == null)
                throw new InvalidOperationException("App.MainWindow must not be null.");

            mainWindow = (MainWindow)MainWindow;
            mainWindow.PropertyChanged += NewNotificationArrived;

            trayIcon.Visible = true;
            timer.Start();
        }

        private void NewNotificationArrived(object sender, System.ComponentModel.PropertyChangedEventArgs args)
        {
            if (args.PropertyName == "Notifications")
            {
                var notifications = mainWindow.Notifications.Where(n => n.IsNew);
                var newNotifications = notifications.Except(lastNotifications);

                if (newNotifications.Any())
                {
                    foreach (var notification in newNotifications)
                    {
                        ShowInfo(notification.Content, notification.Time);
                    }
                    lastNotifications = notifications;
                }
            }
        }

        private async void TimerElapsed(object sender, ElapsedEventArgs e)
        {
            mainWindow.Notifications = await DHClient.GetNotificationsAsync();
        }

        public void StopBackgroundWorker()
        {
            timer.Stop();
            trayIcon.Visible = false;
        }

        public void ShowInfo(string title, string message)
        {
            //TODO: Implement interactive notification.
            trayIcon.ShowBalloonTip(2000, title, message, Forms.ToolTipIcon.Info);
        }
    }
}
