using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking.Connectivity;
using Microsoft.Toolkit.Uwp;
using Microsoft.Toolkit.Uwp.Helpers;

namespace CampusNet
{
    public static class DataStorage
    {
        public static async void SaveFileAsync(string description, Object data)
        {
            var helper = new LocalObjectStorageHelper();
            await helper.SaveFileAsync(description, data);
        }

        public static async Task<T> GetFileAsync<T>(string description)
        {
            T data = default(T);
            var helper = new LocalObjectStorageHelper();
            if (await helper.FileExistsAsync(description))
            {
                return await helper.ReadFileAsync(description, data);
            }
            else
            {
                return default(T);
            }
        }

        public static string GetSettings(string description)
        {
            Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            return localSettings.Values[description] as string;
        }

        public static void SaveSettings(string description, string data)
        {
            Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            localSettings.Values[description] = data;
        }
    }
}
