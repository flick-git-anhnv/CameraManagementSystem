namespace iParkingv8.Object.Objects.Devices
{
    public class Camera
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string HttpPort { get; set; } = "80";
        public string RtspPort { get; set; } = "554";
        public string Username { get; set; } = "admin";
        public string Password { get; set; } = "Kztek123456";
        public int FrameRate { get; set; }
        public string Resolution { get; set; } = "1920x1080";
        public int Channel { get; set; }
        public int Type { get; set; }
        public string ComputerId { get; set; } = string.Empty;
        public object ResizeConfigs { get; set; } = string.Empty;
        public bool Enabled { get; set; }
        public string CreatedUtc { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public object UpdatedUtc { get; set; } = string.Empty;
        public object UpdatedBy { get; set; } = string.Empty;
        public string GetCameraType()
        {
            return Type switch
            {
                0 => "SECUS",
                1 => "SHANY",
                2 => "BOSCH",
                3 => "VANTECH",
                4 => "CNB",
                5 => "HIK",
                6 => "ENSTER",
                7 => "DAHUA",
                8 => "HANSE",
                9 => "TIANDY",
                10 => "DMAX",
                11 => "VIVANTEK",
                12 => "HANET",
                13 => "CUSTOM",
                14 => "PELCO",
                15 => "AVIGILON",
                16 => "ZKTECO",
                _ => Type.ToString(),
            };
        }
    }
}
