﻿using Microsoft.Toolkit.Uwp.Helpers;
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
        private static ObservableCollection<Network> favoriteNetworks;
        private static ObservableCollection<Account> accounts;
        public static ObservableCollection<Network> FavoriteNetworks { get => favoriteNetworks; set => favoriteNetworks = value; }
        public static ObservableCollection<Account> Accounts { get => accounts; set => accounts = value; }

        public App()
        {
            this.InitializeComponent();

            SetBackgroundTask();
        }

        protected override async void OnLaunched(LaunchActivatedEventArgs e)
        {
            var localHelper = new LocalObjectStorageHelper();
            App.FavoriteNetworks = new ObservableCollection<Network>();
            App.Accounts = new ObservableCollection<Account>();

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

            if (App.FavoriteNetworks == null) App.FavoriteNetworks = new ObservableCollection<Network>();

            if (await localHelper.FileExistsAsync("Accounts"))
            {
                App.Accounts = await localHelper.ReadFileAsync<ObservableCollection<Account>>("Accounts");
            }

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

        private void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
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
                var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForViewIndependentUse();

                if (ssid.Contains("Tsinghua") || _favoriteNetworks.Where(u => u.Ssid == ssid).Count() != 0)
                {
                    var response = await NetHelper.LoginAsync(currentAccount.Username, currentAccount.Password);
                    if (response == "Login is successful.")
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

                            var toast = new NotificationToast(".Net Campus", String.Format(resourceLoader.GetString("Auto-loginToast"), username, usage, balance, ssid));
                            toast.Tag = "64";
                            toast.Group = "Login";
                            toast.Show();
                        }
                    }
                    else if (response == "E2620: You are already online." || response == "IP has been online, please logout.")
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

                            var toast = new NotificationToast(".Net Campus", String.Format(resourceLoader.GetString("Auto-loginToast"), username, usage, balance, ssid));
                            toast.Tag = "64";
                            toast.Group = "Login";
                            toast.Show();
                        }
                    }
                    else if (response == "E2553: Password is error.")
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

                        var toast = new NotificationToast(".Net Campus", String.Format(resourceLoader.GetString("WrongPasswordToast"), currentAccount.Username));
                        toast.Tag = "64";
                        toast.Group = "Login";
                        toast.Show();
                    }
                }
            }
            else
            {
                ToastNotificationManager.History.Clear();
            }
        }
    }
}
