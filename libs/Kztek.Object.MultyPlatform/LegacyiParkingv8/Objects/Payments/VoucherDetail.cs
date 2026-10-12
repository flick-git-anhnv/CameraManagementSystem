namespace iParkingv8.Object.Objects.Payments
{
    public class VoucherDetail
    {
        public string Id { get; set; } = string.Empty;
        public int UsedQuantity { get; set; } = 0;
        public int Quantity { get; set; } = 0;
        public string VoucherTypeName { get; set; } = string.Empty;
        public int Type { get; set; } = 0;
        public bool Enabled { get; set; } = false;
        public int Value { get; set; }
        public DateTime FromUtc { get; set; }
        public DateTime ToUtc { get; set; }
        public DateTime CreatedUtc { get; set; }
        public DateTime UpdatedUtc { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public VoucherErrorData? ErrorData { get; set; }
    }
}
