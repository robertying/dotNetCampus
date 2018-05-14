using Microsoft.Toolkit.Uwp.Helpers;
using Microsoft.Toolkit.Uwp.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.ApplicationModel.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

namespace CampusNet
{
    public sealed partial class RootPage : Page
    {
        public RootPage()
        {
            this.InitializeComponent();
            this.Loaded += RootPage_Loaded;

            Window.Current.SetTitleBar(TitlebarRegion);
            Window.Current.CoreWindow.SizeChanged += (s, e) => UpdateTitlebarRegion();

            ContentFrame.Navigate(typeof(GeneralPage));
        }

        private void UpdateTitlebarRegion()
        {
            var height = CoreApplication.GetCurrentView().TitleBar.Height;
            TitlebarRegion.Height = height;
        }

        private async void RootPage_Loaded(object sender, RoutedEventArgs e)
        {
            var localHelper = new LocalObjectStorageHelper();
            string oldImgaeUri = null;

            if (localHelper.KeyExists("BackgroundImage"))
            {
                oldImgaeUri = localHelper.Read<string>("BackgroundImage");
            }

            if (oldImgaeUri != null)
            {
                OldBackgroundImage.Source = await ImageCache.Instance.GetFromCacheAsync(new Uri(oldImgaeUri));
            }

            var imageSource = await AssetsHelper.GetBingWallpaperAsync();
            if (imageSource != null)
            {
                NewBackgroundImage.Source = new BitmapImage(imageSource);
                localHelper.Save("BackgroundImage", imageSource.OriginalString);
            }

            if (imageSource != null)
            {
                if (this.Resources["FadeIn_Image"] is Storyboard fadeIn) fadeIn.Begin();
            }
        }

        private void NavView_Loaded(object sender, RoutedEventArgs e)
        {
            foreach (NavigationViewItemBase item in NavView.MenuItems)
            {
                if (item is NavigationViewItem && item.Tag.ToString() == "general")
                {
                    NavView.SelectedItem = item;
                    break;
                }
            }

            ContentFrame.Navigated += On_Navigated;
        }

        private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            if (args.IsSettingsInvoked)
            {
                var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
                NewBackgroundImage.Visibility = Visibility.Collapsed;
                OldBackgroundImage.Visibility = Visibility.Collapsed;
                ContentFrame.Navigate(typeof(SettingsPage));
                NavView.Header = resourceLoader.GetString("Settings");
            }
            else
            {
                var item = sender.MenuItems.OfType<NavigationViewItem>().First(x => (string)x.Content == (string)args.InvokedItem);
                NavView_Navigate(item as NavigationViewItem);
            }
        }

        private void NavView_Navigate(NavigationViewItem item)
        {
            var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();

            switch (item.Tag)
            {
                case "general":
                    NavView.Header = " ";
                    ContentFrame.Navigate(typeof(GeneralPage));
                    NewBackgroundImage.Visibility = Visibility.Visible;
                    OldBackgroundImage.Visibility = Visibility.Visible;
                    break;

                case "wifi":
                    NewBackgroundImage.Visibility = Visibility.Collapsed;
                    OldBackgroundImage.Visibility = Visibility.Collapsed;
                    ContentFrame.Navigate(typeof(WifiPage));
                    NavView.Header = resourceLoader.GetString("Wi-Fi");
                    break;

                case "account":
                    NewBackgroundImage.Visibility = Visibility.Collapsed;
                    OldBackgroundImage.Visibility = Visibility.Collapsed;
                    ContentFrame.Navigate(typeof(AccountPage));
                    NavView.Header = resourceLoader.GetString("Account");
                    break;
            }
        }

        private void On_Navigated(object sender, NavigationEventArgs e)
        {
            if (ContentFrame.SourcePageType == typeof(SettingsPage))
            {
                NavView.SelectedItem = NavView.SettingsItem as NavigationViewItem;
            }
            else
            {
                Dictionary<Type, string> lookup = new Dictionary<Type, string>()
                {
                    {typeof(GeneralPage), "general"},
                    {typeof(WifiPage), "wifi"},
                    {typeof(AccountPage), "account"}
                };

                String stringTag = lookup[ContentFrame.SourcePageType];

                foreach (NavigationViewItemBase item in NavView.MenuItems)
                {
                    if (item is NavigationViewItem && item.Tag.Equals(stringTag))
                    {
                        item.IsSelected = true;
                        break;
                    }
                }
            }
        }
    }
}
