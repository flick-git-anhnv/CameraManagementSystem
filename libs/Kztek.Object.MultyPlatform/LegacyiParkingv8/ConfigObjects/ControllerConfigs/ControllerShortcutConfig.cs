namespace iParkingv8.Object.ConfigObjects.ControllerConfigs
{
    public class ControllerShortcutConfig
    {
        public string ControllerId { get; set; } = string.Empty;
        public Dictionary<int, int> KeySetByRelays { get; set; } = new Dictionary<int, int>();
    }
}
