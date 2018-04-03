using Microsoft.Toolkit.Uwp.Helpers;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.Networking.Connectivity;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace CampusNet
{
    public sealed partial class WifiPage : Page
    {
        private Network connectedNetwork;

        public ObservableCollection<Network> FavoriteNetworks
        {
            get => App.FavoriteNetworks;
            set => App.FavoriteNetworks = value;
        }

        public WifiPage()
        {
            this.InitializeComponent();
            DataContext = this;
        }

        private void GetCurrentNetwork()
        {
            var connectedProfile = NetworkInformation.GetInternetConnectionProfile();
            if (connectedProfile != null)
            {
                connectedNetwork = new Network
                {
                    Ssid = connectedProfile.ProfileName
                };
                var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
                StatusTextBlock.Text = resourceLoader.GetString("ConnectedTo") + ' ' + connectedNetwork.Ssid;
            }
            else
            {
                var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
                StatusTextBlock.Text = resourceLoader.GetString("Disconnected");
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            GetCurrentNetwork();
        }

        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
            var success = await Windows.System.Launcher.LaunchUriAsync(new Uri(@"ms-availablenetworks:"));
            if (success)
            {
                Notification.Show(resourceLoader.GetString("ConnectPopupNotification"), 5000);
            }
            else
            {
                Notification.Show(resourceLoader.GetString("OpenPanelFail"), 5000);
            }
        }

        private async void AddFavoritesButton_Click(object sender, RoutedEventArgs e)
        {
            var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
            if (connectedNetwork == null)
            {
                Notification.Show(resourceLoader.GetString("ConnectFirst"), 5000);
            }
            else
            {
                if (connectedNetwork.Ssid.Contains("Tsinghua") || FavoriteNetworks.Where(u => u.Ssid == connectedNetwork.Ssid).Count() != 0)
                {
                    Notification.Show(connectedNetwork.Ssid + ' ' + resourceLoader.GetString("AlreadyFavorite"), 5000);
                }
                else
                {
                    FavoriteNetworks.Add(connectedNetwork);

                    var localHelper = new LocalObjectStorageHelper();
                    await localHelper.SaveFileAsync("Networks", FavoriteNetworks);

                    Notification.Show(connectedNetwork.Ssid + ' ' + resourceLoader.GetString("AddedToFavorites"), 5000);
                }
            }
        }

        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListView list = sender as ListView;

            if (e.AddedItems.Count == 1)
            {
                if (list.ContainerFromItem(e.AddedItems[0]) is ListViewItem addedItem)
                {
                    addedItem.ContentTemplate = this.Resources["List_Selected"] as DataTemplate;
                }
            }
            if (e.RemovedItems.Count == 1)
            {
                if (list.ContainerFromItem(e.RemovedItems[0]) is ListViewItem removedItem)
                {
                    removedItem.ContentTemplate = this.Resources["List_Normal"] as DataTemplate;
                }
            }
        }

        private void ListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            ListView list = sender as ListView;
            ListViewItem listItem = list.ContainerFromItem(e.ClickedItem) as ListViewItem;

            if (listItem.IsSelected)
            {
                listItem.IsSelected = false;
                list.SelectionMode = ListViewSelectionMode.None;
            }
            else
            {
                list.SelectionMode = ListViewSelectionMode.Single;
                listItem.IsSelected = true;
            }
        }

        private async void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
            var dialog = new ContentDialog()
            {
                Title = resourceLoader.GetString("Warning"),
                Content = resourceLoader.GetString("RemoveNetworkConfirm"),
                PrimaryButtonText = resourceLoader.GetString("Yes"),
                SecondaryButtonText = resourceLoader.GetString("No")
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                String ssid = (sender as Button).Tag as String;
                var index = (FavoriteNetworks.Where(u => u.Ssid == ssid).ToList())[0];
                FavoriteNetworks.Remove(index);

                var localHelper = new LocalObjectStorageHelper();
                await localHelper.SaveFileAsync("Networks", FavoriteNetworks);

                Notification.Show(ssid + ' ' + resourceLoader.GetString("RemoveSuccess"), 5000);
            }
        }
    }
}
