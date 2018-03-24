using Microsoft.Toolkit.Uwp.UI;
using Microsoft.Toolkit.Uwp.UI.Animations;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
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
            var oldImgaeUri = DataStorage.GetSettings("BackgroundImage");
            if (oldImgaeUri != null)
            {
                OldBackgroundImage.Source = await ImageCache.Instance.GetFromCacheAsync(new Uri(oldImgaeUri));
            }

            var imageSource = await AssetsHelper.GetBingWallpaperAsync();
            if (imageSource != null)
            {
                NewBackgroundImage.Source = new BitmapImage(imageSource);
                DataStorage.SaveSettings("BackgroundImage", imageSource.OriginalString);
            }

            if (imageSource != null)
            {
                NewBackgroundImage.Fade(100, 3000, 0).Start();
                OldBackgroundImage.Fade(0, 3000, 3000).Start();
            }
        }

        private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs e)
        {
            if (e.IsSettingsSelected)
            {
                ContentFrame.Navigate(typeof(SettingsPage));
                NewBackgroundImage.Visibility = Visibility.Collapsed;
                OldBackgroundImage.Visibility = Visibility.Collapsed;
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
                        NewBackgroundImage.Visibility = Visibility.Visible;
                        OldBackgroundImage.Visibility = Visibility.Visible;
                        NavView.Header = "";
                        break;
                    case "wifi":
                        ContentFrame.Navigate(typeof(WifiPage));
                        NewBackgroundImage.Visibility = Visibility.Collapsed;
                        OldBackgroundImage.Visibility = Visibility.Collapsed;
                        var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
                        NavView.Header = resourceLoader.GetString("Wi-Fi");
                        break;
                    case "account":
                        ContentFrame.Navigate(typeof(AccountPage));
                        NewBackgroundImage.Visibility = Visibility.Collapsed;
                        OldBackgroundImage.Visibility = Visibility.Collapsed;
                        resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
                        NavView.Header = resourceLoader.GetString("Account");
                        break;
                }
            }
        }
    }
}
