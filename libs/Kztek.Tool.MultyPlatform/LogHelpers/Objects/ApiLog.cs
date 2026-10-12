using Kztek.Object;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Kztek.Tool
{
    public class ApiLog : BaseLog
    {
        [JsonIgnore]
        public EmSystemAction Action { get; set; } = EmSystemAction.MainServer;
        [JsonIgnore]
        public EmSystemActionType ActionType { get; set; } = EmSystemActionType.INFO;
        [JsonIgnore]
        public EmSystemActionDetail ActionDetail { get; set; } = EmSystemActionDetail.CREATE;

        public string EndPoint { get; set; } = string.Empty;
        public DateTime StartTime { get; set; } = DateTime.Now;
        public DateTime EndTime { get; set; } = DateTime.Now;
        public double Duration
        {
            get
            {
                return (EndTime - StartTime).TotalMilliseconds;
            }
            set
            {
                duration = value;
            }
        }
        public string ApiMethod { get; set; } = string.Empty;
        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> Params { get; set; } = new Dictionary<string, string>();
        public object? Body { get; set; } = null;
        public int ResponseStatus { get; set; } = 0;
        public string ResponseContent { get; set; } = string.Empty;
        public string ResponseErrorMessage { get; set; } = string.Empty;
        public object? Ex { get; set; } = null;
        private double duration = 0;
       
    }
}

