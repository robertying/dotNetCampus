using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Json;

namespace CampusNet
{
    public static class AuthHelper
    {
        private static readonly string AUTH4_URL = "https://auth4.tsinghua.edu.cn/cgi-bin/srun_portal";
        private static readonly string AUTH6_URL = "https://auth6.tsinghua.edu.cn/cgi-bin/srun_portal";
        private static readonly string AUTH4_CHALLENGE_URL = "https://auth4.tsinghua.edu.cn/cgi-bin/get_challenge";
        private static readonly string AUTH6_CHALLENGE_URL = "https://auth6.tsinghua.edu.cn/cgi-bin/get_challenge";
        private static readonly string USER_AGENT = ".Net Campus";

        private static unsafe List<uint> S(string a, bool b)
        {
            int c = a.Length;
            List<uint> v = new List<uint>();
            uint value = 0;
            byte* p = (byte*)&value;
            for (int i = 0; i < c; i += 4)
            {
                p[0] = (byte)a[i];
                p[1] = (byte)(i + 1 >= c ? 0 : a[i + 1]);
                p[2] = (byte)(i + 2 >= c ? 0 : a[i + 2]);
                p[3] = (byte)(i + 3 >= c ? 0 : a[i + 3]);
                v.Add(value);
            }
            if (b)
            {
                v.Add((uint)c);
            }
            return v;
        }

        private static unsafe string L(List<uint> a, bool b)
        {
            int d = a.Count;
            uint c = ((uint)(d - 1)) << 2;
            StringBuilder aa = new StringBuilder();
            if (b)
            {
                uint m = a[d - 1];
                if (m < c - 3 || m > c)
                {
                    return null;
                }
                c = m;
            }
            uint value = 0;
            byte* p = (byte*)&value;
            for (int i = 0; i < d; i++)
            {
                value = a[i];
                aa.Append((char)p[0]);
                aa.Append((char)p[1]);
                aa.Append((char)p[2]);
                aa.Append((char)p[3]);
            }
            if (b)
            {
                return aa.ToString().Substring(0, (int)c);
            }
            else
            {
                return aa.ToString();
            }
        }

        private static string Encode(string str, string key)
        {
            if (str.Length == 0)
            {
                return String.Empty;
            }
            List<uint> v = S(str, true);
            List<uint> k = S(key, false);
            while (k.Count < 4)
            {
                k.Add(0);
            }
            int n = v.Count - 1;
            uint z = v[n];
            uint y = v[0];
            int q = 6 + 52 / (n + 1);
            uint d = 0;
            while (q-- > 0)
            {
                d += 0x9E3779B9;
                uint e = (d >> 2) & 3;
                for (int p = 0; p <= n; p++)
                {
                    y = v[p == n ? 0 : p + 1];
                    uint m = (z >> 5) ^ (y << 2);
                    m += (y >> 3) ^ (z << 4) ^ (d ^ y);
                    m += k[(p & 3) ^ (int)e] ^ z;
                    z = v[p] += m;
                }
            }
            return L(v, false);
        }

        private unsafe static string Base64Encode(string t)
        {
            string n = "LVoJPiCN2R8G90yg+hmFHuacZ1OWMnrsSTXkYpUq/3dlbfKwv6xztjI7DeBE45QA";
            StringBuilder u = new StringBuilder();
            int a = t.Length;
            char r = '=';
            int h = 0;
            byte* p = (byte*)&h;
            for (int o = 0; o < a; o += 3)
            {
                p[2] = (byte)t[o];
                p[1] = (byte)(o + 1 < a ? t[o + 1] : 0);
                p[0] = (byte)(o + 2 < a ? t[o + 2] : 0);
                for (int i = 0; i < 4; i += 1)
                {
                    if (o * 8 + i * 6 > a * 8)
                    {
                        u.Append(r);
                    }
                    else
                    {
                        u.Append(n[h >> 6 * (3 - i) & 0x3F]);
                    }
                }
            }
            return u.ToString();
        }

        private static async Task<string> GetChallengeAsync(int stack, string username)
        {
            Windows.Web.Http.HttpClient httpClient = new Windows.Web.Http.HttpClient();
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

            Windows.Web.Http.HttpResponseMessage httpResponse = new Windows.Web.Http.HttpResponseMessage();
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
            var passwordMD5 = "5e543256c480ac577d30f76f9120eb74";

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

            Windows.Web.Http.HttpClient httpClient = new Windows.Web.Http.HttpClient();
            var httpHeaders = httpClient.DefaultRequestHeaders;
            httpHeaders.UserAgent.TryParseAdd(USER_AGENT);
            Windows.Web.Http.HttpResponseMessage httpResponse = new Windows.Web.Http.HttpResponseMessage();
            string httpResponseBody = "";
            var httpForm = new Windows.Web.Http.HttpFormUrlEncodedContent(data);

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

        public static async Task<string> LogoutAsync(int stack, string username, string password)
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
                ["ip"] = JsonValue.CreateStringValue(""),
                ["acid"] = JsonValue.CreateStringValue("1"),
                ["enc_ver"] = JsonValue.CreateStringValue("s" + "run" + "_bx1")
            };

            var data = new Dictionary<string, string>
            {
                ["info"] = "{SRBX1}" + Base64Encode(Encode(info.Stringify(), token)),
                ["action"] = "logout",
                ["ac_id"] = "1",
                ["double_stack"] = "1",
                ["n"] = n,
                ["type"] = "1"
            };
            data["chksum"] = Utility.ComputeSHA1(token + username + token + "1" + token + "" + token + n + token + type + token + data["info"]);

            Windows.Web.Http.HttpClient httpClient = new Windows.Web.Http.HttpClient();
            Windows.Web.Http.HttpResponseMessage httpResponse = new Windows.Web.Http.HttpResponseMessage();
            string httpResponseBody = "";
            var httpForm = new Windows.Web.Http.HttpFormUrlEncodedContent(data);

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
