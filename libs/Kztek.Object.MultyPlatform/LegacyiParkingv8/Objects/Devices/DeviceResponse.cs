using iParkingv6.Objects.Datas;
using iParkingv8.Object.Objects.Bases;
using Kztek.Object;
using Kztek.Object.MultyPlatform.LegacyiParkingv8.Enums;
using Kztek.Object.MultyPlatform.LegacyiParkingv8.Enums.Devices;

namespace iParkingv8.Object.Objects.Devices
{
    public class DeviceResponse
    {
        public int DurationInMillisecond { get; set; }
        public int TotalCount { get; set; }
        public int TotalPage { get; set; }
        public int PageSize { get; set; }
        public int PageIndex { get; set; }
        public List<DeviceData> Data { get; set; } = [];

        public Computer? GetComputerByIp(List<string> validIps)
        {
            if (Data == null) return null;
            if (Data.Count == 0) return null;
            var computerData = Data.Where(e => e.Type == EmDeviceType.COMPUTER).ToList();
            if (computerData.Count > 0)
            {
                foreach (var item in computerData)
                {
                    if (item.Attributes.Count == 0) continue;
                    foreach (var attribute in item.Attributes)
                    {
                        if (attribute == null)
                        {
                            continue;
                        }
                        if (attribute.Code.Equals("ipaddress", StringComparison.CurrentCultureIgnoreCase) &&
                            validIps.Contains(attribute.Value.ToUpper()))
                        {
                            return new Computer()
                            {
                                IpAddress = attribute.Value,
                                Id = item.Id,
                                Name = item.Name,
                                Code = item.Code,
                            };
                        }
                    }
                }
                return null;
            }
            else
            {
                return null;
            }
        }
        public List<Gate> GetGatesByComputer(Computer computer)
        {
            if (Data == null) return [];
            if (Data.Count == 0) return [];
            var _computer = Data.Where(e => e.Type == EmDeviceType.COMPUTER &&
                                        e.Id.Equals(computer.Id, StringComparison.CurrentCultureIgnoreCase)).FirstOrDefault();
            var gates = Data.Where(e => e.Type == EmDeviceType.GATE &&
                                        e.Id.Equals(_computer.Parent.Id, StringComparison.CurrentCultureIgnoreCase))
                                 .ToList();
            List<Gate> result = [];
            foreach (var gateConfig in gates)
            {
                Gate gate = new()
                {
                    Id = gateConfig.Id,
                    Name = gateConfig.Name,
                    Code = gateConfig.Code,
                };
                result.Add(gate);
            }
            return result;
        }
        public List<Lane> GetLanesByComputer(Computer computer)
        {
            if (Data == null) return [];
            if (Data.Count == 0) return [];
            var lanes = Data.Where(e => e.Type == EmDeviceType.LANE &&
                                        e.Parent.Id.Equals(computer.Id, StringComparison.CurrentCultureIgnoreCase))
                                 .ToList();
            List<Lane> result = [];
            foreach (var laneConfig in lanes)
            {
                if (!laneConfig.Enabled)
                {
                    continue;
                }
                Lane lane = new()
                {
                    Id = laneConfig.Id,
                    Name = laneConfig.Name,
                    Code = laneConfig.Code,
                    ComputerId = computer.Id,
                };
                string controlUnitConfig = laneConfig.GetAttributeValue("controlUnits");
                string cameraConfig = laneConfig.GetAttributeValue("cameras");
                string laneTypeConfig = laneConfig.GetAttributeValue("laneType");
                string autoOpenBarrieConfig = laneConfig.GetAttributeValue("autoOpenBarrier");
                string displayLedConfig = laneConfig.GetAttributeValue("displayLed");
                string isUseLoopConfig = laneConfig.GetAttributeValue("loop");
                lane.ReverseLaneId = laneConfig.GetAttributeValue("reverseLaneId");
                lane.auto_open_barrier = (EmAutoOpenBarrier)Enum.Parse(typeof(EmAutoOpenBarrier), laneConfig.GetAttributeValue("auto_open_barrier"), true);
                lane.ControlUnits = string.IsNullOrEmpty(controlUnitConfig) ?
                    [] :
                    Newtonsoft.Json.JsonConvert.DeserializeObject<List<ControllerInLane>>(controlUnitConfig) ?? [];
                lane.Cameras = string.IsNullOrEmpty(cameraConfig) ?
                                    [] :
                                    Newtonsoft.Json.JsonConvert.DeserializeObject<List<CameraInLane>>(cameraConfig) ?? [];


                lane.Type = int.Parse(laneTypeConfig);
                lane.DisplayLed = !string.IsNullOrEmpty(displayLedConfig) && Convert.ToBoolean(displayLedConfig);
                lane.Loop = !string.IsNullOrEmpty(isUseLoopConfig) && Convert.ToBoolean(isUseLoopConfig);
                result.Add(lane);
            }
            return result;
        }
        public List<Lane> GetAllLanes()
        {
            if (Data == null) return [];
            if (Data.Count == 0) return [];
            var lanes = Data.Where(e => e.Type == EmDeviceType.LANE)
                                 .ToList();
            List<Lane> result = [];
            foreach (var laneConfig in lanes)
            {
                Lane lane = new()
                {
                    Id = laneConfig.Id,
                    Name = laneConfig.Name,
                    Code = laneConfig.Code,
                };
                string controlUnitConfig = laneConfig.GetAttributeValue("controlUnits");
                string cameraConfig = laneConfig.GetAttributeValue("cameras");
                string laneTypeConfig = laneConfig.GetAttributeValue("laneType");
                string autoOpenBarrieConfig = laneConfig.GetAttributeValue("autoOpenBarrier");
                string displayLedConfig = laneConfig.GetAttributeValue("displayLed");
                string isUseLoopConfig = laneConfig.GetAttributeValue("loop");
                lane.ReverseLaneId = laneConfig.GetAttributeValue("reverseLaneId");
                lane.auto_open_barrier = (EmAutoOpenBarrier)Enum.Parse(typeof(EmAutoOpenBarrier), laneConfig.GetAttributeValue("auto_open_barrier"), true);
                lane.ControlUnits = string.IsNullOrEmpty(controlUnitConfig) ?
                    [] :
                    Newtonsoft.Json.JsonConvert.DeserializeObject<List<ControllerInLane>>(controlUnitConfig) ?? [];
                lane.Cameras = string.IsNullOrEmpty(cameraConfig) ?
                                    [] :
                                    Newtonsoft.Json.JsonConvert.DeserializeObject<List<CameraInLane>>(cameraConfig) ?? [];


                lane.Type = int.Parse(laneTypeConfig);
                lane.DisplayLed = !string.IsNullOrEmpty(displayLedConfig) && Convert.ToBoolean(displayLedConfig);
                lane.Loop = !string.IsNullOrEmpty(isUseLoopConfig) && Convert.ToBoolean(isUseLoopConfig);
                result.Add(lane);
            }
            return result;
        }

        public IEnumerable<Camera> GetCamerasByComputer(Computer computer)
        {
            if (Data == null || Data.Count == 0)
                yield break;

            var cameras = Data.Where(e =>
                e.Type == EmDeviceType.CAMERA &&
                e.Parent.Id.Equals(computer.Id, StringComparison.CurrentCultureIgnoreCase));

            foreach (var cameraConfig in cameras)
            {
                yield return new Camera
                {
                    Id = cameraConfig.Id,
                    Name = cameraConfig.Name,
                    ComputerId = computer.Id,
                    HttpPort = cameraConfig.GetAttributeValue("httpPort"),
                    Channel = ParseInt(cameraConfig.GetAttributeValue("channel")),
                    Type = ParseInt(cameraConfig.GetAttributeValue("typeCode")),
                    FrameRate = ParseInt(cameraConfig.GetAttributeValue("frameRate")),
                    Username = cameraConfig.GetAttributeValue("username"),
                    Password = cameraConfig.GetAttributeValue("password"),
                    IpAddress = cameraConfig.GetAttributeValue("ipAddress"),
                    RtspPort = cameraConfig.GetAttributeValue("rtspPort"),
                    Resolution = cameraConfig.GetAttributeValue("resolution")
                };
            }
        }

        private static int ParseInt(string? value)
        {
            return int.TryParse(value, out var result) ? result : 0;
        }

        public List<Bdk> GetBDKsByComputer(Computer computer)
        {
            if (Data == null) return [];
            if (Data.Count == 0) return [];
            var controllers = Data.Where(e => e.Type == EmDeviceType.CONTROL_UNIT &&
                                        e.Parent.Id.Equals(computer.Id, StringComparison.CurrentCultureIgnoreCase))
                                 .ToList();
            List<Bdk> result = [];
            foreach (var controllerConfig in controllers)
            {
                if (!controllerConfig.Enabled)
                {
                    continue;
                }
                Bdk controller = new()
                {
                    Id = controllerConfig.Id,
                    Name = controllerConfig.Name,
                    ComputerId = computer.Id,
                    Code = controllerConfig.Code,
                };
                string typeCode = controllerConfig.GetAttributeValue("typeCode");
                string communicationType = controllerConfig.GetAttributeValue("communicationType");
                string baudrate = controllerConfig.GetAttributeValue("baudrate");
                string comport = controllerConfig.GetAttributeValue("comport");

                controller.Type = string.IsNullOrEmpty(typeCode) ? 0 : int.Parse(typeCode);
                controller.CommunicationType = string.IsNullOrEmpty(communicationType) ? 0 : int.Parse(communicationType);
                controller.Baudrate = baudrate;
                controller.Comport = comport;
                result.Add(controller);
            }
            return result;
        }
        public List<Led> GetLedsByConputer(Computer computer)
        {
            if (Data == null) return [];
            if (Data.Count == 0) return [];
            var leds = Data.Where(e => e.Type == EmDeviceType.LED &&
                                        e.Parent.Id.Equals(computer.Id, StringComparison.CurrentCultureIgnoreCase))
                                 .ToList();
            List<Led> result = [];
            foreach (var ledConfig in leds)
            {
                if (!ledConfig.Enabled)
                {
                    continue;
                }
                Led led = new()
                {
                    id = ledConfig.Id,
                    name = ledConfig.Name,
                    computerId = computer.Id,
                };
                string typeCode = ledConfig.GetAttributeValue("typeCode");
                string row = ledConfig.GetAttributeValue("row");
                string baudrate = ledConfig.GetAttributeValue("baudrate");
                string comport = ledConfig.GetAttributeValue("comport");
                string address = ledConfig.GetAttributeValue("address");

                led.type = string.IsNullOrEmpty(typeCode) ? 0 : int.Parse(typeCode);
                led.address = string.IsNullOrEmpty(address) ? "1" : address;
                led.baudrate = string.IsNullOrEmpty(baudrate) ? 100 : int.Parse(baudrate);
                led.row = string.IsNullOrEmpty(row) ? 100 : int.Parse(row);
                led.comport = comport;

                result.Add(led);
            }
            return result;
        }
    }

    public class DeviceData : BaseDevice
    {
        public BaseDevice Parent { get; set; } = new BaseDevice();
        public List<Attributes> Attributes { get; set; } = [];
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
    }

    public class BaseDevice
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public EmDeviceType Type { get; set; }
        public bool Enabled { get; set; }
    }
}
