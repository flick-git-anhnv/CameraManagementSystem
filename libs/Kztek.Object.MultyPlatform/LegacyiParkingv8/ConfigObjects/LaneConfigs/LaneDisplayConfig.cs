namespace iParkingv8.Object.ConfigObjects.LaneConfigs
{
    /// <summary>
    /// Thông tin kích thước hiển thị trên giao diện
    /// </summary>
    public class LaneDisplayConfig
    {
        public string LaneId { get; set; } = string.Empty;
        public int DisplayIndex { get; set; } = 0;

        public int splitterMain { get; set; } = 224;
        public int splitterCamera_Pic { get; set; } = 224;
        public int splitterLpr_DGV { get; set; } = 224;

        public int splitterCamera_EventImageList { get; set; } = 224;
        public int splitterImageList_Result { get; set; } = 444;
        public int splitterResult_plate { get; set; } = 488;
        public int splitterPlate_EventInfo { get; set; } = 625;
        public int splitterEventInfo_Function { get; set; } = 956;

        public static LaneDisplayConfig CreateDefault(int height, string laneId, int displayIndex)
        {
            if (height > 1000)
            {
                return new LaneDisplayConfig()
                {
                    LaneId = laneId,
                    DisplayIndex = displayIndex,
                    splitterCamera_EventImageList = 224,
                    splitterImageList_Result = 212,
                    splitterResult_plate = 126,
                    splitterPlate_EventInfo = 130,
                    splitterEventInfo_Function = 32
                };
            }
            return new LaneDisplayConfig()
            {
                LaneId = laneId,
                DisplayIndex = displayIndex,
                splitterCamera_EventImageList = 224,
                splitterImageList_Result = 212,
                splitterResult_plate = 126,
                splitterPlate_EventInfo = 130,
                splitterEventInfo_Function = 32
            };
        }
    }
}
