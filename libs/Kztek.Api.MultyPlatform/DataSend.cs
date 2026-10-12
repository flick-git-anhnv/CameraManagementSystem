using System;
using System.Collections.Generic;
using System.Text;

namespace Kztek.Api
{
    public class DataSend
    {
        public string Url { get; set; } = string.Empty;
        public string Method { get; set; } = string.Empty;
        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> Params { get; set; } = new Dictionary<string, string>();
        public object? Data { get; set; } = default;
        public string ErrorMessage { get; set; } = string.Empty;
    }
}
