// G4.4: Port từ Utils.cs (WinForms).
// Thay đổi: xoá using System.Drawing (không dùng trong file này — chỉ thừa import).
// Logic tất cả method giữ nguyên 100%.
using System;
using System.Text;
using System.Text.RegularExpressions;

namespace Kztek.Cameras
{
    public enum DateInterval
    {
        Year,
        Month,
        Weekday,
        Day,
        Hour,
        Minute,
        Second
    }

    public static class Utils
    {
        public static string RemoveUseLess(string st)
        {
            for (int i = st.Length - 1; i >= 0; i--)
            {
                char ch = char.ToUpper(st[i]);
                if ((ch < 'A' || ch > 'Z') && (ch < '0' || ch > '9'))
                {
                    st = st.Remove(i, 1);
                }
            }
            return st;
        }

        public static string MatchOneStr(ref string pattern, ref string test)
        {
            string result = "";
            try
            {
                result = Regex.Match(test, pattern).Groups["val"].ToString();
            }
            catch
            {
            }
            return result;
        }

        public static int MatchOneInt(ref string pattern, ref string test)
        {
            int iRet = 0;
            string RetStr = Utils.MatchOneStr(ref pattern, ref test);
            try
            {
                iRet = Convert.ToInt32(RetStr);
            }
            catch
            {
            }
            return iRet;
        }

        public static byte[] GetArrayASCII(ref string text)
        {
            byte[] result;
            try
            {
                result = Encoding.ASCII.GetBytes(text);
            }
            catch
            {
                result = null;
            }
            return result;
        }

        public static string GetStringASCII(ref byte[] text)
        {
            string GetStringASCII;
            try
            {
                GetStringASCII = Encoding.ASCII.GetString(text);
            }
            catch
            {
                GetStringASCII = "";
            }
            return GetStringASCII;
        }

        public static string GetB64String(ref string text)
        {
            byte[] temp = Utils.GetArrayASCII(ref text);
            return Utils.GetB64String(ref temp);
        }

        public static string GetB64String(ref byte[] text)
        {
            return Utils.GetB64String(ref text, 0, text.Length);
        }

        public static string GetB64String(ref byte[] text, int i_sod, int i_len)
        {
            string result = "";
            try
            {
                result = Convert.ToBase64String(text, i_sod, i_len);
            }
            catch
            {
                return "";
            }
            return result;
        }

        public static long DateDiff(DateInterval interval, DateTime date1, DateTime date2)
        {
            TimeSpan ts = date2 - date1;
            switch (interval)
            {
                case DateInterval.Year:
                    return (long)(date2.Year - date1.Year);
                case DateInterval.Month:
                    return (long)(date2.Month - date1.Month + 12 * (date2.Year - date1.Year));
                case DateInterval.Weekday:
                    return Utils.Fix(ts.TotalDays) / 7L;
                case DateInterval.Day:
                    return Utils.Fix(ts.TotalDays);
                case DateInterval.Hour:
                    return Utils.Fix(ts.TotalHours);
                case DateInterval.Minute:
                    return Utils.Fix(ts.TotalMinutes);
                default:
                    return Utils.Fix(ts.TotalSeconds);
            }
        }

        private static long Fix(double Number)
        {
            if (Number >= 0.0)
            {
                return (long)Math.Floor(Number);
            }
            return (long)Math.Ceiling(Number);
        }
    }
}
