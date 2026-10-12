namespace iParkingv8.Object.Objects.Payments
{
    public class EPassVehicleInfo
    {
        public string PlateNumber { get; set; } = string.Empty;
        public int Type { get; set; }
        public decimal Weight { get; set; }
        public int Seat { get; set; }
        public string TransId { get; set; } = string.Empty;
    }
}
