using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Background;
using Windows.ApplicationModel.Core;
using Windows.Devices.WiFi;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
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
            App.FavoriteNetworks = await DataStorage.GetFileAsync<ObservableCollection<Network>>("Networks");
            App.Accounts = await DataStorage.GetFileAsync<ObservableCollection<Account>>("Accounts");
            if (App.FavoriteNetworks == null) App.FavoriteNetworks = new ObservableCollection<Network>();
            if (App.Accounts == null) App.Accounts = new ObservableCollection<Account>();

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
            App.FavoriteNetworks = await DataStorage.GetFileAsync<ObservableCollection<Network>>("Networks");
            App.Accounts = await DataStorage.GetFileAsync<ObservableCollection<Account>>("Accounts");
            if (App.FavoriteNetworks == null) App.FavoriteNetworks = new ObservableCollection<Network>();
            if (App.Accounts == null) App.Accounts = new ObservableCollection<Account>();

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

        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();

            deferral.Complete();
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
            var oldVersion = DataStorage.GetSettings("version");
            var packageVersion = Package.Current.Id.Version;
            var newVersion = String.Format("{0}.{1}.{2}.{3}", packageVersion.Major, packageVersion.Minor, packageVersion.Build, packageVersion.Revision);
            DataStorage.SaveSettings("version", newVersion);

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
            builder.Name = "Background Trigger";
            BackgroundTaskRegistration registeredTask = null;

            builder.SetTrigger(new SystemTrigger(SystemTriggerType.InternetAvailable, false));
            builder.AddCondition(new SystemCondition(SystemConditionType.SessionConnected));
            builder.AddCondition(new SystemCondition(SystemConditionType.UserPresent));
            registeredTask = builder.Register();

            builder.SetTrigger(new SystemTrigger(SystemTriggerType.SessionConnected, false));
            builder.AddCondition(new SystemCondition(SystemConditionType.InternetAvailable));
            registeredTask = builder.Register();
        }

        private async Task Connect()
        {
            var _accounts = await DataStorage.GetFileAsync<ObservableCollection<Account>>("Accounts");
            var _favoriteNetworks = await DataStorage.GetFileAsync<ObservableCollection<Network>>("Networks");
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
                }
            }
        }
    }
}
