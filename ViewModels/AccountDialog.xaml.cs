using Microsoft.Toolkit.Uwp.Helpers;
using System;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace CampusNet
{
    public sealed partial class AccountDialog : ContentDialog
    {
        private bool validUsernameFlag = false;
        private bool validPasswordFlag = false;
        private Account oldAccount;

        public Account OldAccount { get => oldAccount; set => oldAccount = value; }

        public AccountDialog()
        {
            this.InitializeComponent();
        }

        private async void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            AccountDialog accountDialog = sender as AccountDialog;
            var username = accountDialog.UsernameTextBox.Text;
            var password = accountDialog.PasswordBox.Password;

            Account newAccount = new Account
            {
                Username = username,
                Password = password
            };

            var duplicatedAccounts = App.Accounts.Where(u => u.Username == newAccount.Username).ToList();
            if (duplicatedAccounts.Count > 0)
            {
                duplicatedAccounts.Single().Username = newAccount.Username;
                duplicatedAccounts.Single().Password = newAccount.Password;
            }
            else
            {
                if (await UseregHelper.LoginAsync(newAccount.Username, newAccount.Password) == "ok")
                {
                    App.Accounts.Add(newAccount);

                    var localHelper = new LocalObjectStorageHelper();
                    await localHelper.SaveFileAsync("Accounts", App.Accounts);
                }
                else
                {
                    var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
                    ContentDialog contentDialog = new ContentDialog()
                    {
                        Title = resourceLoader.GetString("Error"),
                        Content = resourceLoader.GetString("AccountValidationFail"),
                        PrimaryButtonText = resourceLoader.GetString("Ok")
                    };
                    await contentDialog.ShowAsync();
                }
            }
        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
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
                IsPrimaryButtonEnabled = true;
            }
            else
            {
                IsPrimaryButtonEnabled = false;
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
                IsPrimaryButtonEnabled = true;
            }
            else
            {
                IsPrimaryButtonEnabled = false;
            }
        }
    }
}
