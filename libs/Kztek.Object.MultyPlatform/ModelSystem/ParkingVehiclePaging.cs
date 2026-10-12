namespace ParkingHelper.ModelSystem
{
    public class ParkingVehiclePaging
    {
        private string keyword = "";
        private string fromDate = "";
        private string toDate = "";
        private string cardgroupIds = "";
        private string customergroupIds = "";
        private string laneIds = "";
        private string userIds = "";
        private string plateNumber = "";

        public string Keyword { get => keyword; set => keyword = value; }
        public string FromDate { get => fromDate; set => fromDate = value; }
        public string ToDate { get => toDate; set => toDate = value; }
        public string CardgroupIds { get => cardgroupIds; set => cardgroupIds = value; }
        public string CustomergroupIds { get => customergroupIds; set => customergroupIds = value; }
        public string LaneIds { get => laneIds; set => laneIds = value; }
        public string UserIds { get => userIds; set => userIds = value; }
        public string PlateNumber { get => plateNumber; set => plateNumber = value; }
    }
}
