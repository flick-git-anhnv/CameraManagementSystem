using Kztek.Object;
using System.Collections.Generic;

namespace iParkingv6.Objects.Datas
{
    public class Bdk
    {
        public string Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;

        public int CommunicationType { get; set; }
        public string Comport { get; set; } = string.Empty;
        public string Baudrate { get; set; } = string.Empty;

        private int type = 0;
        public int TypeCode
        {
            get => type; set
            {
                type = value;
            }
        }
        public int Type
        {
            get => type;
            set
            {
                type = value;
            }
        }

        public string ComputerId { get; set; } = string.Empty;
        public bool Enabled { get; set; }

        public string CreatedUtc { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public object UpdatedUtc { get; set; } = string.Empty;
        public object UpdatedBy { get; set; } = string.Empty;

        public bool IsConnect { get; set; } = false;
        public int OutputCount { get; set; }
        public List<CardFormatConfig> configs { get; set; } = new List<CardFormatConfig>();
    }
}
