using DHApp.Properties;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using WinForms = System.Windows.Forms;

namespace DHApp
{
    public partial class MainWindow : Window
    {
        private WinForms.NotifyIcon trayIcon;
        private Timer timer = new Timer(1000 * 10 * 1);

        public MainWindow()
        {
            InitializeComponent();

            TitleGrid.MouseLeftButtonDown += (_, __) => DragMove();

            TitleGrid.Visibility
                = UsernameText.Visibility
                = IgnoreButton.Visibility
                = LogoutButton.Visibility
                = Progress.Visibility
                = NotificationList.Visibility
                = Visibility.Collapsed;

            timer.Elapsed += Timer_Elapsed;

#pragma warning disable RECS0165
            DHClient.Login += async cookie =>
#pragma warning restore RECS0165
            {
                Settings.Default.Cookie = cookie;
                Settings.Default.Save();

                Show();
                ToggleVisibilities();
                UsernameText.Text = DHClient.Username;

                await ShowProgress(async () => NotificationList.ItemsSource = await DHClient.GetNotificationsAsync());

                timer.Start();
            };

            DHClient.Logout += () =>
            {
                timer.Stop();
                Settings.Default.Cookie = null;
                Settings.Default.Save();

                ToggleVisibilities();
                UsernameText.Text = null;
                NotificationList.ItemsSource = null;
            };

            trayIcon = new WinForms.NotifyIcon
            {
                Icon = new System.Drawing.Icon("dhlogo.jpg"), //UNDONE: not found. .ico?
                ContextMenu = new WinForms.ContextMenu(),
                Visible = true
            };
            //trayIcon.MouseDown += Notifier_MouseDown;

        }

        private async void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            await ShowProgress(async () => NotificationList.ItemsSource = await DHClient.GetNotificationsAsync());
        }

        private async void IgnoreButton_Click(object sender, RoutedEventArgs e)
        {
            await ShowProgress(async () =>
            {
                await DHClient.IgnoreNotificationsAsync();
                NotificationList.ItemsSource = null;
            });
        }

        private async void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            await ShowProgress(async () => await DHClient.LogoutAsync());
        }

        private void NotificationList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Process.Start(((DHNotification)NotificationList.SelectedItem).Url);
        }

        private async Task ShowProgress(Func<Task> task)
        {
            await Dispatcher.InvokeAsync(async () =>
            {
                LogoutButton.IsEnabled = false;
                IgnoreButton.IsEnabled = false;
                NotificationList.IsEnabled = false;
                Progress.Visibility = Visibility.Visible;

                await task();

                LogoutButton.IsEnabled = true;
                IgnoreButton.IsEnabled = true;
                NotificationList.IsEnabled = true;
                Progress.Visibility = Visibility.Collapsed;
            });
        }

        private void TryLogin(object sender, RoutedEventArgs e)
        {
            if (!DHClient.LoginWithCookie(Settings.Default.Cookie))
            {
                Hide();
                new LoginWindow().ShowDialog();
                Show();
            }
        }

        private void ToggleVisibilities()
        {
#pragma warning disable IDE0007
            foreach (FrameworkElement item in RootPanel.Children)
#pragma warning restore IDE0007
                item.Visibility = item.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }
    }
}
