using System;
using System.Linq;
using System.Threading.Tasks;

namespace CampusNet
{
    public static class AssetsHelper
    {
        private static readonly Uri BING_URI = new Uri("https://www.bing.com/HPImageArchive.aspx?format=xml&idx=0&n=1&mkt=en-US");

        public static async Task<Uri> GetBingWallpaperAsync()
        {
            var httpClient = new Windows.Web.Http.HttpClient();
            Windows.Web.Http.HttpResponseMessage httpResponse = new Windows.Web.Http.HttpResponseMessage();
            string httpResponseBody = "";

            httpResponse = await httpClient.GetAsync(BING_URI);
            if (!httpResponse.IsSuccessStatusCode)
            {
                return null;
            }

            httpResponseBody = await httpResponse.Content.ReadAsStringAsync();

            HtmlAgilityPack.HtmlDocument htmlDocument = new HtmlAgilityPack.HtmlDocument();
            htmlDocument.LoadHtml(httpResponseBody);

            var data = htmlDocument.DocumentNode.Descendants("urlBase");
            if (data == null) return null;
            string url = "https://www.bing.com" + data.First().InnerText + "_1920x1080.jpg";

            return new Uri(url);
        }
    }
}
