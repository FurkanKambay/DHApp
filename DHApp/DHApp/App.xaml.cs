using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Timers;
using System.Windows;
using ToastNotifications;
using ToastNotifications.Lifetime;
using ToastNotifications.Position;
using Forms = System.Windows.Forms;

namespace DHApp
{
    public partial class App : Application
    {
        #region Fields
        private const int notificationTimeout = 3000;
        private Timer timer;

        public new MainWindow MainWindow;
        private IEnumerable<DHNotification> lastNotifications;

        private Notifier notifier;
        private Forms.NotifyIcon trayIcon;
        #endregion Fields

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
                    new Forms.MenuItem("Çıkış", new EventHandler((s, a) => Current.Shutdown()))
                })
            };

            trayIcon.MouseDoubleClick += async (s, args) =>
            {
                if (args.Button == Forms.MouseButtons.Left)
                {
                    StopBackgroundWorker();

                    MainWindow.Notifications = await DHClient.GetNotificationsAsync();
                    MainWindow.Show();
                    MainWindow.Activate();
                }
            };

            Exit += (s, a) =>
            {
                trayIcon.Visible = false;
                notifier.Dispose();
                Logger.Log("Application exit");
            };
        }

        #region Public Methods
        public void StartBackgroundWorker()
        {
            if (MainWindow == null)
            {
                Logger.Log("Couldn't start background worker");
                throw new InvalidOperationException("App.MainWindow must not be null.");
            }

            lastNotifications = MainWindow.Notifications;

            MainWindow.PropertyChanged += NewNotificationArrived;
            trayIcon.Visible = true;
            InitializeNotifier();
            timer.Start();

            Logger.Log("Background worker started");
        }

        public void StopBackgroundWorker()
        {
            timer.Stop();
            notifier?.Dispose();
            trayIcon.Visible = false;
            MainWindow.PropertyChanged -= NewNotificationArrived;

            Logger.Log("Background worker stopped");
        }

        public void ShowNotification(DHNotification notification)
        {
            if (!trayIcon.Visible)
            {
                Logger.Log("Couldn't show notification");
                throw new InvalidOperationException("Tray icon is not visible");
            }

            notifier.ShowNotification(notification);

            Logger.Log("Showed notification");
        }

        public void ShowMessage(string message)
        {
            if (!trayIcon.Visible)
            {
                Logger.Log("Couldn't show message");
                throw new InvalidOperationException();
            }

            notifier.ShowMessage(message);

            Logger.Log("Showed information");
        }
        #endregion Public Methods

        #region Private Methods
        private void InitializeNotifier()
        {
            notifier?.Dispose();
            notifier = new Notifier(cfg =>
            {
                cfg.PositionProvider = new PrimaryScreenPositionProvider(
                    Corner.TopRight, 16, 16);

                cfg.LifetimeSupervisor = new TimeAndCountBasedLifetimeSupervisor(
                    TimeSpan.FromSeconds(5),
                    MaximumNotificationCount.FromCount(5));

                cfg.Dispatcher = Current.Dispatcher;

                cfg.DisplayOptions.Width = 360;
            });
        }

        private void NewNotificationArrived(object sender, System.ComponentModel.PropertyChangedEventArgs args)
        {
            if (args.PropertyName == nameof(MainWindow.Notifications))
            {
                var currentNotifications = MainWindow.Notifications.Where(n => n.IsNew);
                var newNotifications = currentNotifications.Except(lastNotifications);

                if (newNotifications.Any())
                    Logger.Log("New notification(s) arrived");

                foreach (var notification in newNotifications)
                    ShowNotification(notification);

                lastNotifications = currentNotifications;
            }
        }
        #endregion Private Methods
    }
}
