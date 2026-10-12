namespace Kztek.Object.MultyPlatform.Device
{
    public class BarrieOpenModeConfig
    {
        public int BarrieIndex { get; set; }
        public EmBarrieOpenMode OpenMode { get; set; } = EmBarrieOpenMode.ALL;
        /// <summary>
        /// Mã âm thanh (index) gửi xuống bộ điều khiển của controller này, theo từng loại âm thanh hệ thống.
        /// Key là tên enum EmSystemSoundType (dạng string để tránh phụ thuộc ngược vào iParkingv8.Object).
        /// Loại âm thanh nào không có trong map sẽ dùng mã mặc định (0).
        /// </summary>
        public Dictionary<string, int> SoundIndexMap { get; set; } = new Dictionary<string, int>();
    }
}
