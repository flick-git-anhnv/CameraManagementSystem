namespace CMS.Core.Enums;

public enum AlarmEventType
{
    MotionDetected,
    TripwireCrossed,
    IntrusionDetected,
    BlacklistPersonDetected,
    BlacklistVehicleDetected,
    DeviceOffline,
    DeviceTampering,

    // Aliases
    LineCrossing = TripwireCrossed,
    Intrusion = IntrusionDetected,
    FaceBlacklist = BlacklistPersonDetected,
    PlateBlacklist = BlacklistVehicleDetected,
    CameraOffline = DeviceOffline
}

public enum AlarmLevel
{
    Info,
    Warning,
    Critical
}
