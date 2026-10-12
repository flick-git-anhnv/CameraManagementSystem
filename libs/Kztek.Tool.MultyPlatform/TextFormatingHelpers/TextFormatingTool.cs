using Newtonsoft.Json;
using System;
using System.Globalization;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;

namespace Kztek.Tool.TextFormatingHelpers
{
    public class TextFormatingTool
    {
        #region: Money Format
        public static string ConvertToMoneyFormat<T>(T money) where T : IComparable, IConvertible, IFormattable
        {
            CultureInfo culutreInfo = new CultureInfo("vi-VN");
            culutreInfo.DateTimeFormat.ShortDatePattern = "dd/MM/yyyy";
            culutreInfo.DateTimeFormat.DateSeparator = "/";
            culutreInfo.NumberFormat.PercentDecimalSeparator = ".";
            return money.ToString("C0", culutreInfo);
        }

        public static string GetMoneyFormat(string str_money)
        {
            try
            {
                long money = Convert.ToInt64(str_money.Trim());
                CultureInfo culutreInfo = new CultureInfo("vi-VN");
                culutreInfo.DateTimeFormat.ShortDatePattern = "dd/MM/yyyy";
                culutreInfo.DateTimeFormat.DateSeparator = "/";
                culutreInfo.NumberFormat.PercentDecimalSeparator = ".";
                string a = money.ToString("C0", culutreInfo);
                a = a.Remove(a.Length - 1);
                a += "VND";
                return a;
            }
            catch (Exception)
            {
                return "";
            }

        }

        public static T MoneyToSpecific<T>(string money) where T : IComparable, IConvertible, IFormattable
        {

            CultureInfo culutreInfo = new CultureInfo("vi-VN");
            culutreInfo.DateTimeFormat.ShortDatePattern = "dd/MM/yyyy";
            culutreInfo.DateTimeFormat.DateSeparator = "/";
            culutreInfo.NumberFormat.PercentDecimalSeparator = ".";
            try
            {
                while (money[0] == '0')
                {
                    money = money[1..];
                }
                return (T)Convert.ChangeType(decimal.Parse(money, NumberStyles.Currency, culutreInfo), typeof(T));
            }
            catch (Exception)
            {
                return (T)Convert.ChangeType(decimal.Parse("-1", NumberStyles.Currency, culutreInfo), typeof(T));
            }

        }
        #endregion

        #region: Char Helper
        public static char NextCharacter(char input)
        {
            return input == 'z' ? 'a' : (char)(input + 1);
        }
        public static string ConvertToACSIIString(string s)
        {
            Regex regex = new Regex("\\p{IsCombiningDiacriticalMarks}+");
            string temp = s.Normalize(NormalizationForm.FormD);
            return regex.Replace(temp, string.Empty).Replace('\u0111', 'd').Replace('\u0110', 'D');
        }
        #endregion

        #region: Render
        //public static Size GetTextSize(string data, Font font)
        //{
        //    return TextRenderer.MeasureText(data, font);
        //}
        #endregion: End Render

        public static string StandardlizePlateNumber(string plateNunber)
        {
            plateNunber = plateNunber.Trim();
            plateNunber = plateNunber.ToUpper();
            plateNunber = plateNunber.Replace("-", string.Empty);
            plateNunber = plateNunber.Replace("_", string.Empty);
            plateNunber = plateNunber.Replace(".", string.Empty);
            return plateNunber;
        }
        public static string GetLogMessage(object? obj)
        {
            if (obj == null)
            {
                return "_";
            }
            if (obj is string v)
            {
                return v;
            }
            else
            {
                return Newtonsoft.Json.JsonConvert.SerializeObject(obj);
            }
        }
        #region: Internal
        public static string BeautyJson(object? obj)
        {
            if (obj == null) { return ""; }

            if (obj is string)
            {
                return (obj as string)!;
            }

            try
            {
                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    DefaultValueHandling = DefaultValueHandling.Ignore,

                };
                var jsonString = JsonConvert.SerializeObject(obj, Formatting.Indented, settings);//.Replace("\r\n", "\r").Replace("\n", "\r").Replace("\r", "<br>\r\n").Replace("  ", " &nbsp;");
                return jsonString;
            }
            catch (Exception)
            {
                return "";
            }

        }
        #endregion: End Internal
    }
}

