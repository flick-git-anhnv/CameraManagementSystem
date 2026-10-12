using Kztek.Object.MultyPlatform.LegacyiParkingv8.Enums.Parkings;
using Kztek.Object.MultyPlatform;

namespace iParkingv8.Object.Objects.Parkings;

public class AccessKey
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public EmAccessKeyType Type { get; set; } = EmAccessKeyType.CARD;
    public EmAccessKeyStatus Status { get; set; } = EmAccessKeyStatus.IN_USE;
    public string Note { get; set; } = string.Empty;
    public List<AccessKey> Metrics { get; set; } = [];
    public string CustomerId { get; set; } = string.Empty;

    public CustomerDto? Customer { get; set; }
    public string ExpiredUtc { get; set; } = string.Empty;
    public Collection? Collection { get; set; }
    public DateTime? ExpireTime
    {
        get
        {
            if (string.IsNullOrEmpty(ExpiredUtc))
            {
                return null;
            }

            try
            {
                return ExpiredUtc.Contains('T')
                    ? DateTime.ParseExact(ExpiredUtc[.."yyyy-MM-ddTHH:mm:ss".Length], "yyyy-MM-ddTHH:mm:ss", null).AddHours(7)
                    : DateTime.Parse(ExpiredUtc).AddHours(7);
            }
            catch
            {
                return DateTime.Now;
            }
        }
    }
    public AccessKey? GetVehicleInfo()
    {
        if (Type == EmAccessKeyType.VEHICLE)
        {
            return this;
        }

        return Metrics is { Count: > 0 } ? Metrics[0] : null;
    }

    public string GetStatusName() => Status switch
    {
        EmAccessKeyStatus.LOCKED => KztekLocalizationHelper.Translate("AccessKeyStatusLocked"),
        EmAccessKeyStatus.IN_USE => KztekLocalizationHelper.Translate("AccessKeyStatusInUse"),
        EmAccessKeyStatus.UN_USED => KztekLocalizationHelper.Translate("AccessKeyStatusUnused"),
        _ => string.Empty,
    };

    public string GetTypeName() => Type switch
    {
        EmAccessKeyType.VEHICLE => KztekLocalizationHelper.Translate("AccessKeyTypeVehicle"),
        EmAccessKeyType.CARD => KztekLocalizationHelper.Translate("AccessKeyTypeCard"),
        EmAccessKeyType.QRCODE => KztekLocalizationHelper.Translate("AccessKeyTypeQrCode"),
        EmAccessKeyType.FINGER_PRINT => KztekLocalizationHelper.Translate("AccessKeyTypeFingerPrint"),
        EmAccessKeyType.FACE_ID => KztekLocalizationHelper.Translate("AccessKeyTypeFaceId"),
        _ => Type.ToString(),
    };
}
