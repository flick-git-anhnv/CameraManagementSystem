namespace iParkingv8.Object.ConfigObjects.LaneConfigs
{
    public class KioskConfig
    {
        public string LaneId { get; set; } = string.Empty;
        public string LaneName { get; set; } = string.Empty;
        public int DailyReader { get; set; } = 1;
        public int MonthReader { get; set; } = 2;
        public int TimeoutTakeCard { get; set; } = 30;
    }
}
