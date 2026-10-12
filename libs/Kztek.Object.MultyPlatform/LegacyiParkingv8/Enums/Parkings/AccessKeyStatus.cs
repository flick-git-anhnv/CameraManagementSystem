using Kztek.Object.MultyPlatform;

namespace Kztek.Object.MultyPlatform.LegacyiParkingv8.Enums.Parkings;

public enum EmAccessKeyStatus
{
    LOCKED,
    IN_USE,
    UN_USED,
}

public static class AccessKeyStatus
{
    public static string ToDisplayString(this EmAccessKeyStatus accessKeyStatus) => accessKeyStatus switch
    {
        EmAccessKeyStatus.LOCKED => KztekLocalizationHelper.Translate("AccessKeyStatusLocked"),
        EmAccessKeyStatus.IN_USE => KztekLocalizationHelper.Translate("AccessKeyStatusInUse"),
        EmAccessKeyStatus.UN_USED => KztekLocalizationHelper.Translate("AccessKeyStatusUnused"),
        _ => string.Empty,
    };
}
