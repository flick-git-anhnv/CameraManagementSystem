using iParkingv8.Object.Objects.Bases;

namespace iParkingv8.Object.Objects.Payments
{
    public class Voucher
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public List<Attributes> Attributes { get; set; } = [];
        public List<string> Vouchers { get; set; } = [];
        public string GetAttributeValue(string code)
        {
            foreach (var item in Attributes)
            {
                if (item.Code.Equals(code, StringComparison.CurrentCultureIgnoreCase))
                {
                    return item.Value;
                }
            }
            return string.Empty;
        }
        public string GetCode()
        {
            if (Vouchers != null && Vouchers.Count > 0)
            {
                return Vouchers[0];
            }
            return "";
        }
    }

   
    public enum EmDiscountType
    {
        Percent,
        Cash,
        Time
    }
}
