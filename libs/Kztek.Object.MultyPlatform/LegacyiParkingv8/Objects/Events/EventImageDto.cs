using Kztek.Object.MultyPlatform.LegacyiParkingv8.Enums.Bases;

namespace iParkingv8.Object.Objects.Events
{
    public class EventImageDto
    {
        public string PresignedUrl { get; set; } = string.Empty;
        public EmImageType Type { get; set; } = EmImageType.VEHICLE;
        public string Base64 { get; set; } = string.Empty;
    }
}
