using DHApp.Properties;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace DHApp.Views
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        #region Properties and Fields
        private static App app = (App)Application.Current;

        private DHNotification[] notifications;
        public DHNotification[] Notifications
        {
            get => notifications ?? Array.Empty<DHNotification>();
            set
            {
                if (notifications != value)
                {
                    notifications = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool canOperate;
        public bool CanOperate
        {
            get => canOperate;
            set
            {
                canOperate = value;
                OnPropertyChanged();
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName]string propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public event PropertyChangedEventHandler PropertyChanged;
        #endregion Properties and Fields

        public MainWindow()
        {
            InitializeComponent();

            app.MainWindow = this;
            DataContext = this;

            TitleGrid.MouseLeftButtonDown += (_, __) => DragMove();

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

            if (!string.IsNullOrWhiteSpace(await DHClient.GetAvatarUrlAsync()))
                Avatar.Source = new BitmapImage(new Uri(DHClient.AvatarUrl));

            await RefreshNotificationsAsync();
        }

        private void OnDHLogout()
        {
            app.StopBackgroundWorker();

            Settings.Default.Cookie = null;
            Settings.Default.Save();

            Notifications = null;
            ShowLoginWindow();
        }
        #endregion DH Events

        #region Click Events
        private async void RefreshClicked(object sender, RoutedEventArgs e)
        {
            await RefreshNotificationsAsync();
        }

        private async void IgnoreClicked(object sender, RoutedEventArgs e)
        {
            CanOperate = false;

            await DHClient.IgnoreNotificationsAsync();
            Notifications = null;

            CanOperate = true;
        }

        private async void LogOutClicked(object sender, RoutedEventArgs e)
        {
            CanOperate = false;
            await DHClient.LogOutAsync();
        }

        private void CloseClicked(object sender, RoutedEventArgs e)
        {
            Hide();
            app.StartBackgroundWorker();
            app.ShowMessage("Program arka planda çalışmaya devam ediyor. Yeni bildirimlerden haberdar edileceksiniz.");
        }

        private void NotificationClicked(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is ListViewItem item && item.IsSelected && item.Content is DHNotification notification)
                Process.Start(notification.Url);
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

        private async Task RefreshNotificationsAsync()
        {
            CanOperate = false;
            Notifications = await DHClient.GetNotificationsAsync();
            CanOperate = true;
        }
        #endregion Methods
    }
}
