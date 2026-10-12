namespace iParkingv8.Object.Objects.Payments
{
    public class VoucherApply
    {
        public string voucherTypeName { get; set; } = string.Empty;
        public string EventOutId { get; set; } = string.Empty;
        public string VoucherId { get; set; } = string.Empty;
        public long Amount { get; set; } = 0;
        public string CreatedBy { get; set; } = string.Empty;
        public VoucherDetail? Voucher { get; set; }
    }
}
