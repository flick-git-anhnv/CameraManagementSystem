using Kztek.Object;
using Kztek.Object.MultyPlatform.LegacyiParkingv8.Enums.Payments;

namespace iParkingv8.Object.Objects.Payments
{
    public class PaymentResult
    {
        public string TopicName { get; set; } = string.Empty;
        public string Id { get; set; } = string.Empty;
        public EmOrderStatus Status { get; set; } = EmOrderStatus.FAILED;
        public OrderMethod Method { get; set; } = OrderMethod.CASH;
    }
}
