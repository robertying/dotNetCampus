using Microsoft.Toolkit.Uwp.Helpers;
using System;
using Windows.ApplicationModel;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace CampusNet
{
    public sealed partial class SettingsPage : Page
    {
        private string version;

        public SettingsPage()
        {
            this.InitializeComponent();
            DataContext = this;

            var packageVersion = Package.Current.Id.Version;
            Version = String.Format("{0}.{1}.{2}.{3}", packageVersion.Major, packageVersion.Minor, packageVersion.Build, packageVersion.Revision);
        }

        public string Version { get => version; set => version = value; }

        private async void GitHubHyperlink_Click(object sender, RoutedEventArgs e)
        {
            await Windows.System.Launcher.LaunchUriAsync(new Uri("https://github.com/robertying/dotNetCampus"));
        }

        private async void PrivacyPolicyHyperlink_Click(object sender, RoutedEventArgs e)
        {
            await Windows.System.Launcher.LaunchUriAsync(new Uri("https://github.com/robertying/dotNetCampus/blob/master/PRIVACYPOLICY.md"));
        }

        private void RoamingToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleSwitch toggleSwitch)
            {
                var localHelper = new LocalObjectStorageHelper();
                if (toggleSwitch.IsOn == true)
                {
                    localHelper.Save("Roaming", true);
                }
                else
                {
                    localHelper.Save("Roaming", false);
                }
            }
        }
    }
}
