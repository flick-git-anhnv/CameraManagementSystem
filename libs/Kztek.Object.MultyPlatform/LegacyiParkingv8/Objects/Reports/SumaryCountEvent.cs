namespace iParkingv8.Object.Objects.Reports
{
    public class SumaryCountEvent
    {
        /// <summary>
        /// Số lượng xe đang trong bãi
        /// </summary>
        public int CountAllEventIn { get; set; } = 0;
        /// <summary>
        /// Tổng số xe ra khỏi bãi trong ngày
        /// </summary>
        public int TotalVehicleOut { get; set; } = 0;
        /// <summary>
        /// Tổng số xe vào bãi trong ngày
        /// </summary>
        public int TotalVehicleIn { get; set; } = 0;
    }

}
