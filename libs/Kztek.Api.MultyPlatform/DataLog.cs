using System;
using System.Collections.Generic;
using System.Text;

namespace Kztek.Api
{
    public class DataLog
    {
        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> Params { get; set; } = new Dictionary<string, string>();
        public object? Data { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }
}
