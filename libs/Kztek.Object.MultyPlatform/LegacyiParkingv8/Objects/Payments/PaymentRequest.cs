using Kztek.Object;

namespace iParkingv8.Object.Objects.Payments
{
    public class PaymentRequest
    {
        public TargetType TargetType { get; set; }
        public string TargetId { get; set; }
        public OrderMethod Method { get; set; } = OrderMethod.CASH;
        public long Amount { get; set; }
        public string PosId { get; set; } = string.Empty;
        public string Epc { get; set; } = string.Empty;
        public string Description { get; set; }
        public string PlateNumber { get; set; }
        public string? CreatedBy { get; set; } = null;
        public string? TransactionId { get; set; }
        public PaymentRequest(string targetId, OrderMethod orderMethod, long amount, string description,
                             TargetType targetType = TargetType.Entry, string posId = "", string epc = "", string plateNumber = "")
        {
            TargetId = targetId;
            Method = orderMethod;
            Amount = amount;
            TargetType = targetType;
            PosId = posId;
            Epc = epc;
            Description = description.Replace("-", " ") + " " + posId;
            PlateNumber = plateNumber;
        }
    }
}
