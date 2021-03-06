﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Windows.Web.Http;

namespace CampusNet
{
    public static class NetHelper
    {
        private static readonly string BASE_URL = "https://net.tsinghua.edu.cn";
        private static readonly string STATUS_URL = BASE_URL + "/rad_user_info.php";
        private static readonly string LOGIN_URL = BASE_URL + "/do_login.php";
        private static readonly string ONLINE_URL = BASE_URL + "/wired/succeed.html?online";
        private static readonly string USER_AGENT = ".Net Campus";
        private static HttpClient httpClient = new HttpClient();

        public static async Task<string> LoginAsync(string username, string password)
        {
            string queryString = LOGIN_URL + "?action=login&username=" + username + "&password={MD5_HEX}" + password + "&ac_id=1";

            var httpHeaders = httpClient.DefaultRequestHeaders;
            httpHeaders.UserAgent.TryParseAdd(USER_AGENT);

            HttpResponseMessage httpResponse = new HttpResponseMessage();
            string httpResponseBody = "";
            var cancellationTokenSource = new CancellationTokenSource(1000);

            try
            {
                httpResponse = await httpClient.GetAsync(new Uri(queryString)).AsTask(cancellationTokenSource.Token);
            }
            catch
            {
                return null;
            }

            if (httpResponse.IsSuccessStatusCode)
            {
                httpResponseBody = await httpResponse.Content.ReadAsStringAsync();
                Debug.WriteLine("NetHelper.LoginAsync(): " + httpResponseBody);
                return httpResponseBody;
            }
            else
            {
                return null;
            }
        }

        public static async Task<string> LogoutAsync()
        {
            Dictionary<string, string> form = new Dictionary<string, string>
            {
                ["action"] = "logout"
            };
            var httpForm = new HttpFormUrlEncodedContent(form);

            HttpResponseMessage httpResponse = new HttpResponseMessage();
            string httpResponseBody = "";
            var cancellationTokenSource = new CancellationTokenSource(1000);

            try
            {
                httpResponse = await httpClient.PostAsync(new Uri(LOGIN_URL), httpForm);
            }
            catch
            {
                return null;
            }

            if (httpResponse.IsSuccessStatusCode)
            {
                httpResponseBody = await httpResponse.Content.ReadAsStringAsync();
                Debug.WriteLine("NetHelper.LogoutAsync(): " + httpResponseBody);
                return httpResponseBody;
            }
            else
            {
                return null;
            }
        }

        public static async Task<Dictionary<string, object>> GetStatusAsync()
        {
            HttpResponseMessage httpResponse = new HttpResponseMessage();
            string httpResponseBody = "";
            var cancellationTokenSource = new CancellationTokenSource(1000);

            try
            {
                httpResponse = await httpClient.GetAsync(new Uri(STATUS_URL));
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
            if (httpResponseBody == "") return null;
            Debug.WriteLine("NetHelper.GetStatusAsync(): " + httpResponseBody);

            var info_strs = httpResponseBody.Split(',');
            if (info_strs == null) return null;

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

        public static async Task<bool> IsOnline()
        {
            var cancellationTokenSource = new CancellationTokenSource(1000);
            HttpResponseMessage httpResponse = new HttpResponseMessage();

            try
            {
                httpResponse = await httpClient.GetAsync(new Uri("https://net.tsinghua.edu.cn")).AsTask(cancellationTokenSource.Token);
            }
            catch
            {
                return false;
            }

            if (httpResponse.IsSuccessStatusCode)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static async Task<bool> IsCampus()
        {
            var cancellationTokenSource = new CancellationTokenSource(1000);
            HttpResponseMessage httpResponse = new HttpResponseMessage();

            try
            {
                httpResponse = await httpClient.GetAsync(new Uri("https://usereg.tsinghua.edu.cn")).AsTask(cancellationTokenSource.Token);
            }
            catch
            {
                return false;
            }

            if (httpResponse.IsSuccessStatusCode)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
