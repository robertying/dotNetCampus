using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using WinRTXamlToolkit.Controls.DataVisualization.Charting;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media.Animation;
using Microsoft.Toolkit.Uwp.Helpers;

namespace CampusNet
{
    public sealed partial class GeneralPage : Page
    {
        private Network connectedNetwork;
        private Account currentAccount;
        private bool isOnline = false;

        public List<UsageWithDate> DetailUsage { get; set; }
        public Status ConnectionStatus { get; set; }

        public GeneralPage()
        {
            this.InitializeComponent();

            DataContext = this;
            ConnectionStatus = new Status();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            GetCurrentNetwork();
            currentAccount = App.Accounts.FirstOrDefault();
            isOnline = await NetHelper.IsOnline();
            var isCampus = await NetHelper.IsCampus();

            if (connectedNetwork != null && isCampus)
            {
                if (isOnline)
                {
                    SetLoginButton();
                    await LoadCachedStatusAsync();
                }

                await LoginNetworkIfFavoriteAsync();

                await GetConnectionStatusAsync();
                await GetUsageChartSourceAsync();
            }
        }

        private void GetCurrentNetwork()
        {
            var connectedProfile = Windows.Networking.Connectivity.NetworkInformation.GetInternetConnectionProfile();
            if (connectedProfile != null)
            {
                connectedNetwork = new Network
                {
                    Ssid = connectedProfile.ProfileName
                };
                ConnectionStatus.Network = connectedNetwork.Ssid;
            }
            else
            {
                var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
                NetworkButton.Content = resourceLoader.GetString("Disconnected");
            }
        }

        private void SetLoginButton()
        {
            isOnline = true;
            ProgressRing.IsActive = false;
            var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
            RadialProgressBar.Visibility = Visibility.Visible;
            RadialProgressBar.Value = 180;
            LoginButton.Content = resourceLoader.GetString("Online");
        }

        private void SetLoginButtonAnimation()
        {
            isOnline = true;
            ProgressRing.IsActive = false;
            RadialProgressBar.Visibility = Visibility.Visible;
            RadialProgressBar.FlowDirection = FlowDirection.LeftToRight;
            if (this.Resources["RadialProgressBarBecomeFull"] is Storyboard radialProgressBarBecomeFull)
            {
                radialProgressBarBecomeFull.Begin();
            }
            var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
            LoginButton.Content = resourceLoader.GetString("Online");
        }

        private async Task LoadCachedStatusAsync()
        {
            var localHelper = new LocalObjectStorageHelper();
            Status status = null;

            if (await localHelper.FileExistsAsync("ConnectionStatus"))
            {
                status = await localHelper.ReadFileAsync<Status>("ConnectionStatus");
            }
            if (status != null)
            {
                ConnectionStatus.Usage = status.Usage;
                ConnectionStatus.Balance = status.Balance;
                ConnectionStatus.Username = status.Username;
                ConnectionStatus.Session = status.Session;
            }

            if (await localHelper.FileExistsAsync("DetailUsage"))
            {
                DetailUsage = await localHelper.ReadFileAsync<List<UsageWithDate>>("DetailUsage");
            }
        }

        private async Task LoginNetworkIfFavoriteAsync()
        {
            var isWired = !Windows.Networking.Connectivity.NetworkInformation.GetInternetConnectionProfile().IsWlanConnectionProfile && !Windows.Networking.Connectivity.NetworkInformation.GetInternetConnectionProfile().IsWwanConnectionProfile;

            if (isWired)
            {
                var credential = CredentialHelper.GetCredentialFromLocker(currentAccount.Username);
                if (credential != null)
                {
                    credential.RetrievePassword();
                }
                else
                {
                    App.Accounts.Remove(currentAccount);

                    var rootFrame = Window.Current.Content as Frame;
                    rootFrame.Navigate(typeof(WelcomePage));

                    return;
                }

                await WiredLogin(credential);
            }
            else if (!isWired && App.FavoriteNetworks.Where(u => u.Ssid == connectedNetwork.Ssid).Count() != 0 || connectedNetwork.Ssid.Contains("Tsinghua"))
            {
                await WirelessLogin();
            }
            else
            {
                var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
                Notification.Show(connectedNetwork.Ssid + ' ' + resourceLoader.GetString("AddFavoritesSuggestion"), 1000 * 10);
            }
        }

        private async Task WirelessLogin()
        {
            var response = await NetHelper.LoginAsync(currentAccount.Username, currentAccount.Password);

            if (response == "Login is successful.")
            {
                SetLoginButtonAnimation();
            }
            else if (response == "IP has been online, please logout.")
            {
                SetLoginButton();
            }
            else if (response == "E2532: The two authentication interval cannot be less than 3 seconds.")
            {
                await Task.Delay(3500);
                await LoginNetworkIfFavoriteAsync();
            }
            else if (response == "E2553: Password is error.")
            {
                CredentialHelper.RemoveAccount(App.Accounts[0].Username);

                App.Accounts.RemoveAt(0);
                var localHelper = new LocalObjectStorageHelper();
                await localHelper.SaveFileAsync("Accounts", App.Accounts);

                var rootFrame = Window.Current.Content as Frame;
                rootFrame.Navigate(typeof(WelcomePage));
            }
        }

        private async Task WiredLogin(Windows.Security.Credentials.PasswordCredential credential)
        {
            var response = await AuthHelper.LoginAsync(4, currentAccount.Username, credential.Password);

            if (response.Contains("login_ok"))
            {
                SetLoginButtonAnimation();
                await GetConnectionStatusAsync();

                if ((await AuthHelper.LoginAsync(6, currentAccount.Username, credential.Password)).Contains("login_error"))
                {
                    await Task.Delay(10100);
                    await AuthHelper.LoginAsync(6, currentAccount.Username, credential.Password);
                }
            }
            else if (response == "ip_already_online_error")
            {
                SetLoginButton();
            }
            else if (response.Contains("login_error"))
            {
                await Task.Delay(10100);
                await LoginNetworkIfFavoriteAsync();
            }
            else
            {
                CredentialHelper.RemoveAccount(App.Accounts[0].Username);

                App.Accounts.RemoveAt(0);
                var localHelper = new LocalObjectStorageHelper();
                await localHelper.SaveFileAsync("Accounts", App.Accounts);

                var rootFrame = Window.Current.Content as Frame;
                rootFrame.Navigate(typeof(WelcomePage));
            }
        }

        private async Task GetConnectionStatusAsync()
        {
            var status = await NetHelper.GetStatusAsync();
            if (status != null)
            {
                ConnectionStatus.Usage = Utility.GetUsageDescription((long)status["total_usage"]);
                ConnectionStatus.Balance = Utility.GetBalanceDescription(Convert.ToDouble(status["balance"]));
                ConnectionStatus.Username = status["username"] as string;
                var sessionNumber = await UseregHelper.GetSessionNumberAsync(currentAccount.Username, currentAccount.Password);
                ConnectionStatus.Session = sessionNumber == -1 ? "--" : sessionNumber.ToString();

                var localHelper = new LocalObjectStorageHelper();
                await localHelper.SaveFileAsync("ConnectionStatus", ConnectionStatus);
            }
        }

        private async Task GetUsageChartSourceAsync()
        {
            if (DetailUsage != null)
            {
                (DetailUsageChart.Series[0] as LineSeries).ItemsSource = DetailUsage;
                SetChartAxis();
                if (this.Resources["FadeIn_Chart"] is Storyboard fadeIn) fadeIn.Begin();
            }

            await UseregHelper.LoginAsync(currentAccount.Username, currentAccount.Password);
            DetailUsage = await UseregHelper.GetDetailUsageForChart();

            if (DetailUsage != null)
            {
                var localHelper = new LocalObjectStorageHelper();
                await localHelper.SaveFileAsync("DetailUsage", DetailUsage);

                (DetailUsageChart.Series[0] as LineSeries).ItemsSource = DetailUsage;
                SetChartAxis();
            }

        }

        private void SetChartAxis()
        {
            ((LineSeries)DetailUsageChart.Series[0]).IndependentAxis = new LinearAxis()
            {
                Maximum = 30,
                Location = AxisLocation.Bottom,
                Orientation = AxisOrientation.X,
                Interval = 5,
                ShowGridLines = false,
            };

            if (DetailUsage.Count() == 0 || (DetailUsage.Count() != 0 && DetailUsage.Last().Usage <= 25))
            {
                ((LineSeries)DetailUsageChart.Series[0]).DependentRangeAxis = new LinearAxis()
                {
                    Minimum = 0,
                    Maximum = 25,
                    Location = AxisLocation.Left,
                    Orientation = AxisOrientation.Y,
                    Interval = 5,
                    ShowGridLines = true,
                };
            }
            else if (DetailUsage.Last().Usage <= 50 && DetailUsage.Last().Usage > 25)
            {
                ((LineSeries)DetailUsageChart.Series[0]).DependentRangeAxis = new LinearAxis()
                {
                    Minimum = 0,
                    Maximum = 50,
                    Location = AxisLocation.Left,
                    Orientation = AxisOrientation.Y,
                    Interval = 10,
                    ShowGridLines = true,
                };
            }
            else
            {
                ((LineSeries)DetailUsageChart.Series[0]).DependentRangeAxis = new LinearAxis()
                {
                    Minimum = 0,
                    Location = AxisLocation.Left,
                    Orientation = AxisOrientation.Y,
                    Interval = 10,
                    ShowGridLines = true,
                };
            }
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            var isWired = !Windows.Networking.Connectivity.NetworkInformation.GetInternetConnectionProfile().IsWlanConnectionProfile && !Windows.Networking.Connectivity.NetworkInformation.GetInternetConnectionProfile().IsWwanConnectionProfile;

            if (isOnline == false)
            {
                ProgressRing.IsActive = true;
                await Task.Delay(1000);

                if (!isWired)
                {
                    await WirelessLogin();
                }
                else
                {
                    var credential = CredentialHelper.GetCredentialFromLocker(currentAccount.Username);
                    if (credential != null)
                    {
                        credential.RetrievePassword();
                    }
                    else
                    {
                        App.Accounts.Remove(currentAccount);

                        var rootFrame = Window.Current.Content as Frame;
                        rootFrame.Navigate(typeof(WelcomePage));

                        return;
                    }

                    await WiredLogin(credential);
                }

                await GetConnectionStatusAsync();
                await GetUsageChartSourceAsync();
            }
            else
            {
                SetLogoutButtonAnimation();

                ProgressRing.IsActive = true;
                await Task.Delay(1500);

                if (!isWired)
                {
                    await WirelessLogout();
                }
                else
                {
                    var credential = CredentialHelper.GetCredentialFromLocker(currentAccount.Username);
                    if (credential != null)
                    {
                        credential.RetrievePassword();
                    }
                    else
                    {
                        App.Accounts.Remove(currentAccount);

                        var rootFrame = Window.Current.Content as Frame;
                        rootFrame.Navigate(typeof(WelcomePage));

                        return;
                    }

                    await WiredLogout(credential);
                }
            }
        }

        private void SetLogoutButton()
        {
            ProgressRing.IsActive = false;
            RadialProgressBar.Visibility = Visibility.Collapsed;
            isOnline = false;
            var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
            LoginButton.Content = resourceLoader.GetString("Offline");
        }

        private async void SetLogoutButtonAnimation()
        {
            ProgressRing.IsActive = false;
            RadialProgressBar.FlowDirection = FlowDirection.RightToLeft;
            if (this.Resources["RadialProgressBarBecomeZero"] is Storyboard radialProgressBarBecomeZero)
            {
                radialProgressBarBecomeZero.Begin();
            }
            isOnline = false;
            var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
            await Task.Delay(1500);
            LoginButton.Content = resourceLoader.GetString("Offline");
        }

        private async Task WirelessLogout()
        {
            var response = await NetHelper.LogoutAsync();

            if (response == "Logout is successful.")
            {
                SetLogoutButtonAnimation();
            }
            else
            {
                isOnline = await NetHelper.IsOnline();

                if (isOnline)
                {
                    SetLoginButton();
                }
                else
                {
                    SetLogoutButton();
                }
            }
        }

        private async Task WiredLogout(Windows.Security.Credentials.PasswordCredential credential)
        {
            var response = await AuthHelper.LogoutAsync(4, currentAccount.Username, "111");

            if (response.Contains("logout_ok"))
            {
                SetLogoutButtonAnimation();
            }
            else
            {
                isOnline = await NetHelper.IsOnline();

                if (isOnline)
                {
                    SetLoginButton();
                }
                else
                {
                    SetLogoutButton();
                }
            }
        }
    }
}
