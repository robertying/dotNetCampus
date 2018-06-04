using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Web.Http;

namespace CampusNet
{
    public static class AssetsHelper
    {
        private static readonly Uri BING_URI = new Uri("https://www.bing.com/HPImageArchive.aspx?format=xml&idx=0&n=1&mkt=en-US");
        private static HttpClient httpClient = new HttpClient();

        public static async Task<Uri> GetBingWallpaperAsync()
        {
            HttpResponseMessage httpResponse = new HttpResponseMessage();
            string httpResponseBody = "";

            try
            {
                httpResponse = await httpClient.GetAsync(BING_URI);
            }
            catch
            {
                return null;
            }
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
