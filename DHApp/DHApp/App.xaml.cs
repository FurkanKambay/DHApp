using DHApp.Views;
using System;
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
        private Timer timer = new Timer(1000 * 10);

        public new MainWindow MainWindow;
        private string[] lastNotifications;

        private Notifier notifier;
        private Forms.NotifyIcon trayIcon;
        #endregion Fields

        public App()
        {
            timer.Elapsed += Timer_Elapsed;

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

                    MainWindow.Show();
                    MainWindow.Notifications = await DHClient.GetNotificationsAsync();
                }
            };

            Exit += (s, a) =>
            {
                trayIcon.Visible = false;
                notifier?.Dispose();
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

            lastNotifications = MainWindow.Notifications.Select(n => n.Url).ToArray();

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

            Logger.Log("Background worker stopped");
        }

        public void ShowMessage(string message, string clickUrl = null)
        {
            string messageType = (clickUrl == null) ? "message" : "notification";

            if (!trayIcon.Visible)
            {
                Logger.Log($"Couldn't show {messageType}");
                throw new InvalidOperationException("Tray icon is not visible");
            }

            notifier.Notify<DHNotification>(() => new DHNotification(message, clickUrl));
            Logger.Log($"Showed {messageType}");
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

        private async void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            timer.Stop();
            var currentNotifications = await DHClient.GetNotificationsAsync() ?? Array.Empty<DHNotification>();

            foreach (var newOne in currentNotifications.Where(n => !lastNotifications.Contains(n.Url)))
                ShowMessage(newOne.Content, newOne.Url);

            lastNotifications = currentNotifications.Select(n => n.Url).ToArray();
            timer.Start();
        }
        #endregion Private Methods
    }
}
