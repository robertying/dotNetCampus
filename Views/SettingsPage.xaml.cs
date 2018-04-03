using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

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
    }
}
