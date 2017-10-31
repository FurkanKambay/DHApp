using DHApp.Views;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using ToastNotifications;
using ToastNotifications.Lifetime;
using ToastNotifications.Position;
using Forms = System.Windows.Forms;
using Threading = System.Threading;

namespace DHApp
{
    public partial class App : Application
    {
        public new MainWindow MainWindow;

        private Notifier notifier;
        private Forms.NotifyIcon trayIcon;
        private Timer timer = new Timer(1000 * 5);
        private string[] lastNotifications;

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

            trayIcon.MouseDoubleClick += async (s, a) =>
            {
                if (a.Button == Forms.MouseButtons.Left)
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
            };
        }

        public void StartBackgroundWorker()
        {
            if (MainWindow == null)
                throw new InvalidOperationException("App.MainWindow is null.");

            lastNotifications = MainWindow.Notifications.Select(n => n.Url).ToArray();

            trayIcon.Visible = true;

            InitializeNotifier();

            timer.Start();
        }

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

        public void StopBackgroundWorker()
        {
            timer.Stop();
            notifier?.Dispose();
            trayIcon.Visible = false;
        }

        public void ShowMessage(string message, string clickUrl = null)
        {
            if (notifier == null)
                throw new InvalidOperationException("Notifier is null. Initialize the notifier first.");

            notifier.Notify<DHNotification>(() => new DHNotification(message, clickUrl));

            // await Task.Delay(TimeSpan.FromSeconds(5));
            // notifier?.Dispose();
        }

        private async void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            timer.Stop();
            var currentNotifications = await DHClient.GetNotificationsAsync() ?? Array.Empty<DHNotification>();

            // InitializeNotifier();
            // BUG: "belongs to different thread"

            foreach (var newOne in currentNotifications.Where(n => !lastNotifications.Contains(n.Url)))
                ShowMessage(newOne.Content, newOne.Url);

            // notifier?.Dispose();

            lastNotifications = currentNotifications.Select(n => n.Url).ToArray();
            timer.Start();
        }
    }
}
