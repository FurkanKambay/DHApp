using DHApp.Properties;
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

            LoginNameTB.Text = "Microsoft Specialist";
            PasswordPB.Password = string.Empty;
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            LoginNameTB.IsEnabled = PasswordPB.IsEnabled = LoginButton.IsEnabled = false;

            if (await DHClient.LoginAsync(LoginNameTB.Text, PasswordPB.Password))
                DialogResult = true;
            else
                LoginNameTB.IsEnabled = PasswordPB.IsEnabled = LoginButton.IsEnabled = true;
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

        private void Close_Click(object sender, RoutedEventArgs e) =>
            DialogResult = false;
    }
}
