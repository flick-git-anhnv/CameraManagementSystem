namespace iParkingv8.Object.Objects.Reports
{
    public class ShiftHandOverReport
    {
        public Dictionary<string, ShiftHandOverDetail> Report { get; set; } = [];
    }
    public class ShiftHandOverDetail
    {
        public long Ton { get; set; }
        public long Vao { get; set; }
        public long Ra { get; set; }
        public long Amount { get; set; }
        public long RealFee { get; set; }
        public long Discount { get; set; }
    }
}