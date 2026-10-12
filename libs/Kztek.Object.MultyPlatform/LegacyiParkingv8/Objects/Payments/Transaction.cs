using Kztek.Object;
using Kztek.Object.MultyPlatform.LegacyiParkingv8.Enums.Payments;

namespace iParkingv8.Object.Objects.Payments
{
    public class Transaction
    {
        public string Id { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;

        public TargetType TargetType { get; set; } = TargetType.Exit;
        public string TargetId { get; set; } = string.Empty;

        public OrderMethod Method { get; set; }
        public EmOrderStatus Status { get; set; }
        public int Amount { get; set; }

        public string QrCode { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
