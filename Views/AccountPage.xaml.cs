using Microsoft.Toolkit.Uwp.Helpers;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace CampusNet
{
    public sealed partial class AccountPage : Page
    {
        private Account currentAccount;
        private ObservableCollection<Session> sessionsForCurrentAccount;

        public AccountPage()
        {
            this.InitializeComponent();
            DataContext = this;
            Accounts.CollectionChanged += Accounts_CollectionChanged;
            SessionsForCurrentAccount = new ObservableCollection<Session>();
        }

        private async void Accounts_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            CurrentAccount = Accounts.FirstOrDefault();
            if (CurrentAccount == null) CurrentAccount = new Account();

            await GetAllAccountsInfo();

            CurrentUsernameTextBlock.Text = CurrentAccount.Username ?? "";
            CurrentUsageTextBlock.Text = CurrentAccount.Usage ?? "";
            CurrentBalanceTextBlock.Text = CurrentAccount.Balance ?? "";

            SessionsForCurrentAccount.Clear();
            if (CurrentAccount.Sessions != null)
            {
                foreach (var session in CurrentAccount.Sessions)
                {
                    SessionsForCurrentAccount.Add(session);
                }
            }
        }

        public ObservableCollection<Account> Accounts
        {
            get => App.Accounts;
            set => App.Accounts = value;
        }
        public Account CurrentAccount { get => currentAccount; set => currentAccount = value; }
        public ObservableCollection<Session> SessionsForCurrentAccount { get => sessionsForCurrentAccount; set => sessionsForCurrentAccount = value; }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            CurrentAccount = Accounts.FirstOrDefault();
            await GetAllAccountsInfo();

            if (CurrentAccount.Sessions != null)
            {
                foreach (var session in CurrentAccount.Sessions)
                {
                    SessionsForCurrentAccount.Add(session);
                }
            }
        }

        private async Task GetAllAccountsInfo()
        {
            foreach (var account in Accounts.ToList())
            {
                var info = await UseregHelper.GetInfoAsync(account.Username, account.Password);
                if (info != null)
                {
                    account.Usage = Utility.GetUsageDescription(Convert.ToDouble(info["usage"] as string));
                    account.Balance = Utility.GetBalanceDescription(Convert.ToDouble(info["balance"] as string));
                    account.Sessions = info["sessions"] as ObservableCollection<Session>;
                }
            }
        }

        private async void AddAccountButton_Click(object sender, RoutedEventArgs e)
        {
            var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
            AccountDialog accountDialog = new AccountDialog
            {
                Title = resourceLoader.GetString("AddNewAccount")
            };
            var accountNumber = App.Accounts.Count();
            var result = await accountDialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                if (App.Accounts.Count() > accountNumber)
                {
                    Notification.Show(App.Accounts.Last().Username + ' ' + resourceLoader.GetString("AddAccountSuccess"), 5000);
                }
            }
        }

        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListView list = sender as ListView;
            if (e.AddedItems.Count == 1)
            {
                if (list.ContainerFromItem(e.AddedItems[0]) is ListViewItem addedItem)
                {
                    addedItem.ContentTemplate = this.Resources["List_Selected"] as DataTemplate;
                }
            }
            if (e.RemovedItems.Count == 1)
            {
                if (list.ContainerFromItem(e.RemovedItems[0]) is ListViewItem removedItem)
                {
                    removedItem.ContentTemplate = this.Resources["List_Normal"] as DataTemplate;
                }
            }
        }

        private void ListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            ListView list = sender as ListView;
            ListViewItem listItem = list.ContainerFromItem(e.ClickedItem) as ListViewItem;

            if (listItem.IsSelected)
            {
                listItem.IsSelected = false;
                list.SelectionMode = ListViewSelectionMode.None;
            }
            else
            {
                list.SelectionMode = ListViewSelectionMode.Single;
                listItem.IsSelected = true;
            }
        }

        private async void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
            if (App.Accounts.Count() == 1)
            {
                var d = new ContentDialog()
                {
                    Title = resourceLoader.GetString("Error"),
                    Content = resourceLoader.GetString("RemoveOnlyAccountFail"),
                    PrimaryButtonText = resourceLoader.GetString("Ok")
                };
                await d.ShowAsync();
            }
            else
            {
                resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
                Button button = sender as Button;
                String username = button.Tag as String;
                var dialog = new ContentDialog()
                {
                    Title = resourceLoader.GetString("Warning"),
                    Content = resourceLoader.GetString("RemoveAccountConfirm"),
                    PrimaryButtonText = resourceLoader.GetString("Yes"),
                    SecondaryButtonText = resourceLoader.GetString("No")
                };

                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    Accounts.Remove(Accounts.Where(u => u.Username == username).ToList()[0]);
                    Notification.Show(username + ' ' + resourceLoader.GetString("CurrentAccountRemoved"), 5000);

                    var localHelper = new LocalObjectStorageHelper();
                    await localHelper.SaveFileAsync("Accounts", Accounts);
                    CredentialHelper.RemoveAccount(username);
                }
            }
        }

        private void SwitchButton_Click(object sender, RoutedEventArgs e)
        {
            if (Accounts.Count <= 1)
            {
                return;
            }

            Button button = sender as Button;
            String username = button.Tag as String;

            var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
            var account = Accounts.Where(u => u.Username == username).ToList()[0];
            Accounts.Move(Accounts.IndexOf(account), 0);

            Notification.Show(username + ' ' + resourceLoader.GetString("CurrentAccountChanged"), 5000);
        }

        private async void DisconnectButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            var id = button.Tag as string;

            var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
            var dialog = new ContentDialog()
            {
                Title = resourceLoader.GetString("Warning"),
                Content = resourceLoader.GetString("DisconnectSessionConfirm"),
                PrimaryButtonText = resourceLoader.GetString("Yes"),
                SecondaryButtonText = resourceLoader.GetString("No")
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                if (await UseregHelper.LogoutAsync(CurrentAccount.Username, CurrentAccount.Password, id) == "下线请求已发送")
                {
                    var disconnectedSession = SessionsForCurrentAccount.Where(u => u.ID == id).ToList()[0];
                    SessionsForCurrentAccount.Remove(disconnectedSession);
                    Notification.Show(disconnectedSession.IP + ' ' + resourceLoader.GetString("SessionDisconnectSuccess"), 5000);
                }
            }
        }
    }
}
