using Microsoft.Toolkit.Uwp.UI.Animations;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Background;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

namespace CampusNet
{
    public sealed partial class RootPage : Page
    {
        public RootPage()
        {
            this.InitializeComponent();

            string appName = Windows.ApplicationModel.Package.Current.DisplayName;
            AppTitle.Text = appName;
            NavView.Header = "";

            this.Loaded += RootPage_Loaded;
            ContentFrame.Navigate(typeof(GeneralPage));
        }

        private async void RootPage_Loaded(object sender, RoutedEventArgs e)
        {
            var imageSource = await AssetsHelper.GetBingWallpaperAsync();
            if (imageSource != null)
            {
                BackgroundImage.Source = new BitmapImage(imageSource);
                BackgroundImage.Blur(value: 0, duration: 3500, delay: 0).Start();
            }
        }

        private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs e)
        {
            if (e.IsSettingsSelected)
            {
                ContentFrame.Navigate(typeof(SettingsPage));
                BackgroundImage.Visibility = Visibility.Collapsed;
                var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
                NavView.Header = resourceLoader.GetString("Settings");
            }
            else
            {
                NavigationViewItem item = e.SelectedItem as NavigationViewItem;

                switch (item.Tag.ToString())
                {
                    case "general":
                        ContentFrame.Navigate(typeof(GeneralPage));
                        BackgroundImage.Visibility = Visibility.Visible;
                        NavView.Header = "";
                        break;
                    case "wifi":
                        ContentFrame.Navigate(typeof(WifiPage));
                        BackgroundImage.Visibility = Visibility.Collapsed;
                        var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
                        NavView.Header = resourceLoader.GetString("Wi-Fi");
                        break;
                    case "account":
                        ContentFrame.Navigate(typeof(AccountPage));
                        BackgroundImage.Visibility = Visibility.Collapsed;
                        resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
                        NavView.Header = resourceLoader.GetString("Account");
                        break;
                }
            }
        }
    }
}
