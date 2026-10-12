namespace iParkingv8.Object.Objects.Devices
{
    public class Gate
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool Enabled { get; set; }
        public bool Deleted { get; set; }
    }
}
