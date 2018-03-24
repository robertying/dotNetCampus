using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Windows.Security.Cryptography.Certificates;
using Windows.Web.Http.Filters;

namespace CampusNet
{
    public static class NetHelper
    {
        private static string BASE_URL = "https://net.tsinghua.edu.cn";
        private static string STATUS_URL = BASE_URL + "/rad_user_info.php";
        private static string LOGIN_URL = BASE_URL + "/do_login.php";
        private static string ONLINE_URL = BASE_URL + "/wired/succeed.html?online";
        private static string USER_AGENT = "Windows NT";

        public static async Task<string> LoginAsync(string username, string password)
        {
            password = Utility.ComputeMD5(password);
            Windows.Web.Http.HttpClient httpClient = new Windows.Web.Http.HttpClient();

            string queryString = LOGIN_URL + "?action=login&username=" + username + "&password={MD5_HEX}" + password + "&ac_id=1";

            var httpHeaders = httpClient.DefaultRequestHeaders;
            httpHeaders.UserAgent.TryParseAdd(USER_AGENT);

            Windows.Web.Http.HttpResponseMessage httpResponse = new Windows.Web.Http.HttpResponseMessage();
            string httpResponseBody = "";

            try
            {
                httpResponse = await httpClient.GetAsync(new Uri(queryString));
                httpResponse.EnsureSuccessStatusCode();
                httpResponseBody = await httpResponse.Content.ReadAsStringAsync();
                Debug.WriteLine("NetHelper.LoginAsync(): " + httpResponseBody);

                return httpResponseBody;
            }
            catch (Exception ex)
            {
                httpResponseBody = "Error: " + ex.HResult.ToString("X") + " Message: " + ex.Message;
                Debug.WriteLine("NetHelper.LoginAsync(): " + httpResponseBody);
                return null;
            }
        }

        public static async Task<string> LogoutAsync()
        {
            Windows.Web.Http.HttpClient httpClient = new Windows.Web.Http.HttpClient();

            Dictionary<string, string> form = new Dictionary<string, string>
            {
                ["action"] = "logout"
            };
            var httpForm = new Windows.Web.Http.HttpFormUrlEncodedContent(form);

            Windows.Web.Http.HttpResponseMessage httpResponse = new Windows.Web.Http.HttpResponseMessage();
            string httpResponseBody = "";

            try
            {
                httpResponse = await httpClient.PostAsync(new Uri(LOGIN_URL), httpForm);
                httpResponse.EnsureSuccessStatusCode();
                httpResponseBody = await httpResponse.Content.ReadAsStringAsync();
                Debug.WriteLine("NetHelper.LogoutAsync(): " + httpResponseBody);
                return httpResponseBody;
            }
            catch (Exception ex)
            {
                httpResponseBody = "Error: " + ex.HResult.ToString("X") + " Message: " + ex.Message;
                Debug.WriteLine("NetHelper.LogoutAsync(): " + httpResponseBody);
                return null;
            }
        }

        public static async Task<Dictionary<string, object>> GetStatusAsync()
        {
            Windows.Web.Http.HttpClient httpClient = new Windows.Web.Http.HttpClient();

            Windows.Web.Http.HttpResponseMessage httpResponse = new Windows.Web.Http.HttpResponseMessage();
            string httpResponseBody = "";

            try
            {
                httpResponse = await httpClient.GetAsync(new Uri(STATUS_URL));
                httpResponse.EnsureSuccessStatusCode();
                httpResponseBody = await httpResponse.Content.ReadAsStringAsync();
                Debug.WriteLine("NetHelper.GetStatusAsync(): " + httpResponseBody);

                var info_strs = httpResponseBody.Split(',');
                Dictionary<string, object> info = new Dictionary<string, object>
                {
                    ["username"] = info_strs[0],
                    ["start_time"] = DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(info_strs[1])),
                    ["usage"] = Convert.ToInt64(info_strs[3]),
                    ["total_usage"] = Convert.ToInt64(info_strs[6]),
                    ["ip"] = info_strs[8],
                    ["balance"] = Convert.ToDouble(info_strs[11])
                };
                return info;
            }
            catch (Exception ex)
            {
                httpResponseBody = "Error: " + ex.HResult.ToString("X") + " Message: " + ex.Message;
                Debug.WriteLine("NetHelper.GetStatusAsync(): " + httpResponseBody);
                return null;
            }
        }

        public static bool IsOnline()
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create("http://net.tsinghua.edu.cn");
                request.AllowAutoRedirect = false;
                request.Method = "HEAD";
                request.Timeout = 100;

                var response = request.GetResponse();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
