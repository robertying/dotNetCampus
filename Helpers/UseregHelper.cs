using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace CampusNet
{
    public class UsageWithDate
    {
        public int Date { get; set; }
        public double Usage { get; set; }

        public UsageWithDate()
        {
            Date = 0;
            Usage = 0;
        }
    }

    public static class UseregHelper
    {
        private static readonly string BASE_URL = "https://usereg.tsinghua.edu.cn";
        private static readonly string LOGIN_URL = BASE_URL + "/do.php";
        private static readonly string INFO_URL = BASE_URL + "/user_info.php";
        private static readonly string SESSIONS_URL = BASE_URL + "/online_user_ipv4.php";
        private static readonly string DETAIL_URL = BASE_URL + "/user_detail_list.php";

        public static Dictionary<string, object> Info { get; set; }

        public static async Task<string> LoginAsync(string username, string password)
        {
            Windows.Web.Http.HttpClient httpClient = new Windows.Web.Http.HttpClient();

            Dictionary<string, string> form = new Dictionary<string, string>
            {
                ["action"] = "login",
                ["user_login_name"] = username,
                ["user_password"] = password
            };
            var httpForm = new Windows.Web.Http.HttpFormUrlEncodedContent(form);

            Windows.Web.Http.HttpResponseMessage httpResponse = new Windows.Web.Http.HttpResponseMessage();
            string httpResponseBody = "";

            try
            {
                httpResponse = await httpClient.PostAsync(new Uri(LOGIN_URL), httpForm);
                httpResponse.EnsureSuccessStatusCode();
                httpResponseBody = await httpResponse.Content.ReadAsStringAsync();
                Debug.WriteLine("UseregHelper.LoginAsync(): " + httpResponseBody);
                return httpResponseBody;
            }
            catch (Exception ex)
            {
                httpResponseBody = "Error: " + ex.HResult.ToString("X") + " Message: " + ex.Message;
                Debug.WriteLine("UseregHelper.LoginAsync(): " + httpResponseBody);
                return null;
            }
        }

        public static async Task<string> LogoutAsync(string username, string password, string id)
        {
            if (await LoginAsync(username, password) == "ok")
            {
                Windows.Web.Http.HttpClient httpClient = new Windows.Web.Http.HttpClient();

                Dictionary<String, String> form = new Dictionary<string, string>
                {
                    ["action"] = "drops",
                    ["user_ip"] = id + ','
                };
                var httpForm = new Windows.Web.Http.HttpFormUrlEncodedContent(form);

                Windows.Web.Http.HttpResponseMessage httpResponse = new Windows.Web.Http.HttpResponseMessage();
                string httpResponseBody = "";

                try
                {
                    httpResponse = await httpClient.PostAsync(new Uri(SESSIONS_URL), httpForm);
                    httpResponse.EnsureSuccessStatusCode();
                    httpResponseBody = await httpResponse.Content.ReadAsStringAsync();
                    Debug.WriteLine("UseregHelper.LogoutAsync(): " + httpResponseBody);
                    return httpResponseBody;
                }
                catch (Exception ex)
                {
                    httpResponseBody = "Error: " + ex.HResult.ToString("X") + " Message: " + ex.Message;
                    Debug.WriteLine("UseregHelper.LoginAsync(): " + httpResponseBody);
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        public static async Task<int> GetSessionNumberAsync(string username, string password)
        {
            if (await LoginAsync(username, password) == "ok")
            {
                Windows.Web.Http.HttpClient httpClient = new Windows.Web.Http.HttpClient();
                Windows.Web.Http.HttpResponseMessage httpResponse = new Windows.Web.Http.HttpResponseMessage();
                string httpResponseBody = "";

                try
                {
                    httpResponse = await httpClient.GetAsync(new Uri(SESSIONS_URL));
                    httpResponse.EnsureSuccessStatusCode();
                    httpResponseBody = await httpResponse.Content.ReadAsStringAsync();

                    HtmlDocument htmlDocument = new HtmlDocument();
                    htmlDocument.LoadHtml(httpResponseBody);
                    var data = htmlDocument.DocumentNode.Descendants("input").Where(d =>
                        d.Attributes.Contains("type") && d.Attributes["type"].Value.Contains("checkbox"));
                    return data.Count() - 1;
                }
                catch (Exception ex)
                {
                    httpResponseBody = "Error: " + ex.HResult.ToString("X") + " Message: " + ex.Message;
                    Debug.WriteLine("UseregHelper.GetSessionNumberAsync(): " + httpResponseBody);
                    return -1;
                }
            }
            else
            {
                return -1;
            }
        }

        public static async Task<Dictionary<string, object>> GetInfoAsync(string username, string password)
        {
            if (await LoginAsync(username, password) == "ok")
            {
                Windows.Web.Http.HttpClient httpClient = new Windows.Web.Http.HttpClient();

                Windows.Web.Http.HttpResponseMessage httpResponse = new Windows.Web.Http.HttpResponseMessage();
                string infoPage = "";
                string sessionPage = "";

                try
                {
                    httpResponse = await httpClient.GetAsync(new Uri(INFO_URL));
                    httpResponse.EnsureSuccessStatusCode();
                    infoPage = await httpResponse.Content.ReadAsStringAsync();

                    httpResponse = await httpClient.GetAsync(new Uri(SESSIONS_URL));
                    httpResponse.EnsureSuccessStatusCode();
                    sessionPage = await httpResponse.Content.ReadAsStringAsync();

                    return ParsePages(infoPage, sessionPage);
                }
                catch (Exception ex)
                {
                    infoPage = "Error: " + ex.HResult.ToString("X") + " Message: " + ex.Message;
                    Debug.WriteLine("UseregHelper.GetInfo(): (info) " + infoPage + " (session) " + sessionPage);
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        private static Dictionary<string, object> ParsePages(string infoPage, string sessionPage)
        {
            HtmlDocument infoPageHTML = new HtmlDocument();
            infoPageHTML.LoadHtml(infoPage);
            var data = infoPageHTML.DocumentNode.Descendants("td").Where(d =>
                d.Attributes.Contains("class") && d.Attributes["class"].Value.Contains("maintd"));

            Dictionary<string, string> allInfo = new Dictionary<string, string>();
            Info = new Dictionary<string, object>();
            for (int i = 1; i < data.Count(); i += 2)
            {
                allInfo[data.ElementAt(i - 1).InnerText.Trim()] = data.ElementAt(i).InnerText.Trim();
            }

            Regex regex = new Regex(@"\d+");
            Info["usage"] = regex.Match(allInfo["使用流量(IPV4)"]).ToString();
            regex = new Regex(@"\d+\.\d+");
            Info["balance"] = regex.Match(allInfo["帐户余额"]).ToString();


            const int ROW_LENGTH = 14;
            HtmlDocument sessionPageHTML = new HtmlDocument();
            sessionPageHTML.LoadHtml(sessionPage);
            data = sessionPageHTML.DocumentNode.Descendants("td").Where(d =>
                d.Attributes.Contains("class") && d.Attributes["class"].Value.Contains("maintd"));

            Info["sessions"] = new ObservableCollection<Session>();
            for (var i = ROW_LENGTH; i <= data.Count() - ROW_LENGTH; i += ROW_LENGTH)
            {
                (Info["sessions"] as ObservableCollection<Session>).Add(new Session
                {
                    ID = data.ElementAt(i).Descendants("input").First().GetAttributeValue("value", "error"),
                    IP = data.ElementAt(i + 1).InnerText.Trim(),
                    StartTime = Utility.GetTimeDescription(DateTime.Parse(data.ElementAt(i + 2).InnerText.Trim().Replace(' ', 'T') + "+08:00")),
                    Usage = Utility.GetUsageDescription(Utility.ParseUsageString(data.ElementAt(i + 3).InnerText.Trim())),
                    DeviceName = data.ElementAt(i + 11).InnerText.Trim()
                });
            }

            return Info;
        }

        public static async Task<List<UsageWithDate>> GetDetailUsageForChart()
        {
            Windows.Web.Http.HttpClient httpClient = new Windows.Web.Http.HttpClient();
            Windows.Web.Http.HttpResponseMessage httpResponse = new Windows.Web.Http.HttpResponseMessage();
            string httpResponseBody = "";
            double dayUsage = 0;
            List<UsageWithDate> DetailUsage = new List<UsageWithDate>();

            for (int i = 1; i <= DateTime.Now.Day; ++i)
            {
                string month = "";
                if (DateTime.Now.Month <= 9)
                {
                    month = '0' + DateTime.Now.Month.ToString();
                }
                else
                {
                    month = DateTime.Now.Month.ToString();
                }

                string day = "";
                if (i <= 9)
                {
                    day = '0' + i.ToString();
                }
                else
                {
                    day = i.ToString();
                }

                string queryDate = DateTime.Now.Year.ToString() + '-' + month + '-' + day;
                string queryString = '?' + "action=query&" +
                                     "start_time=" + queryDate + '&' +
                                     "end_time=" + queryDate + '&' +
                                     "offset=10000";

                try
                {
                    httpResponse = await httpClient.GetAsync(new Uri(DETAIL_URL + queryString));
                    httpResponse.EnsureSuccessStatusCode();
                    httpResponseBody = await httpResponse.Content.ReadAsStringAsync();

                    HtmlDocument detailHTML = new HtmlDocument();
                    detailHTML.LoadHtml(httpResponseBody);
                    var data = detailHTML.DocumentNode.Descendants("td").Where(d =>
                    d.Attributes.Contains("align") && d.Attributes["align"].Value.Contains("right"));

                    for (int j = 2; j < data.Count(); j = j + 4)
                    {
                        dayUsage += Utility.ParseUsageString(data.ElementAt(j).InnerText);
                    }
                    DetailUsage.Add(new UsageWithDate
                    {
                        Date = i,
                        Usage = dayUsage / 1e9
                    });
                }
                catch (Exception ex)
                {
                    httpResponseBody = "Error: " + ex.HResult.ToString("X") + " Message: " + ex.Message;
                    Debug.WriteLine("UseregHelper.GetDetailUsageForChart(): " + httpResponseBody);
                    return null;
                }
            }
            return DetailUsage;
        }
    }
}
