using Newtonsoft.Json;
using System;

namespace Kztek.Tool
{
    public class BaseLog
    {
        [JsonIgnore]
        public string Id { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        [JsonIgnore]
        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}

