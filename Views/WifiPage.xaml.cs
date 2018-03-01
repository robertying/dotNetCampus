using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Devices.WiFi;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Networking.Connectivity;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

namespace CampusNet
{
    public sealed partial class WifiPage : Page
    {
        private WiFiAdapter wifiAdapter;
        private string savedProfileName = null;
        private Network connectedNetwork;

        public ObservableCollection<Network> FavoriteNetworks
        {
            get => App.FavoriteNetworks;
            set => App.FavoriteNetworks = value;
        }

        public WifiPage()
        {
            this.InitializeComponent();
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
            else
            {
                var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
                StatusTextBlock.Text = resourceLoader.GetString("Disconnected");
            }

            if (connectedProfile != null && connectedNetwork.Ssid != savedProfileName)
            {
                savedProfileName = connectedNetwork.Ssid;
                var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
                StatusTextBlock.Text = resourceLoader.GetString("ConnectedTo") + ' ' + connectedNetwork.Ssid;
            }
            else if (connectedProfile == null && savedProfileName != null)
            {
                savedProfileName = null;
                var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
                StatusTextBlock.Text = resourceLoader.GetString("Disconnected");
            }
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            var access = await WiFiAdapter.RequestAccessAsync();
            if (access != WiFiAccessStatus.Allowed)
            {
                var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
                StatusTextBlock.Text = resourceLoader.GetString("WiFiAccessDenied");
            }
            else
            {
                DataContext = this;

                var result = await Windows.Devices.Enumeration.DeviceInformation.FindAllAsync(WiFiAdapter.GetDeviceSelector());
                if (result.Count >= 1)
                {
                    wifiAdapter = await WiFiAdapter.FromIdAsync(result[0].Id);
                    await UpdateConnectivityStatusAsync();
                }
                else
                {
                    var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
                    StatusTextBlock.Text = resourceLoader.GetString("WiFiAccessDenied");
                }
            }
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

        private void AddFavoritesButton_Click(object sender, RoutedEventArgs e)
        {
            var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
            if (connectedNetwork == null)
            {
                Notification.Show(resourceLoader.GetString("ConnectFirst"), 5000);
            }
            else
            {
                if (FavoriteNetworks.Where(u => u.Ssid == connectedNetwork.Ssid).Count() != 0)
                {
                    Notification.Show(connectedNetwork.Ssid + ' ' + resourceLoader.GetString("AlreadyFavorite"), 5000);
                }
                else
                {
                    FavoriteNetworks.Add(connectedNetwork);
                    DataStorage.SaveFileAsync("Networks", FavoriteNetworks);
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
                DataStorage.SaveFileAsync("Networks", FavoriteNetworks);

                Notification.Show(ssid + ' ' + resourceLoader.GetString("RemoveSuccess"), 5000);
            }
        }
    }
}
