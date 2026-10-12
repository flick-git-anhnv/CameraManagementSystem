using Kztek.Object;
using Newtonsoft.Json;

namespace Kztek.Tool
{
    public class SystemLog : BaseLog
    {
        [JsonIgnore]
        public EmSystemAction Action { get; set; } = EmSystemAction.Application;
        [JsonIgnore]
        public EmSystemActionDetail ActionDetail { get; set; } = EmSystemActionDetail.CREATE;
        [JsonIgnore]
        public EmSystemActionType ActionType { get; set; } = EmSystemActionType.INFO;

        public object Ex { get; set; } = string.Empty;

        public 
            SystemLog(EmSystemAction Action, EmSystemActionDetail ActionDetail, EmSystemActionType ActionType = EmSystemActionType.INFO, string description = "", object? ex = null)
        {
            this.Action = Action;
            this.ActionDetail = ActionDetail;
            this.ActionType = ActionType;
            Description = description;
            if (ex != null)
            {
                Ex = ex;
            }
            else
            {
                Ex = string.Empty;
            }
        }
        public SystemLog(string description, EmSystemAction Action = EmSystemAction.Application, EmSystemActionType ActionType = EmSystemActionType.INFO, EmSystemActionDetail ActionDetail = EmSystemActionDetail.PROCESS, object? ex = null)
        {
            this.Action = Action;
            this.ActionDetail = ActionDetail;
            this.ActionType = ActionType;
            Description = description;
            if (ex != null)
            {
                Ex = ex;
            }
            else
            {
                Ex = string.Empty;
            }
        }

        public static SystemLog CreateApplicationProccess(string description = "", object? ex = null, EmSystemActionType ActionType = EmSystemActionType.INFO)
        {
            return new SystemLog(EmSystemAction.Application, EmSystemActionDetail.PROCESS, ActionType, description, ex);
        }
        public static SystemLog CreateLoopEvent(string description = "", object? ex = null, EmSystemActionType ActionType = EmSystemActionType.INFO)
        {
            return new SystemLog(EmSystemAction.Application, EmSystemActionDetail.LOOP_EVENT, ActionType, description, ex);
        }
        public static SystemLog CreateCardEvent(string description = "", object? ex = null, EmSystemActionType ActionType = EmSystemActionType.INFO)
        {
            return new SystemLog(EmSystemAction.Application, EmSystemActionDetail.CARD_EVENT, ActionType, description, ex);
        }
    }
}

