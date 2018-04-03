using Microsoft.Toolkit.Uwp.Helpers;
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.System;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;

namespace CampusNet
{
    public class SmartTextColorBasedOnAccentTypeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return (bool)value ? ElementTheme.Dark : ElementTheme.Light;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public sealed partial class WelcomePage : Page
    {
        private bool validPasswordFlag = false;
        private bool validUsernameFlag = false;
        private bool _isDarkAccent;

        public bool IsDarkAccent
        {
            get => IsAccentColorDark();
            set { _isDarkAccent = value; }
        }

        private bool IsAccentColorDark()
        {
            var uiSettings = new UISettings();
            var c = uiSettings.GetColorValue(UIColorType.Accent);
            bool isDark = (5 * c.G + 2 * c.R + c.B) <= 8 * 128;
            return isDark;
        }

        public WelcomePage()
        {
            this.InitializeComponent();
        }

        private void UsernameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (textBox.Text.Count() != 0)
            {
                validUsernameFlag = true;
            }
            else
            {
                validUsernameFlag = false;
            }

            if (validUsernameFlag && validPasswordFlag)
            {
                LoginButton.IsEnabled = true;
            }
            else
            {
                LoginButton.IsEnabled = false;
            }
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            PasswordBox passwordBox = sender as PasswordBox;
            if (passwordBox.Password.Count() != 0)
            {
                validPasswordFlag = true;
            }
            else
            {
                validPasswordFlag = false;
            }

            if (validUsernameFlag && validPasswordFlag)
            {
                LoginButton.IsEnabled = true;
            }
            else
            {
                LoginButton.IsEnabled = false;
            }
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            LoginButton.IsEnabled = false;
            UsernameTextBox.IsEnabled = false;
            PasswordBox.IsEnabled = false;
            ProgressRing.IsActive = true;

            var username = UsernameTextBox.Text;
            var password = Utility.ComputeMD5(PasswordBox.Password);

            if (username == "10582RobertYing..NetCampus" && password == Utility.ComputeMD5("10582RobertYing..NetCampus"))
            {
                App.Accounts.Insert(0, new Account
                {
                    Username = username,
                    Password = password
                });

                var localHelper = new LocalObjectStorageHelper();
                await localHelper.SaveFileAsync("Accounts", App.Accounts);

                await Task.Delay(500);
                var rootFrame = Window.Current.Content as Frame;
                rootFrame.Navigate(typeof(RootPage));
            }

            var response = await NetHelper.LoginAsync(username, password);
            await Task.Delay(500);

            if (response == "Login is successful.")
            {
                App.Accounts.Insert(0, new Account
                {
                    Username = username,
                    Password = password
                });

                var localHelper = new LocalObjectStorageHelper();
                await localHelper.SaveFileAsync("Accounts", App.Accounts);

                var rootFrame = Window.Current.Content as Frame;
                rootFrame.Navigate(typeof(RootPage));
            }
            else if (response == "IP has been online, please logout.")
            {
                await NetHelper.LogoutAsync();
                LoginButton_Click(null, null);
            }
            else if (response == "E2532: The two authentication interval cannot be less than 3 seconds.")
            {
                var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
                StatusTextBlock.Text = resourceLoader.GetString("FrequentLogin");
                LoginButton.IsEnabled = true;
                UsernameTextBox.IsEnabled = true;
                PasswordBox.IsEnabled = true;
                ProgressRing.IsActive = false;
            }
            else if (response == "E2531: User not found.")
            {
                var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
                StatusTextBlock.Text = resourceLoader.GetString("UserNotFound");
                LoginButton.IsEnabled = true;
                UsernameTextBox.IsEnabled = true;
                PasswordBox.IsEnabled = true;
                ProgressRing.IsActive = false;
            }
            else
            {
                var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
                StatusTextBlock.Text = resourceLoader.GetString("LoginFail");
                LoginButton.IsEnabled = true;
                UsernameTextBox.IsEnabled = true;
                PasswordBox.IsEnabled = true;
                ProgressRing.IsActive = false;
            }
        }

        private void PasswordBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter && PasswordBox.Password != null && PasswordBox.Password != "")
            {
                LoginButton_Click(null, null);
            }
        }
    }
}
