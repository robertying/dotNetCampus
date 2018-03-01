using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CampusNet
{
    public static class AssetsHelper
    {
        private static Uri BING_URI = new Uri("https://www.bing.com/HPImageArchive.aspx?format=xml&idx=0&n=1&mkt=en-US");

        public static async Task<Uri> GetBingWallpaperAsync()
        {
            var httpClient = new Windows.Web.Http.HttpClient();
            Windows.Web.Http.HttpResponseMessage httpResponse = new Windows.Web.Http.HttpResponseMessage();
            string httpResponseBody = "";

            try
            {
                httpResponse = await httpClient.GetAsync(BING_URI);
                httpResponse.EnsureSuccessStatusCode();
                httpResponseBody = await httpResponse.Content.ReadAsStringAsync();
                //Debug.WriteLine("GetBingWallpaperAsync(): " + httpResponseBody);

                HtmlAgilityPack.HtmlDocument htmlDocument = new HtmlAgilityPack.HtmlDocument();
                htmlDocument.LoadHtml(httpResponseBody);

                var data = htmlDocument.DocumentNode.Descendants("urlBase");
                if (data == null) return null;
                string url = "https://www.bing.com" + data.First().InnerText + "_1920x1080.jpg";

                return new Uri(url);
            }
            catch (Exception ex)
            {
                httpResponseBody = "Error: " + ex.HResult.ToString("X") + " Message: " + ex.Message;
                Debug.WriteLine("GetBingWallpaperAsync(): " + httpResponseBody);
                return null;
            }
        }
    }
}
