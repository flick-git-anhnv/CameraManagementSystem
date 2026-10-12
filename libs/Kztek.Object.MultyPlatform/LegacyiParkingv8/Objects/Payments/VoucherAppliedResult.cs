namespace iParkingv8.Object.Objects.Payments
{
    public class VoucherAppliedResult
    {
        public string id { get; set; }
        public string targetId { get; set; }
        public int transactionTargetType { get; set; }
        public string voucherId { get; set; }
        public string VoucherTypeName { get; set; }
        public int Amount { get; set; }
        public string createdBy { get; set; }
        public DateTime createdUtc { get; set; }
    }
}
