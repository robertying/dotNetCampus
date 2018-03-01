using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.Storage.Streams;

namespace CampusNet
{
    public static class Utility
    {
        public static string ComputeMD5(string str)
        {
            var alg = HashAlgorithmProvider.OpenAlgorithm(HashAlgorithmNames.Md5);
            IBuffer buff = CryptographicBuffer.ConvertStringToBinary(str, BinaryStringEncoding.Utf8);
            var hashed = alg.HashData(buff);
            var res = CryptographicBuffer.EncodeToHexString(hashed);
            return res;
        }

        public static double ParseUsageString(string usage)
        {
            double num = Convert.ToDouble(usage.Substring(0, usage.Count() - 1));
            char unit = Char.ToUpper(usage[usage.Count() - 1]);

            var ratio = 1e9;
            switch (unit)
            {
                case 'B': ratio = 1; break;
                case 'K': ratio = 1e3; break;
                case 'M': ratio = 1e6; break;
                case 'G': ratio = 1e9; break;
            }

            return Math.Round(num * ratio);
        }

        public static string GetTimeDescription(DateTime t)
        {
            var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
            const int SECOND = 1;
            const int MINUTE = 60 * SECOND;
            const int HOUR = 60 * MINUTE;
            const int DAY = 24 * HOUR;
            const int MONTH = 30 * DAY;

            var ts = new TimeSpan(DateTime.Now.Ticks - t.Ticks);
            double delta = Math.Abs(ts.TotalSeconds);

            if (delta < 1 * MINUTE)
                return ts.Seconds == 1 ? resourceLoader.GetString("OneSecondAgo") : ts.Seconds + resourceLoader.GetString("SecondsAgo");

            if (delta < 2 * MINUTE)
                return resourceLoader.GetString("AMinuteAgo");

            if (delta < 45 * MINUTE)
                return ts.Minutes + resourceLoader.GetString("MinutesAgo");

            if (delta < 90 * MINUTE)
                return resourceLoader.GetString("AnHourAgo");

            if (delta < 24 * HOUR)
                return ts.Hours + resourceLoader.GetString("HoursAgo");

            if (delta < 48 * HOUR)
                return resourceLoader.GetString("Yesterday");

            if (delta < 30 * DAY)
                return ts.Days + resourceLoader.GetString("DaysAgo");

            if (delta < 12 * MONTH)
            {
                int months = Convert.ToInt32(Math.Floor((double)ts.Days / 30));
                return months <= 1 ? resourceLoader.GetString("OneMonthAgo") : months + resourceLoader.GetString("MonthsAgo");
            }
            else
            {
                int years = Convert.ToInt32(Math.Floor((double)ts.Days / 365));
                return years <= 1 ? resourceLoader.GetString("OneYearAgo") : years + resourceLoader.GetString("YearsAgo");
            }
        }

        public static string GetUsageDescription(double usage)
        {
            if (usage < 1e3)
                return usage.ToString() + " B";
            else if (usage < 1e6)
                return (usage / 1e3).ToString("N2") + " KB";
            else if (usage < 1e9)
                return (usage / 1e6).ToString("N2") + " MB";
            else
                return (usage / 1e9).ToString("N2") + " GB";
        }

        public static string GetBalanceDescription(double balance)
        {
            return "￥" + balance.ToString("N2");
        }
    }
}
