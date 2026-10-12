namespace iParkingv8.Object.Objects.Devices
{
    public class ControllerInLane
    {
        public string Id { get; set; } = string.Empty;
        public List<int> Readers { get; set; } = [];
        public List<int> Inputs { get; set; } = [];
        public List<int> Barriers { get; set; } = [];
        public List<int> Alarms { get; set; } = [];
    }
}
