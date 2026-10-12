using CMS.Core.Enums;
using CMS.Core.Interfaces;
using CMS.Drivers.Dahua;
using CMS.Drivers.GenericRtsp;
using CMS.Drivers.Hikvision;
using System.Collections.Concurrent;

namespace CMS.Drivers;

public class CameraDriverFactory
{
    private static readonly ConcurrentDictionary<CameraBrand, ICameraDriver> _drivers = new();

    static CameraDriverFactory()
    {
        // Đăng ký các driver mặc định
        RegisterDriver(new HikvisionDriver());
        RegisterDriver(new DahuaDriver());
        RegisterDriver(new GenericRtspDriver(CameraBrand.ONVIF));
        RegisterDriver(new GenericRtspDriver(CameraBrand.Uniview));
        RegisterDriver(new GenericRtspDriver(CameraBrand.Hanwha));
        RegisterDriver(new GenericRtspDriver(CameraBrand.Tiandy));
        RegisterDriver(new GenericRtspDriver(CameraBrand.Bosch));
        RegisterDriver(new GenericRtspDriver(CameraBrand.Pelco));
        RegisterDriver(new GenericRtspDriver(CameraBrand.Milesight));
        RegisterDriver(new GenericRtspDriver(CameraBrand.ZKTeco));
        RegisterDriver(new GenericRtspDriver(CameraBrand.CustomRTSP));
    }

    public static void RegisterDriver(ICameraDriver driver)
    {
        _drivers[driver.Brand] = driver;
    }

    public static ICameraDriver GetDriver(CameraBrand brand)
    {
        if (_drivers.TryGetValue(brand, out var driver))
        {
            return driver;
        }

        // Fallback sang ONVIF Generic
        return _drivers[CameraBrand.ONVIF];
    }

    public static IEnumerable<ICameraDriver> GetAllDrivers() => _drivers.Values;
}
