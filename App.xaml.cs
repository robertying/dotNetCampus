using Microsoft.Toolkit.Uwp.Helpers;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Background;
using Windows.ApplicationModel.Core;
using Windows.UI;
using Windows.UI.Notifications;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace CampusNet
{
    sealed partial class App : Application
    {
        public static ObservableCollection<Network> FavoriteNetworks { get; set; }
        public static ObservableCollection<Account> Accounts { get; set; }

        public App()
        {
            InitializeComponent();

            SetBackgroundTask();
        }

        private void CheckOnLaunch()
        {
            for (int i = Accounts.Count - 1; i >= 0; i--)
            {
                var duplicates = Accounts.Where(x => x.Username == Accounts[i].Username).ToList();
                if (duplicates.Count > 1)
                {
                    Accounts.Remove(Accounts[i]);
                }
            }
            FavoriteNetworks = new ObservableCollection<Network>(FavoriteNetworks.Distinct());
        }

        protected override async void OnLaunched(LaunchActivatedEventArgs e)
        {
            var localHelper = new LocalObjectStorageHelper();
            FavoriteNetworks = new ObservableCollection<Network>();
            Accounts = new ObservableCollection<Account>();

            if (await localHelper.FileExistsAsync("Networks"))
            {
                FavoriteNetworks = await localHelper.ReadFileAsync<ObservableCollection<Network>>("Networks");
            }
            if (FavoriteNetworks == null) FavoriteNetworks = new ObservableCollection<Network>();

            if (await localHelper.FileExistsAsync("Accounts"))
            {
                Accounts = await localHelper.ReadFileAsync<ObservableCollection<Account>>("Accounts");
            }
            if (Accounts == null) Accounts = new ObservableCollection<Account>();

            CheckOnLaunch();

            if (!(Window.Current.Content is Frame rootFrame))
            {
                rootFrame = new Frame();
                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {

                }

                Window.Current.Content = rootFrame;
            }

            if (e.PrelaunchActivated == false)
            {
                if (rootFrame.Content == null)
                {
                    if (Accounts.Count() == 0)
                    {
                        rootFrame.Navigate(typeof(WelcomePage), e.Arguments);
                    }
                    else
                    {
                        rootFrame.Navigate(typeof(RootPage), e.Arguments);
                    }
                }

                Window.Current.Activate();

                ExtendAcrylicIntoTitleBar();
            }
        }

        protected override async void OnActivated(IActivatedEventArgs e)
        {
            var localHelper = new LocalObjectStorageHelper();
            if (await localHelper.FileExistsAsync("Networks"))
            {
                App.FavoriteNetworks = await localHelper.ReadFileAsync<ObservableCollection<Network>>("Networks");
            }
            if (App.FavoriteNetworks == null) App.FavoriteNetworks = new ObservableCollection<Network>();

            if (await localHelper.FileExistsAsync("Accounts"))
            {
                App.Accounts = await localHelper.ReadFileAsync<ObservableCollection<Account>>("Accounts");
            }
            if (App.Accounts == null) App.Accounts = new ObservableCollection<Account>();


            if (!(Window.Current.Content is Frame rootFrame))
            {
                rootFrame = new Frame();
                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {

                }

                Window.Current.Content = rootFrame;
            }

            if (rootFrame.Content == null)
            {
                if (Accounts.Count() == 0)
                {
                    rootFrame.Navigate(typeof(WelcomePage));
                }
                else
                {
                    rootFrame.Navigate(typeof(RootPage));
                }
            }

            Window.Current.Activate();

            ExtendAcrylicIntoTitleBar();
        }

        protected override void OnBackgroundActivated(BackgroundActivatedEventArgs args)
        {
            base.OnBackgroundActivated(args);
            IBackgroundTaskInstance taskInstance = args.TaskInstance;
            Run(taskInstance);
        }

        private async void Run(IBackgroundTaskInstance taskInstance)
        {
            BackgroundTaskDeferral deferral = taskInstance.GetDeferral();
            if (taskInstance.Task.Name != "Time Trigger")
            {
                await Connect();
            }
            else
            {
                await LowBalanceAlert();
            }
            deferral.Complete();
        }

        private void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        private void ExtendAcrylicIntoTitleBar()
        {
            var coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            coreTitleBar.ExtendViewIntoTitleBar = true;

            var viewTitleBar = ApplicationView.GetForCurrentView().TitleBar;
            viewTitleBar.ButtonBackgroundColor = Colors.Transparent;
            viewTitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
            viewTitleBar.ButtonForegroundColor = (Color)Resources["SystemBaseHighColor"];
        }

        private async void SetBackgroundTask()
        {
            var localHelper = new LocalObjectStorageHelper();
            var oldVersion = localHelper.Read<string>("Version");
            var packageVersion = Package.Current.Id.Version;
            var newVersion = String.Format("{0}.{1}.{2}.{3}", packageVersion.Major, packageVersion.Minor, packageVersion.Build, packageVersion.Revision);
            localHelper.Save("Version", newVersion);

            if (oldVersion == null)
            {
                foreach (var item in BackgroundTaskRegistration.AllTasks)
                {
                    item.Value.Unregister(true);
                }

                await BackgroundExecutionManager.RequestAccessAsync();
            }
            else if (oldVersion != newVersion)
            {
                foreach (var item in BackgroundTaskRegistration.AllTasks)
                {
                    item.Value.Unregister(true);
                }

                BackgroundExecutionManager.RemoveAccess();
                await BackgroundExecutionManager.RequestAccessAsync();
            }

            var builder = new BackgroundTaskBuilder();
            BackgroundTaskRegistration registeredTask = null;

            var isNetworkTriggerSet = false;
            var isSessionTriggerSet = false;
            var isTimeTriggerSet = false;
            foreach (var item in BackgroundTaskRegistration.AllTasks)
            {
                if (item.Value.Name == "Network Trigger")
                {
                    isNetworkTriggerSet = true;
                }
                if (item.Value.Name == "Session Trigger")
                {
                    isSessionTriggerSet = true;
                }
                if (item.Value.Name == "Time Trigger")
                {
                    isTimeTriggerSet = true;
                }
            }

            if (!isNetworkTriggerSet)
            {
                builder.SetTrigger(new SystemTrigger(SystemTriggerType.NetworkStateChange, false));
                builder.Name = "Network Trigger";
                registeredTask = builder.Register();
            }
            if (!isSessionTriggerSet)
            {
                builder.SetTrigger(new SystemTrigger(SystemTriggerType.SessionConnected, false));
                builder.Name = "Session Trigger";
                registeredTask = builder.Register();
            }
            if (!isTimeTriggerSet)
            {
                builder.SetTrigger(new TimeTrigger(1440, false));
                builder.AddCondition(new SystemCondition(SystemConditionType.UserPresent));
                builder.Name = "Time Trigger";
                registeredTask = builder.Register();
            }
        }

        private async Task Connect()
        {
            var localHelper = new LocalObjectStorageHelper();
            ObservableCollection<Network> _favoriteNetworks = null;
            ObservableCollection<Account> _accounts = null;

            if (await localHelper.FileExistsAsync("Networks"))
            {
                _favoriteNetworks = await localHelper.ReadFileAsync<ObservableCollection<Network>>("Networks");
            }
            if (_favoriteNetworks == null) _favoriteNetworks = new ObservableCollection<Network>();

            if (await localHelper.FileExistsAsync("Accounts"))
            {
                _accounts = await localHelper.ReadFileAsync<ObservableCollection<Account>>("Accounts");
            }
            if (_accounts == null) _accounts = new ObservableCollection<Account>();


            var profile = Windows.Networking.Connectivity.NetworkInformation.GetInternetConnectionProfile();

            if (profile != null && _accounts.Count() > 0)
            {
                var currentAccount = _accounts.First();
                var ssid = profile.ProfileName;

                var isWired = !profile.IsWlanConnectionProfile && !profile.IsWwanConnectionProfile;

                if (isWired)
                {
                    var credential = CredentialHelper.GetCredentialFromLocker(currentAccount.Username);
                    if (credential != null)
                    {
                        credential.RetrievePassword();
                    }
                    else
                    {
                        return;
                    }

                    var response = await AuthHelper.LoginAsync(4, currentAccount.Username, credential.Password);
                    if (response.Contains("login_ok"))
                    {
                        ShowAutoLoginNotification(ssid);
                        await Task.Delay(10100);
                        await AuthHelper.LoginAsync(6, currentAccount.Username, credential.Password);
                    }
                    else if (response == "ip_already_online_error")
                    {
                        ShowAutoLoginNotification(ssid);
                    }
                    else if (response.Contains("login_error"))
                    {
                        await Task.Delay(10100);
                        await AuthHelper.LoginAsync(4, currentAccount.Username, credential.Password);
                        await Task.Delay(10100);
                        await AuthHelper.LoginAsync(6, currentAccount.Username, credential.Password);
                    }
                    else
                    {
                        ShowWrongPasswordNotification(ssid, currentAccount.Username);
                    }
                }
                else if (!isWired && ssid.Contains("Tsinghua") || _favoriteNetworks.Where(u => u.Ssid == ssid).Count() != 0)
                {
                    var response = await NetHelper.LoginAsync(currentAccount.Username, currentAccount.Password);

                    if (response == "Login is successful.")
                    {
                        ShowAutoLoginNotification(ssid);
                    }
                    else if (response == "E2620: You are already online." || response == "IP has been online, please logout.")
                    {
                        ShowAutoLoginNotification(ssid);
                    }
                    else if (response == "E2532: The two authentication interval cannot be less than 3 seconds.")
                    {
                        await Task.Delay(3500);
                        await NetHelper.LoginAsync(currentAccount.Username, currentAccount.Password);
                    }
                    else if (response == "E2553: Password is error.")
                    {
                        ShowWrongPasswordNotification(ssid, currentAccount.Username);
                    }
                }
            }
            else
            {
                ToastNotificationManager.History.Clear();
            }
        }

        private async void ShowAutoLoginNotification(string ssid)
        {
            var status = await NetHelper.GetStatusAsync();
            if (status != null)
            {
                var lastToast = ToastNotificationManager.History.GetHistory().FirstOrDefault();
                if (lastToast != null)
                {
                    var expirationTime = lastToast.ExpirationTime ?? DateTimeOffset.MinValue;
                    var lastProfileName = lastToast.Content.InnerText.Split("\n").Last();
                    var now = DateTimeOffset.Now.AddDays(3);
                    var timeDiff = now.Subtract(expirationTime);
                    var lastSsid = lastProfileName.Substring(lastProfileName.LastIndexOf(' ') + 1);

                    if (lastProfileName == ssid && timeDiff.Minutes < 1)
                    {
                        return;
                    }
                    else if (lastSsid == ssid && timeDiff.Minutes < 1)
                    {
                        return;
                    }
                }

                var usage = Utility.GetUsageDescription((long)status["total_usage"]);
                var balance = Utility.GetBalanceDescription(Convert.ToDouble(status["balance"]));
                var username = status["username"] as string;

                var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForViewIndependentUse();
                var toast = new NotificationToast(".Net Campus", String.Format(resourceLoader.GetString("Auto-loginToast"), username, usage, balance, ssid))
                {
                    Tag = "64",
                    Group = "Login"
                };
                toast.Show();
            }
        }

        private void ShowWrongPasswordNotification(string ssid, string username)
        {
            var lastToast = ToastNotificationManager.History.GetHistory().FirstOrDefault();
            if (lastToast != null)
            {
                var expirationTime = lastToast.ExpirationTime ?? DateTimeOffset.MinValue;
                var lastProfileName = lastToast.Content.InnerText.Split("\n").Last();
                var now = DateTimeOffset.Now.AddDays(3);
                var timeDiff = now.Subtract(expirationTime);
                var lastSsid = lastProfileName.Substring(lastProfileName.LastIndexOf(' ') + 1);

                if (lastProfileName == ssid && timeDiff.Minutes < 1)
                {
                    return;
                }
                else if (lastSsid == ssid && timeDiff.Minutes < 1)
                {
                    return;
                }
            }

            var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForViewIndependentUse();
            var toast = new NotificationToast(".Net Campus", String.Format(resourceLoader.GetString("WrongPasswordToast"), username))
            {
                Tag = "64",
                Group = "Login"
            };
            toast.Show();
        }

        private async Task LowBalanceAlert()
        {
            var profile = Windows.Networking.Connectivity.NetworkInformation.GetInternetConnectionProfile();

            if (profile != null)
            {
                var status = await NetHelper.GetStatusAsync();
                if (status != null)
                {
                    var balance = Convert.ToDouble(status["balance"]);
                    var balanceDescription = Utility.GetBalanceDescription(balance);

                    if (balance < 5)
                    {
                        var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForViewIndependentUse();
                        var toast = new NotificationToast(".Net Campus", String.Format(resourceLoader.GetString("LowBalanceAlert"), balanceDescription))
                        {
                            Tag = "65",
                            Group = "Balance"
                        };
                        toast.Show();
                    }
                }
            }
        }
    }
}
