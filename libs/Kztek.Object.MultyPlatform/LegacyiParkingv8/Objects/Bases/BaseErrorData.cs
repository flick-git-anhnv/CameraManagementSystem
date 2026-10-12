using Kztek.Object.MultyPlatform.LegacyiParkingv8.Enums.Bases;

namespace iParkingv8.Object.Objects.Bases;

public class BaseErrorData
{
    private const string NotFoundError = "ERROR.ENTITY.NOT_FOUND";
    private const string ValidateNotFoundError = "ERROR.ENTITY.VALIDATION.FIELD_NOT_FOUND";
    private const string ValidateDuplicatedError = "ERROR.ENTITY.VALIDATION.FIELD_DUPLICATED";
    private const string ValidateInvalidError = "ERROR.ENTITY.VALIDATION.FIELD_INVALID";
    private const string ValidateNotActiveError = "ERROR.ENTITY.VALIDATION.FIELD_NOT_ACTIVE";

    public string Route { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string ErrorCode { get; set; } = string.Empty;
    public string DetailCode { get; set; } = string.Empty;
    public List<ErrorDescription> Fields { get; set; } = [];
    public Dictionary<string, object>? Payload = [];

    public override string ToString()
    {
        if (Fields == null || Fields.Count == 0)
        {
            return Route.Equals("access-keys", StringComparison.CurrentCultureIgnoreCase) &&
                   ErrorCode == NotFoundError
                ? "Thẻ chưa được đăng ký."
                : string.Empty;
        }

        string name = Fields[0].Name;
        string errorCode = Fields[0].ErrorCode;

        return (Route, name, errorCode) switch
        {
            ("discounts", "VoucherCode", ValidateDuplicatedError) => "Voucher đã được sử dụng!",
            ("discounts", "VoucherCode", ValidateNotFoundError) => "Voucher chưa được đăng ký trong hệ thống!",
            ("discounts", "VoucherCode", ValidateNotActiveError) => "Voucher chưa được kích hoạt!",
            ("discounts", "FromDate", ValidateInvalidError) => "Voucher chưa được kích hoạt!",
            ("discounts", "ToDate", ValidateInvalidError) => "Voucher đã hết hạn sử dụng!",
            ("discounts", "TimeOfDay", ValidateInvalidError) => "VOUCHER không được phép sử dụng trong khung giờ này!",
            ("discounts", "AccessKeyCode", ValidateNotFoundError) => "Thẻ chưa được đăng ký.",
            ("discounts", "AccessKeyCode", ValidateNotActiveError) => "Thẻ của bạn chưa được kích hoạt!",
            ("discounts", "Collection", ValidateNotActiveError) => "Nhóm định danh chưa được kích hoạt!",
            ("discounts", "collectionId", "ERROR.ENTITY.VALIDATION.FIELD_NOT_MATCH_WITH_ATTRIBUTES") => "Sai loại voucher!",
            ("discounts", "Entry", ValidateNotFoundError) => "Xe chưa vào bãi.",
            ("discounts", "Entry", ValidateDuplicatedError) => "Voucher đã được sử dụng!",
            ("discounts", "Exit", ValidateDuplicatedError) => "Voucher đã được sử dụng!",
            (_, "AccessKeyId", ValidateNotFoundError) when Route.Equals("entries", StringComparison.CurrentCultureIgnoreCase) => "Thẻ chưa được đăng ký.",
            (_, "AccessKeyId", ValidateNotFoundError) when Route.Equals("exits", StringComparison.CurrentCultureIgnoreCase) => "Xe chưa vào bãi.",
            (_, "AccessKeyId", ValidateDuplicatedError) when Route.Equals("entries", StringComparison.CurrentCultureIgnoreCase) => "Xe đã vào bãi",
            (_, "AccessKeyId", ValidateNotActiveError) => "Thẻ của bạn chưa được kích hoạt!",
            (_, "Metrics", ValidateNotActiveError) => "Phương tiện hết hạn sử dụng",
            (_, "DeviceId", ValidateNotFoundError) => "Làn không có trong hệ thống",
            (_, "DeviceId", ValidateInvalidError) => "Sai quyền truy cập!",
            (_, "PlateNumber", "ERROR.ENTITY.VALIDATION.FIELD_NOT_MATCH_WITH_SYSTEM") => "BIỂN SỐ KHÔNG KHỚP VỚI BIỂN ĐĂNG KÝ!",
            (_, "PlateNumber", "ERROR.ENTITY.VALIDATION.FIELD_NOT_MATCH_WITH_ENTRY") => "BIỂN SỐ CHECK OUT KHÔNG KHỚP BIỂN SỐ LÚC CHECK IN.",
            (_, "PlateNumber", "ERROR.ENTITY.VALIDATION.BLACKLISTED") => "BIỂN SỐ ĐÃ BỊ CHẶN.",
            (_, "Exit", "ERROR.ENTITY.VALIDATION.EVENT_IN_COOLDOWN") => "TRONG THỜI GIAN CHỜ RA",
            (_, "Entry", "ERROR.ENTITY.VALIDATION.EVENT_IN_COOLDOWN") => "TRONG THỜI GIAN CHỜ VÀO",
            _ => $"{name} - {errorCode}",
        };
    }

    public EmAlarmCode GetAbnormalCode()
    {
        if (Fields == null || Fields.Count == 0)
        {
            return Route.Equals("access-keys", StringComparison.CurrentCultureIgnoreCase) &&
                   ErrorCode == NotFoundError
                ? EmAlarmCode.ACCESS_KEY_NOT_FOUND
                : EmAlarmCode.SYSTEM_ERROR;
        }

        string name = Fields[0].Name;
        string errorCode = Fields[0].ErrorCode;

        if (name.Equals("AccessKeyId", StringComparison.CurrentCultureIgnoreCase))
        {
            if (errorCode == ValidateNotFoundError)
            {
                if (Route.Equals("entries", StringComparison.CurrentCultureIgnoreCase))
                {
                    return EmAlarmCode.ACCESS_KEY_NOT_FOUND;
                }

                if (Route.Equals("exits", StringComparison.CurrentCultureIgnoreCase))
                {
                    return EmAlarmCode.ENTRY_NOT_FOUND;
                }
            }

            if (errorCode == ValidateDuplicatedError &&
                Route.Equals("entries", StringComparison.CurrentCultureIgnoreCase))
            {
                return EmAlarmCode.ENTRY_DUPLICATED;
            }

            if (errorCode == ValidateNotActiveError)
            {
                return EmAlarmCode.ACCESS_KEY_LOCKED;
            }
        }
        else if (name.Equals("Metrics", StringComparison.CurrentCultureIgnoreCase) &&
                 errorCode == ValidateNotActiveError)
        {
            return EmAlarmCode.ACCESS_KEY_EXPIRED;
        }
        else if (name.Equals("DeviceId", StringComparison.CurrentCultureIgnoreCase))
        {
            if (errorCode is ValidateNotFoundError or ValidateInvalidError)
            {
                return EmAlarmCode.ACCESS_KEY_COLLECTION_NOT_ALLOWED_FOR_LANE;
            }
        }
        else if (name.Equals("PlateNumber", StringComparison.CurrentCultureIgnoreCase))
        {
            return errorCode switch
            {
                "ERROR.ENTITY.VALIDATION.FIELD_NOT_MATCH_WITH_SYSTEM" => EmAlarmCode.PLATE_NUMBER_DIFFERENCE_SYSTEM,
                "ERROR.ENTITY.VALIDATION.FIELD_NOT_MATCH_WITH_ENTRY" => EmAlarmCode.PLATE_NUMBER_INVALID,
                "ERROR.ENTITY.VALIDATION.BLACKLISTED" => EmAlarmCode.PLATE_NUMBER_BLACKLISTED,
                _ => EmAlarmCode.SYSTEM_ERROR,
            };
        }

        return EmAlarmCode.SYSTEM_ERROR;
    }
}

public class ErrorDescription
{
    public string Name { get; set; } = string.Empty;
    public string ErrorCode { get; set; } = string.Empty;
}
