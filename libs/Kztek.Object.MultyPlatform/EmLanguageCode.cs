namespace Kztek.Object
{
    /// <summary>Ngôn ngữ giao diện ứng dụng được hỗ trợ.</summary>
    public enum EmLanguageCode
    {
        /// <summary>Tiếng Việt.</summary>
        Vi = 0,
        /// <summary>English (Tiếng Anh).</summary>
        En = 1,
        /// <summary>ລາວ (Tiếng Lào).</summary>
        LoLa = 2,
        /// <summary>日本語 (Tiếng Nhật).</summary>
        Ja = 3,
        /// <summary>한국어 (Tiếng Hàn).</summary>
        Ko = 4,
        /// <summary>中文 (Tiếng Trung).</summary>
        Zh = 5,
    }

    /// <summary>Tiện ích tra cứu tên hiển thị của ngôn ngữ.</summary>
    public class AppLanguage
    {
        /// <summary>
        /// Trả về tên đầy đủ của ngôn ngữ để hiển thị trên giao diện.
        /// </summary>
        /// <param name="language">Mã ngôn ngữ.</param>
        /// <returns>Tên ngôn ngữ (ví dụ: "Tiếng Việt", "English", "ລາວ", "日本語", "한국어", "中文").</returns>
        public static string GetLanguage(EmLanguageCode language)
        {
            return language switch
            {
                EmLanguageCode.Vi   => "Tiếng Việt",
                EmLanguageCode.En   => "English",
                EmLanguageCode.LoLa => "ລາວ",
                EmLanguageCode.Ja   => "日本語",   // khớp AppLocalizationService.JapaneseSettingValue
                EmLanguageCode.Ko   => "한국어",    // khớp AppLocalizationService.KoreanSettingValue
                EmLanguageCode.Zh   => "中文",      // khớp AppLocalizationService.ChineseSettingValue
                _                   => "",
            };
        }
    }
}
