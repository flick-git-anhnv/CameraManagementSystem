using iParkingv8.Object.Objects.Events;

namespace iParkingv8.Object.Objects.Invoices
{
    public class InvoiceData
    {
        public string id { get; set; }
        public string exitId { get; set; }
        public string code { get; set; }
        public string lookupCode { get; set; }
        public int provider { get; set; }
        public int status { get; set; }
        public float amount { get; set; }
        public float taxRate { get; set; }
        public float taxAmount { get; set; }
        public float totalAmount { get; set; }
        public string createdBy { get; set; }
        public DateTime createdUtc { get; set; }
        public DateTime updatedUtc { get; set; }

        public ExitData Exit { get; set; }
    }

}
