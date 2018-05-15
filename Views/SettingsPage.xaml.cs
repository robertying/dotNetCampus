using Microsoft.Toolkit.Uwp.Helpers;
using System;
using Windows.ApplicationModel;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace CampusNet
{
    public sealed partial class SettingsPage : Page
    {
        public SettingsPage()
        {
            this.InitializeComponent();
            DataContext = this;

            var localHelper = new LocalObjectStorageHelper();
            if (localHelper.KeyExists("BalanceThreshold"))
            {
                BalanceSlider.Value = localHelper.Read("BalanceThreshold", 5);
            }
            else
            {
                BalanceSlider.Value = 5;
            }

            var packageVersion = Package.Current.Id.Version;
            Version = String.Format("{0}.{1}.{2}.{3}", packageVersion.Major, packageVersion.Minor, packageVersion.Build, packageVersion.Revision);
        }

        public string Version { get; set; }

        private async void GitHubHyperlink_Click(object sender, RoutedEventArgs e)
        {
            await Windows.System.Launcher.LaunchUriAsync(new Uri("https://github.com/robertying/dotNetCampus"));
        }

        private async void PrivacyPolicyHyperlink_Click(object sender, RoutedEventArgs e)
        {
            await Windows.System.Launcher.LaunchUriAsync(new Uri("https://github.com/robertying/dotNetCampus/blob/master/PRIVACYPOLICY.md"));
        }

        private void Slider_ValueChanged(object sender, RoutedEventArgs e)
        {
            if (sender is Slider slider)
            {
                var localHelper = new LocalObjectStorageHelper();
                localHelper.Save("BalanceThreshold", slider.Value);
            }
        }
    }
}
