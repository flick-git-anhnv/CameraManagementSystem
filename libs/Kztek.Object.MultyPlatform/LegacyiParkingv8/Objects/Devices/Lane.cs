using Kztek.Object.MultyPlatform.LegacyiParkingv8.Enums;

namespace iParkingv8.Object.Objects.Devices
{
    public class Lane
    {
        public string Id { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string ComputerId { get; set; } = string.Empty;
        public int Type { get; set; }
        public string ReverseLaneId { get; set; } = string.Empty;
        public bool Loop { get; set; }
        public bool DisplayLed { get; set; }
        public bool Enabled { get; set; }
        public List<CameraInLane> Cameras { get; set; } = [];
        public List<ControllerInLane> ControlUnits { get; set; } = [];
        public EmAutoOpenBarrier auto_open_barrier { get; set; }
        public bool IsContainReader(string controllerId, int readerIndex, out ControllerInLane? controllerInLane)
        {
            controllerInLane = null;
            if (ControlUnits == null || ControlUnits.Count == 0) return false;

            foreach (ControllerInLane controlUnit in ControlUnits)
            {
                if (controlUnit.Id.Equals(controllerId, StringComparison.CurrentCultureIgnoreCase) &&
                    controlUnit.Readers.Contains(readerIndex))
                {
                    controllerInLane = controlUnit;
                    return true;
                }
            }
            return false;
        }
        public bool IsContainInput(string controllerId, int inputIndex)
        {
            if (ControlUnits == null || ControlUnits.Count == 0) return false;
            foreach (var controlUnit in ControlUnits)
            {
                if (controlUnit.Id.Equals(controllerId, StringComparison.CurrentCultureIgnoreCase) &&
                    controlUnit.Inputs.Contains(inputIndex))
                {
                    return true;
                }
            }
            return false;
        }
        public bool IsContainAlarm(string controllerId, int alarmIndex)
        {
            if (ControlUnits == null || ControlUnits.Count == 0) return false;
            foreach (var controlUnit in ControlUnits)
            {
                if (controlUnit.Id.Equals(controllerId, StringComparison.CurrentCultureIgnoreCase) &&
                    controlUnit.Alarms.Contains(alarmIndex))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
