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
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace CampusNet
{
    sealed partial class App : Application
    {
        private static ObservableCollection<Network> favoriteNetworks;
        private static ObservableCollection<Account> accounts;
        public static ObservableCollection<Network> FavoriteNetworks { get => favoriteNetworks; set => favoriteNetworks = value; }
        public static ObservableCollection<Account> Accounts { get => accounts; set => accounts = value; }

        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;

            SetBackgroundTask();
        }

        protected override async void OnLaunched(LaunchActivatedEventArgs e)
        {
            var localHelper = new LocalObjectStorageHelper();
            if (await localHelper.FileExistsAsync("Networks"))
            {
                App.FavoriteNetworks = await localHelper.ReadFileAsync<ObservableCollection<Network>>("Networks");
            }
            else
            {
                App.FavoriteNetworks = new ObservableCollection<Network>();
            }

            if (await localHelper.FileExistsAsync("Accounts"))
            {
                App.Accounts = await localHelper.ReadFileAsync<ObservableCollection<Account>>("Accounts");
            }
            else
            {
                App.Accounts = new ObservableCollection<Account>();
            }

            FetchRoamingDataAsync();

            Frame rootFrame = Window.Current.Content as Frame;

            if (rootFrame == null)
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
            else
            {
                App.FavoriteNetworks = new ObservableCollection<Network>();
            }

            if (await localHelper.FileExistsAsync("Accounts"))
            {
                App.Accounts = await localHelper.ReadFileAsync<ObservableCollection<Account>>("Accounts");
            }
            else
            {
                App.Accounts = new ObservableCollection<Account>();
            }

            FetchRoamingDataAsync();

            Frame rootFrame = Window.Current.Content as Frame;

            if (rootFrame == null)
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
            await Connect();
            deferral.Complete();
        }

        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        private async void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            await SaveRoamingDataAsync();
            deferral.Complete();
        }

        private async void FetchRoamingDataAsync()
        {
            var roamingHelper = new RoamingObjectStorageHelper();
            var localHelper = new LocalObjectStorageHelper();
            bool isRoamingEnabled;

            if (roamingHelper.KeyExists("Roaming"))
            {
                isRoamingEnabled = roamingHelper.Read<bool>("Roaming");
                localHelper.Save("Roaming", isRoamingEnabled);

                if (isRoamingEnabled)
                {
                    if (await roamingHelper.FileExistsAsync("Accounts"))
                    {
                        App.Accounts = await roamingHelper.ReadFileAsync<ObservableCollection<Account>>("Accounts");
                        await localHelper.SaveFileAsync("Accounts", App.Accounts);
                    }
                    if (await roamingHelper.FileExistsAsync("Networks"))
                    {
                        App.FavoriteNetworks = await roamingHelper.ReadFileAsync<ObservableCollection<Network>>("Networks");
                        await localHelper.SaveFileAsync("Networks", App.FavoriteNetworks);
                    }
                }
            }
        }

        private async Task SaveRoamingDataAsync()
        {
            var roamingHelper = new RoamingObjectStorageHelper();
            var localHelper = new LocalObjectStorageHelper();
            bool isRoamingEnabled = false;

            if (localHelper.KeyExists("Roaming"))
            {
                isRoamingEnabled = localHelper.Read<bool>("Roaming");
            }

            if (isRoamingEnabled)
            {
                roamingHelper.Save("Roaming", isRoamingEnabled);
                await roamingHelper.SaveFileAsync("Accounts", App.Accounts);
                await roamingHelper.SaveFileAsync("Networks", App.FavoriteNetworks);
            }
        }

        private void ExtendAcrylicIntoTitleBar()
        {
            CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = true;
            ApplicationViewTitleBar titleBar = ApplicationView.GetForCurrentView().TitleBar;
            titleBar.ButtonBackgroundColor = Colors.Transparent;
            titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
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
                await BackgroundExecutionManager.RequestAccessAsync();
            }
            else if (oldVersion != newVersion)
            {
                BackgroundExecutionManager.RemoveAccess();
                await BackgroundExecutionManager.RequestAccessAsync();
            }

            var builder = new BackgroundTaskBuilder();
            BackgroundTaskRegistration registeredTask = null;

            var isInternetTriggerSet = false;
            var isSessionTriggerSet = false;
            foreach (var item in BackgroundTaskRegistration.AllTasks)
            {
                if (item.Value.Name == "Internet Trigger")
                {
                    isInternetTriggerSet = true;
                }
                if (item.Value.Name == "Session Trigger")
                {
                    isSessionTriggerSet = true;
                }
            }

            if (!isInternetTriggerSet)
            {
                builder.SetTrigger(new SystemTrigger(SystemTriggerType.InternetAvailable, false));
                builder.AddCondition(new SystemCondition(SystemConditionType.SessionConnected));
                builder.AddCondition(new SystemCondition(SystemConditionType.UserPresent));
                builder.Name = "Internet Trigger";
                registeredTask = builder.Register();
            }

            if (!isSessionTriggerSet)
            {
                builder.SetTrigger(new SystemTrigger(SystemTriggerType.SessionConnected, false));
                builder.AddCondition(new SystemCondition(SystemConditionType.InternetAvailable));
                builder.Name = "Session Trigger";
                registeredTask = builder.Register();
            }
        }

        private async Task Connect()
        {
            var localHelper = new LocalObjectStorageHelper();
            ObservableCollection<Network> _favoriteNetworks = null;
            ObservableCollection<Account> _accounts = null;

            if (await localHelper.FileExistsAsync("Accounts"))
            {
                _accounts = await localHelper.ReadFileAsync<ObservableCollection<Account>>("Accounts");
            }
            if (await localHelper.FileExistsAsync("Networks"))
            {
                _favoriteNetworks = await localHelper.ReadFileAsync<ObservableCollection<Network>>("Networks");
            }

            if (_accounts == null) _accounts = new ObservableCollection<Account>();
            if (_favoriteNetworks == null) _favoriteNetworks = new ObservableCollection<Network>();
            var profile = Windows.Networking.Connectivity.NetworkInformation.GetInternetConnectionProfile();

            if (_accounts.Count() > 0)
            {
                var currentAccount = _accounts.First();
                var ssid = profile.ProfileName;
                var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForViewIndependentUse();

                if (ssid.Contains("Tsinghua") || _favoriteNetworks.Where(u => u.Ssid == ssid).Count() != 0)
                {
                    var response = await NetHelper.LoginAsync(currentAccount.Username, currentAccount.Password);
                    if (response == "Login is successful.")
                    {
                        var status = await NetHelper.GetStatusAsync();
                        if (status != null)
                        {
                            var usage = Utility.GetUsageDescription((long)status["total_usage"]);
                            var balance = Utility.GetBalanceDescription(Convert.ToDouble(status["balance"]));
                            var username = status["username"] as string;

                            var toast = new NotificationToast(".Net Campus", String.Format(resourceLoader.GetString("Auto-loginToast"), username, usage, balance));
                            toast.Tag = "64";
                            toast.Group = "Login";
                            toast.Show();
                        }
                    }
                    else if (response == "IP has been online, please logout.")
                    {
                        var status = await NetHelper.GetStatusAsync();
                        if (status != null)
                        {
                            var usage = Utility.GetUsageDescription((long)status["total_usage"]);
                            var balance = Utility.GetBalanceDescription(Convert.ToDouble(status["balance"]));
                            var username = status["username"] as string;

                            var toast = new NotificationToast(".Net Campus", String.Format(resourceLoader.GetString("Auto-loginToast"), username, usage, balance));
                            toast.Tag = "64";
                            toast.Group = "Login";
                            toast.Show();
                        }
                    }
                    else if (response == "E2553: Password is error.")
                    {
                        var toast = new NotificationToast(".Net Campus", String.Format(resourceLoader.GetString("WrongPasswordToast"), currentAccount.Username));
                        toast.Tag = "64";
                        toast.Group = "Login";
                        toast.Show();
                    }
                }
            }
        }
    }
}
