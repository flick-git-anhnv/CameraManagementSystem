namespace iParkingv8.Object.Objects.Reports
{
    public class SearchRevenueReportResponse
    {
        public long TotalCount { get; set; }
        public long TotalAmount { get; set; }
        public long TotalDiscount { get; set; }
        public long TotalEntryAmount { get; set; }
        public long TotalExitAmount { get; set; }
        public List<RevenueReportItem> Data { get; set; } = [];
    }
    public class RevenueReportItem
    {
        public string Identifier { get; set; } = string.Empty;
        /// <summary>
        /// Tổng Số lượng xe ra theo Identifier
        /// </summary>
        public long Count { get; set; } = 0;
        /// <summary>
        /// Tổng giảm giá theo Identifier
        /// </summary>
        public long Discount { get; set; } = 0;
        /// <summary>
        /// Tổng phí theo Identifier
        /// </summary>
        public long Amount { get; set; } = 0;
        /// <summary>
        /// Tổng trả trước theo Identifier
        /// </summary>
        public long EntryAmount { get; set; } = 0;
        //public long EntryMethod { get; set; } = 0;
        ///// <summary>
        ///// Tổng thực thu theo Identifier
        ///// </summary>
        //public long ExitAmount { get; set; } = 0;
        //public long ExitMethod { get; set; } = 0;
    }
}
