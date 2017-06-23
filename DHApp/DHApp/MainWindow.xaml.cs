using DHApp.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace DHApp
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private static App app = (App)Application.Current;

        private IEnumerable<DHNotification> notifications;
        public IEnumerable<DHNotification> Notifications
        {
            get => notifications;
            set
            {
                if (notifications != value)
                {
                    notifications = value;
                    RaisePropertyChanged(nameof(Notifications));
                }
            }
        }

        public MainWindow()
        {
            InitializeComponent();

            app.MainWindow = this;
            DataContext = this;

            TitleGrid.MouseLeftButtonDown += (_, __) => DragMove();

            DHClient.LoggedIn += OnDHLogin;
            DHClient.LoggedOut += OnDHLogout;
        }

        private async void OnDHLogin(string cookie)
        {
            Settings.Default.Cookie = cookie;
            Settings.Default.Save();

            UsernameText.Text = DHClient.Username;

            if (!string.IsNullOrWhiteSpace(DHClient.AvatarUrl))
                Avatar.Source = new BitmapImage(new Uri(DHClient.AvatarUrl));

            await RefreshNotificationsAsync();
            app.StartBackgroundWorker();
        }

        private void OnDHLogout()
        {
            app.StopBackgroundWorker();

            Settings.Default.Cookie = null;
            Settings.Default.Save();

            NotificationList.ItemsSource = null;
            ShowLoginWindow();
        }

        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            ShowLoginWindow();
        }

        private async void RefreshClicked(object sender, RoutedEventArgs e)
        {
            await RefreshNotificationsAsync();
        }

        private async void IgnoreClicked(object sender, RoutedEventArgs e)
        {
            await DHClient.IgnoreNotificationsAsync();
            Notifications = null;
        }

        private async void LogoutClicked(object sender, RoutedEventArgs e)
        {
            await DHClient.LogOutAsync();
        }

        private void CloseClicked(object sender, RoutedEventArgs e)
        {
            app.ShowInfo(
                "Program arka planda çalışmaya devam ediyor.",
                "Yeni bildirimlerden haberdar edileceksiniz.");
            app.StartBackgroundWorker();
            Hide();
        }

        private void NotificationClicked(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is ListViewItem item && item.IsSelected && item.Content is DHNotification notification)
            {
                System.Diagnostics.Process.Start(notification.Url);
            }
        }

        private void ShowLoginWindow()
        {
            Hide();

            if (new LoginWindow().ShowDialog().Value)
                Show();
            else
                app.Shutdown();
        }

        private async Task RefreshNotificationsAsync()
        {
            Notifications = await DHClient.GetNotificationsAsync();
        }

        protected void RaisePropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
