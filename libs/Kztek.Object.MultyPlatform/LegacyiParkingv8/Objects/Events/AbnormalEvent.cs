using iParkingv8.Object.Objects.Devices;
using iParkingv8.Object.Objects.Parkings;
using System.Text.Json.Serialization;

namespace iParkingv8.Object.Objects.Events
{
    public class AbnormalEvent
    {
        public string Id { get; set; } = string.Empty;
        public string LaneId { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public string PlateNumber { get; set; } = string.Empty;
        public string Note { get; set; } = string.Empty;
        public bool Deleted { get; set; }
        public string CreatedUtc { get; set; } = string.Empty;
        public string CustomerId { get; set; } = string.Empty;
        public AccessKey? AccessKey { get; set; }
        public DeviceData? Device { get; set; }
        public int Code { get; set; }
        public List<EventImageDto> Images { get; set; } = [];
        [JsonIgnore]
        public DateTime? AlarmTime
        {
            get
            {
                try
                {
                    if (string.IsNullOrEmpty(CreatedUtc))
                    {
                        return null;
                    }
                    if (CreatedUtc.Contains('T'))
                    {
                        return DateTime.ParseExact(CreatedUtc[.."yyyy-MM-ddTHH:mm:ss".Length], "yyyy-MM-ddTHH:mm:ss", null).AddHours(7);
                    }
                    else
                    {
                        return DateTime.Parse(CreatedUtc).AddHours(7);
                    }
                }
                catch
                {
                    return null;
                }
            }
        }
    }
}