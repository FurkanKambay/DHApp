using System;
using System.Windows;
using System.Windows.Media;

namespace DHApp
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();

            TitleGrid.MouseDown += (sender, e) =>
            {
                if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
                    DragMove();
            };

            LoginNameTB.Text = "Microsoft Specialist";
            PasswordPB.Password = string.Empty;
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            LoginNameTB.IsEnabled = false;
            PasswordPB.IsEnabled = false;
            LoginButton.IsEnabled = false;
            bool result = await DHClient.LoginAsync(LoginNameTB.Text, PasswordPB.Password);

            if (!result)
            {
                LoginNameTB.IsEnabled = true;
                PasswordPB.IsEnabled = true;
                LoginButton.IsEnabled = true;
            }
            else DialogResult = true;
        }

        private void Box_TextChanged(object sender, RoutedEventArgs e)
        {
            bool isNameOk = !string.IsNullOrWhiteSpace(LoginNameTB.Text);
            bool isPassOk = !string.IsNullOrWhiteSpace(PasswordPB.Password);

            LoginButton.IsEnabled = isNameOk && isPassOk;

            //BUG: is there?

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
