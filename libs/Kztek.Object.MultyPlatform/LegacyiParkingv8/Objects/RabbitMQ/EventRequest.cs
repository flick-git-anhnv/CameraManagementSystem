using iParkingv8.Object.Objects.Events;

namespace Kztek.Object.MultyPlatform.LegacyiParkingv8.Objects.RabbitMQ
{
    public class EventRequest
    {
        public string RequestId { get; set; } = string.Empty;
        public string DeviceId { get; set; } = string.Empty;
        public string LaneId { get; set; } = string.Empty;
        public uint Action { get; set; } = (uint)EmRequestAction.CONFIRM;
        public uint SendFrom { get; set; } = (uint)EmRequestSendFrom.APP;
        public string Message { get; set; } = string.Empty;
        public uint TargetType { get; set; } = (uint)EmRequestTargetType.EXIT;
        public BaseEvent? Data { get; set; }
        public List<EventImageDto> Images { get; set; } = [];

        public string Type { get; set; } = "";
        private string upn = "";
        public string Upn
        {
            get => string.IsNullOrEmpty(upn) ? StaticPool.GetSelectedUserName() : upn;
            set { upn = value; }
        }
    }

}
