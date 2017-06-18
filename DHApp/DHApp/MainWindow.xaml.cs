using System;
using System.Timers;
using System.Windows;
//using WinForms = System.Windows.Forms;

namespace DHApp
{
    public partial class MainWindow : Window
    {
        //private WinForms.NotifyIcon notifyIcon;
        private Timer timer = new Timer(/*ms*/ 1000 * /*s*/ 60 * /*m*/ 0.5);

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;

            timer.Elapsed += Timer_Elapsed;
            timer.Start();

            //notifyIcon = new WinForms.NotifyIcon
            //{
            //    Icon = new System.Drawing.Icon("dh.ico"),
            //    ContextMenu=new WinForms.ContextMenu(),
            //    Visible = true
            //};
            //notifyIcon.MouseDown += Notifier_MouseDown;
        }

        //private void Notifier_MouseDown(object sender, WinForms.MouseEventArgs e)
        //{

        //}

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var loginWindow = new LoginWindow { Owner = this };

            if (loginWindow.ShowDialog().Value)
            {
                LoginButton.Visibility = Visibility.Collapsed;
                NotificationList.ItemsSource = await DHClient.GetNotificationsAsync();
                var cookies = DHClient.Cookies.GetCookies(new Uri(".donanimhaber.com/"));
            }
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            //TODO: get new notifications
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var loginWindow = new LoginWindow { Owner = this };
            loginWindow.ShowDialog();
        }
    }
}
