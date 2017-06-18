using System;
using System.Timers;
using System.Windows;
//using WinForms = System.Windows.Forms;

namespace DHApp
{
    public partial class MainWindow : Window
    {
        //private WinForms.NotifyIcon notifyIcon;
        //private Timer timer = new Timer(/*ms*/ 1000 * /*s*/ 60 * /*m*/ 0.5);

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;

            //TODO: Move Login dialog to MainWindow

            //timer.Elapsed += Timer_Elapsed;
            //timer.Start();

            //notifyIcon = new WinForms.NotifyIcon
            //{
            //    Icon = new System.Drawing.Icon("dh.ico"),
            //    ContextMenu = new WinForms.ContextMenu(),
            //    Visible = true
            //};
            //notifyIcon.MouseDown += Notifier_MouseDown;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var loginWindow = new LoginWindow { Owner = this };

            if (loginWindow.ShowDialog().Value)
            {
                LoginButton.Visibility = Visibility.Collapsed;
                NotificationList.ItemsSource = await DHClient.GetNotificationsAsync();
                //var cookies = DHClient.Cookies.GetCookies(new Uri(".donanimhaber.com/"));
            }
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            var loginWindow = new LoginWindow { Owner = this };
            loginWindow.ShowDialog();
        }
    }
}
