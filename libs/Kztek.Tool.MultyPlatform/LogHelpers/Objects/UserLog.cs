using Kztek.Object;
using Newtonsoft.Json;

namespace Kztek.Tool
{
    public class UserLog : BaseLog
    {
        [JsonIgnore]
        public EmSystemAction SystemAction { get; set; } = EmSystemAction.USER;
        [JsonIgnore]
        public EmSystemActionDetail ActionDetail { get; set; } = EmSystemActionDetail.CREATE;
        [JsonIgnore]
        public EmSystemActionType ActionType { get; set; } = EmSystemActionType.INFO;

        public string Action { get; set; } = string.Empty;
    }
}

