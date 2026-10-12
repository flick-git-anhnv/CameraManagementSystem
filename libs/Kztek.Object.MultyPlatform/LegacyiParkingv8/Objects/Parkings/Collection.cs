using iParkingv8.Object.Objects.Bases;
using Kztek.Object.MultyPlatform.LegacyiParkingv8.Enums.Parkings;
using Kztek.Object.MultyPlatform;

namespace iParkingv8.Object.Objects.Parkings;

public class Collection
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public EmVehicleType VehicleType { get; set; }
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

    public EmPlateNumberValidate GetPlateNumberValidationLevel()
    {
        try
        {
            string config = GetAttributeValue("plate_number.validation.level");
            return string.IsNullOrEmpty(config)
                ? EmPlateNumberValidate.APPROXIMATELY
                : (EmPlateNumberValidate)Enum.Parse(typeof(EmPlateNumberValidate), config, true);
        }
        catch
        {
            return EmPlateNumberValidate.APPROXIMATELY;
        }
    }

    public EmPlateNumberMethod GetCheckPlateNumberMethod()
    {
        try
        {
            string config = GetAttributeValue("plate_number.validation.method");
            return string.IsNullOrEmpty(config)
                ? EmPlateNumberMethod.BOTH
                : (EmPlateNumberMethod)Enum.Parse(typeof(EmPlateNumberMethod), config, true);
        }
        catch
        {
            return EmPlateNumberMethod.BOTH;
        }
    }

    public EmVehicleType GetVehicleType() => VehicleType;

    public EmAccessKeyGroupType GetAccessKeyGroupType()
    {
        try
        {
            string config = GetAttributeValue("type");
            if (string.IsNullOrEmpty(config))
            {
                return EmAccessKeyGroupType.UNKNOWN;
            }

            _ = Enum.TryParse(config, out EmAccessKeyGroupType result);
            return result;
        }
        catch
        {
            return EmAccessKeyGroupType.UNKNOWN;
        }
    }

    public string GetAccessKeyGroupTypeName() => GetAccessKeyGroupType() switch
    {
        EmAccessKeyGroupType.MONTHLY => KztekLocalizationHelper.Translate("AccessKeyGroupTypeMonthly"),
        EmAccessKeyGroupType.DAILY => KztekLocalizationHelper.Translate("AccessKeyGroupTypeDaily"),
        EmAccessKeyGroupType.VIP => KztekLocalizationHelper.Translate("AccessKeyGroupTypeVip"),
        _ => string.Empty,
    };

    public string GetVehicleTypeName() => GetVehicleType() switch
    {
        EmVehicleType.CAR => KztekLocalizationHelper.Translate("VehicleTypeCar"),
        EmVehicleType.MOTORBIKE => KztekLocalizationHelper.Translate("VehicleTypeMotorbike"),
        EmVehicleType.BIKE => KztekLocalizationHelper.Translate("VehicleTypeBike"),
        _ => string.Empty,
    };

    public bool GetEntryByLoop()
    {
        try { return Convert.ToBoolean(GetAttributeValue("entry_by_loop")); }
        catch { return false; }
    }

    public bool GetExitByLoop()
    {
        try { return Convert.ToBoolean(GetAttributeValue("exit_by_loop")); }
        catch { return false; }
    }

    public List<string>? GetActiveLanes()
    {
        try
        {
            string config = GetAttributeValue("lanes");
            if (string.IsNullOrEmpty(config))
            {
                return [];
            }

            return Newtonsoft.Json.JsonConvert.DeserializeObject<List<string>>(config);
        }
        catch
        {
            return [];
        }
    }
}
