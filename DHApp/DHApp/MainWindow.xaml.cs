using DHApp.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace DHApp
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        #region Properties and Fields
        private static App app = (App)Application.Current;

        public event PropertyChangedEventHandler PropertyChanged;
        private IEnumerable<DHNotification> notifications;
        public IEnumerable<DHNotification> Notifications
        {
            get => notifications ?? Enumerable.Empty<DHNotification>();
            set
            {
                if (notifications != value)
                {
                    notifications = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Notifications)));
                }
            }
        }
        #endregion Properties and Fields

        public MainWindow()
        {
            InitializeComponent();

            app.MainWindow = this;
            DataContext = this;

            TitleGrid.MouseLeftButtonDown += (_, __) => DragMove();

            Activated += WindowActivated;
            Deactivated += WindowDeactivated;

            DHClient.Login += OnDHLogin;
            DHClient.Logout += OnDHLogout;

            ShowLoginWindow();
        }

        #region DH Events
        private async void OnDHLogin(string cookie)
        {
            Settings.Default.Cookie = cookie;
            Settings.Default.Save();

            UsernameText.Text = DHClient.Username;

            string avatarUrl = await DHClient.GetAvatarUrlAsync();
            if (!string.IsNullOrWhiteSpace(avatarUrl))
                Avatar.Source = new BitmapImage(new Uri(avatarUrl));

            Logger.Log("Logged in");

            await RefreshNotificationsAsync();
        }

        private void OnDHLogout()
        {
            app.StopBackgroundWorker();

            Settings.Default.Cookie = null;
            Settings.Default.Save();

            Logger.Log("Logged out");

            NotificationList.ItemsSource = null;
            ShowLoginWindow();
        }
        #endregion DH Events

        #region Window Events
        private async void WindowActivated(object sender, EventArgs e)
        {
            await RefreshNotificationsAsync();
            app.StopBackgroundWorker();
        }

        private void WindowDeactivated(object sender, EventArgs e)
        {
            SendToBackground();
        }
        #endregion Window Events

        #region Click Events
        private async void RefreshClicked(object sender, RoutedEventArgs e)
        {
            await RefreshNotificationsAsync();
        }

        private async void IgnoreClicked(object sender, RoutedEventArgs e)
        {
            await DHClient.IgnoreNotificationsAsync();
            Notifications = null;
            Logger.Log("Ignored notifications");
        }

        private async void LogoutClicked(object sender, RoutedEventArgs e)
        {
            await DHClient.LogOutAsync();
        }

        private void CloseClicked(object sender, RoutedEventArgs e)
        {
            SendToBackground();
        }

        private void NotificationClicked(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is ListViewItem item && item.IsSelected && item.Content is DHNotification notification)
                System.Diagnostics.Process.Start(notification.Url);
        }
        #endregion Click Events

        #region Methods
        private void ShowLoginWindow()
        {
            Hide();

            if (new LoginWindow().ShowDialog().Value)
                Show();
            else
                app.Shutdown();
        }

        private void SendToBackground()
        {
            app.StartBackgroundWorker();
            app.ShowInformation(
                "Program arka planda çalışmaya devam ediyor.",
                "Yeni bildirimlerden haberdar edileceksiniz.");

            Hide();
        }

        private async Task RefreshNotificationsAsync()
        {
            Notifications = await DHClient.GetNotificationsAsync();
        }
        #endregion Methods
    }
}
