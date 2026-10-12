using Kztek.Object;
using Newtonsoft.Json;

namespace Kztek.Tool
{
    public class DeviceLog : BaseLog
    {
        [JsonIgnore]
        public EmSystemAction Action { get; set; } = EmSystemAction.DEVICE;
        [JsonIgnore]
        public EmSystemActionType ActionType { get; set; } = EmSystemActionType.INFO;
        [JsonIgnore]
        public EmSystemActionDetail ActionDetail { get; set; } = EmSystemActionDetail.CREATE;

        public string DeviceId { get; set; } = string.Empty;
        public string DeviceName { get; set; } = string.Empty;
        public string Cmd { get; set; } = string.Empty;
        public string Response { get; set; } = string.Empty;

        public DeviceLog() { }
        public DeviceLog(string deviceId, string deviceName, string cmd, object? ex = null)
        {
            DeviceId = deviceId;
            DeviceName = deviceName;
            Cmd = cmd;
            Description = ex == null ? "" : Newtonsoft.Json.JsonConvert.SerializeObject(ex);
        }
    }
}

