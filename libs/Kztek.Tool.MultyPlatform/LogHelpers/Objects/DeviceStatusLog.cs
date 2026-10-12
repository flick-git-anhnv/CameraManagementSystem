using Kztek.Object;
using Newtonsoft.Json;

namespace Kztek.Tool
{
    public class DeviceStatusLog : BaseLog
    {
        [JsonIgnore]
        public EmSystemAction Action { get; set; } = EmSystemAction.DEVICE;
        [JsonIgnore]
        public EmSystemActionType ActionType { get; set; } = EmSystemActionType.INFO;
        [JsonIgnore]
        public EmSystemActionDetail ActionDetail { get; set; } = EmSystemActionDetail.CREATE;

        public string DeviceId { get; set; } = string.Empty;
        public string DeviceName { get; set; } = string.Empty;
        public bool DeviceStatus { get; set; } = false;
        public string Response { get; set; } = string.Empty;
    }
}

