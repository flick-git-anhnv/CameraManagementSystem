namespace iParkingv8.Object.Objects.Devices
{
    public class Computer
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string GateId { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public object Description { get; set; } = string.Empty;
        public bool Enabled { get; set; } = true;
        public string CreatedUtc { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public string UpdatedUtc { get; set; } = string.Empty;
        public string UpdatedBy { get; set; } = string.Empty;
        public Gate? Gate { get; set; }
    }
}
