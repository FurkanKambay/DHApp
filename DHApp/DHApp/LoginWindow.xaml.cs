using System;
using System.Windows;
using System.Windows.Media;

namespace DHApp
{
    public partial class LoginWindow : Window
    {
        //public bool Result { get; private set; }

        public LoginWindow()
        {
            InitializeComponent();
            LoginNameTB.Text = "Microsoft Specialist";
            PasswordPB.Password = string.Empty;
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            LoginButton.IsEnabled = false;
            DialogResult = await DHClient.LoginAsync(LoginNameTB.Text, PasswordPB.Password);

            if (!DialogResult.Value)
                LoginButton.IsEnabled = true;

            //else
            //    Close();
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
    }
}
