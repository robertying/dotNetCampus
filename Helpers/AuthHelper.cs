using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Web.Http;

namespace CampusNet
{
    public static class AuthHelper
    {
        private static readonly string AUTH4_URL = "https://auth4.tsinghua.edu.cn/cgi-bin/srun_portal";
        private static readonly string AUTH6_URL = "https://auth6.tsinghua.edu.cn/cgi-bin/srun_portal";
        private static readonly string AUTH4_CHALLENGE_URL = "https://auth4.tsinghua.edu.cn/cgi-bin/get_challenge";
        private static readonly string AUTH6_CHALLENGE_URL = "https://auth6.tsinghua.edu.cn/cgi-bin/get_challenge";
        private static readonly string USER_AGENT = ".Net Campus";
        private static HttpClient httpClient = new HttpClient();

        private static List<long> S(string a, bool b)
        {
            var c = a.Length;
            List<long> v = new List<long>();

            for (var i = 0; i < c; i += 4)
            {
                v.Add(
                    Convert.ToInt64(a[i]) |
                    Convert.ToInt64(i + 1 >= c ? 0 : a[i + 1]) << 8 |
                    Convert.ToInt64(i + 2 >= c ? 0 : a[i + 2]) << 16 |
                    Convert.ToInt64(i + 3 >= c ? 0 : a[i + 3]) << 24
                    );
            }

            if (b) v.Add(c);
            return v;
        }

        private static string L(List<long> a, bool b)
        {
            var d = a.Count();
            long c = (d - 1) << 2;
            List<string> aa = new List<string>();

            if (b)
            {
                var m = a[d - 1];
                if (m < c - 3 || m > c)
                {
                    return null;
                }
                c = m;
            }
            for (var i = 0; i < d; i++)
            {
                aa.Add(Convert.ToChar(a[i] & 0xff).ToString() + Convert.ToChar((a[i] >> 8) & 0xff).ToString() + Convert.ToChar((a[i] >> 16) & 0xff).ToString() + Convert.ToChar((a[i] >> 24) & 0xff).ToString());
            }

            if (b)
            {
                return String.Join("", aa).Substring(0, (int)c);
            }
            else
            {
                return String.Join("", aa);
            }
        }

        private static string Encode(string str, string key)
        {
            long RightShift(long x, int nn)
            {
                return x >> nn;
            }

            long LeftShift(long x, int nn)
            {
                return (x << nn) & 4294967295;
            }

            if (str.Length == 0) return String.Empty;

            var v = S(str, true);
            var k = S(key, false);

            while (k.Count < 4)
            {
                k.Add(0);
            }

            var n = v.Count() - 1;
            var z = v[n];
            var y = v[0];
            var c = 0x86014019 | 0x183639A0;
            var q = Math.Floor(6.0 + 52 / (n + 1));
            long d = 0;

            while (0 < q)
            {
                q -= 1;
                d = d + c & (0x8CE0D9BF | 0x731F2640);
                var e = RightShift(d, 2) & 3;
                for (int p = 0; p < n; p++)
                {
                    y = v[p + 1];
                    long mm = RightShift(z, 5) ^ LeftShift(y, 2);
                    mm += RightShift(y, 3) ^ LeftShift(z, 4) ^ (d ^ y);
                    long tt = (p & 3) ^ e;
                    mm += k[(int)tt] ^ z;
                    z = v[p] = v[p] + mm & (0xEFB8D130 | 0x10472ECF);
                }

                y = v[0];
                long m = RightShift(z, 5) ^ LeftShift(y, 2);
                m += RightShift(y, 3) ^ LeftShift(z, 4) ^ (d ^ y);
                long t = (n & 3) ^ e;
                m += k[(int)t] ^ z;
                z = v[n] = v[n] + m & (0xBB390742 | 0x44C6F8BD);
            }

            return L(v, false);
        }

        private static string Base64Encode(string t)
        {
            var n = "LVoJPiCN2R8G90yg+hmFHuacZ1OWMnrsSTXkYpUq/3dlbfKwv6xztjI7DeBE45QA";
            var u = "";
            var a = t.Length;
            var r = "=";

            for (var o = 0; o < a; o += 3)
            {
                var h = Convert.ToInt32(t[o]) << 16 | (o + 1 < a ? Convert.ToInt32(t[o + 1]) << 8 : 0) | (o + 2 < a ? Convert.ToInt32(t[o + 2]) : 0);
                for (var i = 0; i < 4; i += 1)
                {
                    if (o * 8 + i * 6 > a * 8)
                    {
                        u += r;
                    }
                    else
                    {
                        u += n.ElementAt(h >> 6 * (3 - i) & 63);
                    }
                }
            }

            return u;
        }

        private static async Task<string> GetChallengeAsync(int stack, string username)
        {

            var httpHeaders = httpClient.DefaultRequestHeaders;
            httpHeaders.UserAgent.TryParseAdd(USER_AGENT);

            string CHALLENGE_URL;
            if (stack == 4)
            {
                CHALLENGE_URL = AUTH4_CHALLENGE_URL;
            }
            else
            {
                CHALLENGE_URL = AUTH6_CHALLENGE_URL;
            }
            string queryString = CHALLENGE_URL + "?username=" + username + "&double_stack=1&ip&callback=callback";

            HttpResponseMessage httpResponse = new HttpResponseMessage();
            string httpResponseBody = "";

            httpResponse = await httpClient.GetAsync(new Uri(queryString));
            if (!httpResponse.IsSuccessStatusCode)
            {
                return null;
            }

            httpResponseBody = await httpResponse.Content.ReadAsStringAsync();
            Debug.WriteLine("AuthHelper.GetChallengeAsync(): " + httpResponseBody);

            int begin = httpResponseBody.IndexOf("{");
            int end = httpResponseBody.LastIndexOf("}");
            return httpResponseBody.Substring(begin, end - begin + 1);
        }

        public static async Task<string> LoginAsync(int stack, string username, string password)
        {
            var n = "200";
            var type = "1";

            var challenge = await GetChallengeAsync(stack, username);
            if (challenge == null) return null;
            if (JsonObject.TryParse(challenge, out JsonObject result) != true) return null;
            string token = result["challenge"].GetString();

            var info = new JsonObject()
            {
                ["username"] = JsonValue.CreateStringValue(username),
                ["password"] = JsonValue.CreateStringValue(password),
                ["ip"] = JsonValue.CreateStringValue(""),
                ["acid"] = JsonValue.CreateStringValue("1"),
                ["enc_ver"] = JsonValue.CreateStringValue("s" + "run" + "_bx1")
            };

            var passwordMD5 = Utility.ComputeMD5HMAC(token, password);

            var data = new Dictionary<string, string>
            {
                ["info"] = "{SRBX1}" + Base64Encode(Encode(info.Stringify(), token)),
                ["action"] = "login",
                ["ac_id"] = "1",
                ["double_stack"] = "1",
                ["n"] = n,
                ["type"] = "1",
                ["username"] = username,
                ["password"] = "{MD5}" + passwordMD5
            };
            data["chksum"] = Utility.ComputeSHA1(token + username + token + passwordMD5 + token + "1" + token + "" + token + n + token + type + token + data["info"]);

            var httpHeaders = httpClient.DefaultRequestHeaders;
            httpHeaders.UserAgent.TryParseAdd(USER_AGENT);
            HttpResponseMessage httpResponse = new HttpResponseMessage();
            string httpResponseBody = "";
            var httpForm = new HttpFormUrlEncodedContent(data);

            string URL;
            if (stack == 4)
            {
                URL = AUTH4_URL;
            }
            else
            {
                URL = AUTH6_URL;
            }

            httpResponse = await httpClient.PostAsync(new Uri(URL), httpForm);
            if (!httpResponse.IsSuccessStatusCode)
            {
                return null;
            }

            httpResponseBody = await httpResponse.Content.ReadAsStringAsync();
            Debug.WriteLine("AuthHelper.LoginAsync(): " + httpResponseBody);

            return httpResponseBody;
        }

        public static async Task<string> LogoutAsync(int stack, string username)
        {
            var data = new Dictionary<string, string>
            {
                ["action"] = "logout"
            };

            HttpResponseMessage httpResponse = new HttpResponseMessage();
            string httpResponseBody = "";
            var httpForm = new HttpFormUrlEncodedContent(data);

            string URL;
            if (stack == 4)
            {
                URL = AUTH4_URL;
            }
            else
            {
                URL = AUTH6_URL;
            }

            httpResponse = await httpClient.PostAsync(new Uri(URL), httpForm);
            if (!httpResponse.IsSuccessStatusCode)
            {
                return null;
            }

            httpResponseBody = await httpResponse.Content.ReadAsStringAsync();
            Debug.WriteLine("AuthHelper.LogoutAsync(): " + httpResponseBody);

            return httpResponseBody;
        }
    }
}
