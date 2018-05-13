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
        private List<UsageWithDate> detailUsage;
        private Network connectedNetwork;
        private Account currentAccount;
        private bool isOnline = false;
        private Status connectionStatus;

        public List<UsageWithDate> DetailUsage { get => detailUsage; set => detailUsage = value; }
        public Status ConnectionStatus { get => connectionStatus; set => connectionStatus = value; }

        public GeneralPage()
        {
            this.InitializeComponent();

            DataContext = this;
            ConnectionStatus = new Status();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            /// Network
            GetCurrentNetwork();

            /// Account
            currentAccount = App.Accounts.FirstOrDefault();

            if (connectedNetwork != null)
            {
                /// Online status
                isOnline = NetHelper.IsOnline();
                if (isOnline)
                {
                    var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
                    RadialProgressBar.Visibility = Visibility.Visible;
                    RadialProgressBar.Value = 180;
                    LoginButton.Content = resourceLoader.GetString("Online");
                }

                /// Load from the local storage.
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

                /// Login
                await LoginNetworkIfFavoriteAsync(connectedNetwork.Ssid);

                /// Usage, username, balance and session
                await GetUsageChartSourceAsync();
                await GetConnectionStatusAsync();
            }
        }

        private async Task GetConnectionStatusAsync()
        {
            if (isOnline)
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

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            if (isOnline == false)
            {
                ProgressRing.IsActive = true;

                await Task.Delay(1000);
                var response = await NetHelper.LoginAsync(currentAccount.Username, currentAccount.Password);
                if (response == "Login is successful.")
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
                else if (response == "IP has been online, please logout.")
                {
                    isOnline = true;
                    ProgressRing.IsActive = false;
                    RadialProgressBar.Visibility = Visibility.Visible;
                    RadialProgressBar.Value = 180;
                    var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
                    LoginButton.Content = resourceLoader.GetString("Online");
                }
                else if (response == "E2532: The two authentication interval cannot be less than 3 seconds.")
                {
                    await Task.Delay(3000);
                    if (await NetHelper.LoginAsync(currentAccount.Username, currentAccount.Password) == "Login is successful.")
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
                }
                else if (response == "E2553: Password is error.")
                {
                    App.Accounts.RemoveAt(0);
                    var localHelper = new LocalObjectStorageHelper();
                    await localHelper.SaveFileAsync("Accounts", App.Accounts);

                    var rootFrame = Window.Current.Content as Frame;
                    rootFrame.Navigate(typeof(WelcomePage));
                }

                ProgressRing.IsActive = false;
            }
            else
            {
                RadialProgressBar.FlowDirection = FlowDirection.RightToLeft;
                if (this.Resources["RadialProgressBarBecomeZero"] is Storyboard radialProgressBarBecomeZero)
                {
                    radialProgressBarBecomeZero.Begin();
                }
                ProgressRing.IsActive = true;
                await Task.Delay(1500);

                if (await NetHelper.LogoutAsync() == "Logout is successful.")
                {
                    ProgressRing.IsActive = false;
                    RadialProgressBar.Visibility = Visibility.Collapsed;
                    isOnline = false;
                    var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
                    LoginButton.Content = resourceLoader.GetString("Offline");
                }
            }
        }

        public async Task LoginNetworkIfFavoriteAsync(string ssid)
        {
            var isWired = !Windows.Networking.Connectivity.NetworkInformation.GetInternetConnectionProfile().IsWlanConnectionProfile && !Windows.Networking.Connectivity.NetworkInformation.GetInternetConnectionProfile().IsWwanConnectionProfile;
            if (!isWired && App.FavoriteNetworks.Where(u => u.Ssid == ssid).Count() != 0 || ssid.Contains("Tsinghua"))
            {
                var response = await NetHelper.LoginAsync(currentAccount.Username, currentAccount.Password);
                if (response == "Login is successful.")
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
                else if (response == "IP has been online, please logout.")
                {
                    isOnline = true;
                    ProgressRing.IsActive = false;
                    RadialProgressBar.Visibility = Visibility.Visible;
                    RadialProgressBar.Value = 180;
                    var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
                    LoginButton.Content = resourceLoader.GetString("Online");
                }
                else if (response == "E2532: The two authentication interval cannot be less than 3 seconds.")
                {
                    await Task.Delay(3000);
                    if (await NetHelper.LoginAsync(currentAccount.Username, currentAccount.Password) == "Login is successful.")
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
                }
                else if (response == "E2553: Password is error.")
                {
                    App.Accounts.RemoveAt(0);
                    var localHelper = new LocalObjectStorageHelper();
                    await localHelper.SaveFileAsync("Accounts", App.Accounts);

                    var rootFrame = Window.Current.Content as Frame;
                    rootFrame.Navigate(typeof(WelcomePage));
                }
            }
            else if (isWired)
            {
                var credential = CredentialHelper.GetCredentialFromLocker(currentAccount.Username);
                if (credential != null)
                {
                    credential.RetrievePassword();
                }
                else
                {
                    var rootFrame = Window.Current.Content as Frame;
                    rootFrame.Navigate(typeof(WelcomePage));
                    return;
                }

                var response = await AuthHelper.LoginAsync(4, currentAccount.Username, credential.Password);
                if (response.Contains("login_ok"))
                {
                    await AuthHelper.LoginAsync(6, currentAccount.Username, credential.Password);

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
                else if (response == "ip_already_online_error")
                {
                    isOnline = true;
                    ProgressRing.IsActive = false;
                    RadialProgressBar.Visibility = Visibility.Visible;
                    RadialProgressBar.Value = 180;
                    var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
                    LoginButton.Content = resourceLoader.GetString("Online");
                }
                else if (response == "login_error#E2532: The two authentication interval cannot be less than 10 seconds.")
                {
                    await Task.Delay(3000);
                    if ((await NetHelper.LoginAsync(currentAccount.Username, currentAccount.Password)).Contains("login_ok"))
                    {
                        await AuthHelper.LoginAsync(6, currentAccount.Username, credential.Password);

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
                }
                else if (response == "SRun Auth Server")
                {
                    App.Accounts.RemoveAt(0);
                    CredentialHelper.RemoveAccount(currentAccount.Username);

                    var localHelper = new LocalObjectStorageHelper();
                    await localHelper.SaveFileAsync("Accounts", App.Accounts);

                    var rootFrame = Window.Current.Content as Frame;
                    rootFrame.Navigate(typeof(WelcomePage));
                }
            }
            else
            {
                var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
                Notification.Show(ssid + ' ' + resourceLoader.GetString("AddFavoritesSuggestion"), 1000 * 10);
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

            if (isOnline)
            {
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
    }
}
