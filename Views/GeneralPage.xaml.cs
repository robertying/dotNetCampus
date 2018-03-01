using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using WinRTXamlToolkit.Controls.DataVisualization.Charting;
using Microsoft.Toolkit.Uwp.UI;
using Microsoft.Toolkit.Uwp.UI.Extensions;
using Windows.Devices.WiFi;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI;
using Microsoft.Toolkit.Uwp.UI.Animations.Behaviors;
using Microsoft.Xaml.Interactivity;
using Microsoft.Toolkit.Uwp.UI.Animations;

namespace CampusNet
{
    public sealed partial class GeneralPage : Page
    {
        private List<UsageWithDate> detailUsage;
        private WiFiAdapter wifiAdapter;
        private string savedProfileName = null;
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
            await GetCurrentNetworkAsync();

            /// Account
            currentAccount = App.Accounts.First();

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
                var status = await DataStorage.GetFileAsync<Status>("ConnectionStatus");
                if (status != null)
                {
                    ConnectionStatus.Usage = status.Usage;
                    ConnectionStatus.Balance = status.Balance;
                    ConnectionStatus.Username = status.Username;
                    ConnectionStatus.Session = status.Session;
                }
                DetailUsage = await DataStorage.GetFileAsync<List<UsageWithDate>>("DetailUsage");

                /// Login
                await LoginNetworkIfFavoriteAsync(connectedNetwork.Ssid);

                /// Usage, username, balance and session
                await GetUsageChartSourceAsync();
                await GetConnectionStatusAsync();
            }
        }

        private async Task GetConnectionStatusAsync()
        {
            if (ConnectionStatus == null) ConnectionStatus = new Status();
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
                    DataStorage.SaveFileAsync("ConnectionStatus", ConnectionStatus);
                }
            }
        }

        private async Task UpdateConnectivityStatusAsync()
        {
            var connectedProfile = await wifiAdapter.NetworkAdapter.GetConnectedProfileAsync();
            if (connectedProfile != null)
            {
                connectedNetwork = new Network
                {
                    Ssid = connectedProfile.ProfileName
                };
            }

            if (connectedProfile != null && connectedNetwork.Ssid != savedProfileName)
            {
                savedProfileName = connectedNetwork.Ssid;
                isOnline = false;
                NetworkButton.Content = connectedNetwork.Ssid;
            }
            else if (connectedProfile == null && savedProfileName != null)
            {
                savedProfileName = null;
                var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
                NetworkButton.Content = resourceLoader.GetString("Disconnected");
            }
        }

        private async Task GetCurrentNetworkAsync()
        {
            var access = await WiFiAdapter.RequestAccessAsync();
            if (access != WiFiAccessStatus.Allowed)
            {
                var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
                NetworkButton.Content = resourceLoader.GetString("Disconnected");
            }
            else
            {
                var result = await Windows.Devices.Enumeration.DeviceInformation.FindAllAsync(WiFiAdapter.GetDeviceSelector());
                if (result.Count >= 1)
                {
                    wifiAdapter = await WiFiAdapter.FromIdAsync(result[0].Id);
                    await UpdateConnectivityStatusAsync();
                }
                else
                {
                    var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
                    NetworkButton.Content = resourceLoader.GetString("Disconnected");
                }
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
            if (App.FavoriteNetworks.Where(u => u.Ssid == ssid).Count() != 0 ||
                ssid == "Tsinghua" ||
                ssid == "Tsinghua-5G")
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
                    DataStorage.SaveFileAsync("DetailUsage", DetailUsage);

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
