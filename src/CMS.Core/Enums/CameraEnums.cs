namespace CMS.Core.Enums;

public enum DeviceStatus
{
    Unknown,
    Online,
    Offline,
    Connecting,
    Error
}

public enum CameraBrand
{
    ONVIF,
    Hikvision,
    Dahua,
    Uniview,
    Hanwha,
    Tiandy,
    Bosch,
    Pelco,
    Milesight,
    ZKTeco,
    CustomRTSP
}

public enum StreamType
{
    MainStream = 0,
    SubStream = 1
}

public enum PtzCommand
{
    Stop,
    Up,
    Down,
    Left,
    Right,
    UpLeft,
    UpRight,
    DownLeft,
    DownRight,
    ZoomIn,
    ZoomOut,
    FocusNear,
    FocusFar
}

public enum RecordingMode
{
    None,
    Continuous247,
    Schedule,
    MotionTriggered,
    AiTriggered
}

[Flags]
public enum AiCapability
{
    None = 0,
    VehicleClassification = 1 << 0,
    PlateRecognition = 1 << 1,
    VehicleColor = 1 << 2,
    VehicleTracking = 1 << 3,
    FaceDetection = 1 << 4,
    FaceRecognition = 1 << 5,
    SinglePersonTracking = 1 << 6,
    MultiCameraReId = 1 << 7
}
