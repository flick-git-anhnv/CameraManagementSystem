using Kztek.Object;

namespace iParkingv8.Object.ConfigObjects.OEMConfigs
{
    public class OEMConfig
    {
        public string AppName { get; set; } = "KZTEK";
        public string CompanyName { get; set; } = "KZTEK";
        /// <summary>
        /// Ngôn ngữ giao diện. Kiểu EmLanguageCode (int) — serialize thành int trong oem.json.
        /// Default: Vi. Config cũ lưu int 0/1/2 tương thích trực tiếp; string cũ ("") sẽ parse
        /// thất bại → STJ dùng default Vi (try/catch trong LoadJson).
        /// </summary>
        public EmLanguageCode Language { get; set; } = EmLanguageCode.Vi;
        public string LogoPath { get; set; } = "";
    }
}
