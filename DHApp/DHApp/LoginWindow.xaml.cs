using System.Windows;
using System.Windows.Media;

namespace DHApp
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
            DataContext = this;

            TitleGrid.MouseLeftButtonDown += (_, __) => DragMove();
            CloseButton.Click += (_, __) => DialogResult = false;

            void tryToLogIn(bool success)
            {
                if (success)
                    DialogResult = true;
                else
                {
                    LoginPanel.IsEnabled = true;
                    PasswordPB.Password = string.Empty;
                    PasswordPB.Focus();
                }
            }

            Loaded += (s, a) =>
            {
                LoginPanel.IsEnabled = false;
                tryToLogIn(DHClient.LogInWithCookie(Properties.Settings.Default.Cookie));
                UsernameTB.Focus();
            };

            LoginButton.Click += async (s, a) =>
            {
                LoginPanel.IsEnabled = false;
                tryToLogIn(await DHClient.LogInAsync(UsernameTB.Text, PasswordPB.Password));
            };

            Box_TextChanged(this, new RoutedEventArgs());
        }

        private void Box_TextChanged(object sender, RoutedEventArgs e)
        {
            bool isNameOk = !string.IsNullOrWhiteSpace(UsernameTB.Text);
            bool isPassOk = !string.IsNullOrWhiteSpace(PasswordPB.Password);

            LoginButton.IsEnabled = isNameOk && isPassOk;

            Brush getColorOf(bool condition) => condition ? (Brush)FindResource("ThemeColor") : new SolidColorBrush(Colors.Red);

            UsernameTB.BorderBrush = getColorOf(isNameOk);
            PasswordPB.BorderBrush = getColorOf(isPassOk);
        }
    }
}
