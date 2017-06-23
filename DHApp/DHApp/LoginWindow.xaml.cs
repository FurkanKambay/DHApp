using System.Windows;
using System.Windows.Media;

namespace DHApp
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();

            TitleGrid.MouseLeftButtonDown += (_, __) => DragMove();

            Loaded += async (s, a) => TryLogin(await DHClient.LogInWithCookieAsync(Properties.Settings.Default.Cookie));
            LoginButton.Click += async (_, __) => TryLogin(await DHClient.LogInAsync(LoginNameTB.Text, PasswordPB.Password));

            Box_TextChanged(this, new RoutedEventArgs()); //UNDONE: LoginButton is still enabled
        }

        private void TryLogin(bool success)
        {
            LoginNameTB.IsEnabled = PasswordPB.IsEnabled = LoginButton.IsEnabled = false;

            if (success)
                DialogResult = true;
            else
            {
                LoginNameTB.IsEnabled = PasswordPB.IsEnabled = LoginButton.IsEnabled = true;
                PasswordPB.Password = string.Empty;
            }
        }

        private void Box_TextChanged(object sender, RoutedEventArgs e)
        {
            bool isNameOk = !string.IsNullOrWhiteSpace(LoginNameTB.Text);
            bool isPassOk = !string.IsNullOrWhiteSpace(PasswordPB.Password);

            LoginButton.IsEnabled = isNameOk && isPassOk;

            var orange = new SolidColorBrush(Colors.DarkOrange);
            var red = new SolidColorBrush(Colors.Red);
            LoginNameTB.BorderBrush = isNameOk ? orange : red;
            PasswordPB.BorderBrush = isPassOk ? orange : red;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
