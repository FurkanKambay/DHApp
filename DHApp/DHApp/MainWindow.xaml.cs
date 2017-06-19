using DHApp.Properties;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
//using WinForms = System.Windows.Forms;

namespace DHApp
{
    public partial class MainWindow : Window
    {
        //private WinForms.NotifyIcon notifyIcon;
        private Timer timer = new Timer(1000 * 30 * 1);

        public MainWindow()
        {
            InitializeComponent();

            timer.Elapsed += Timer_Elapsed;

            DHClient.Login += async cookie =>
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

            //notifyIcon = new WinForms.NotifyIcon
            //{
            //    Icon = new System.Drawing.Icon("dh.ico"),
            //    ContextMenu = new WinForms.ContextMenu(),
            //    Visible = true
            //};
            //notifyIcon.MouseDown += Notifier_MouseDown;

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
                Progress.Visibility = Visibility.Visible;

                await task();

                LogoutButton.IsEnabled = true;
                IgnoreButton.IsEnabled = true;
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
            foreach (FrameworkElement item in RootPanel.Children)
                item.Visibility = item.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        }
    }
}
