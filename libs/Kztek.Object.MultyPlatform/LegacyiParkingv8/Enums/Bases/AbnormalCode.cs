using iParkingv8.Object.Objects.Bases;

namespace Kztek.Object.MultyPlatform.LegacyiParkingv8.Enums.Bases
{
    public enum EmAlarmCode
    {
        // Mã thẻ không có trong hệ thông
        ACCESS_KEY_NOT_FOUND,
        // Mã thẻ không hợp lệ
        ACCESS_KEY_INVALID,
        //Định danh bị khóa
        ACCESS_KEY_LOCKED,
        //Định danh hết hạn
        ACCESS_KEY_EXPIRED,
        //Sai quyền truy cập
        ACCESS_KEY_COLLECTION_NOT_ALLOWED_FOR_LANE,
        // Biển số khác với biển đăng ký
        PLATE_NUMBER_DIFFERENCE_SYSTEM,
        // Biển số vào ra không khớp
        PLATE_NUMBER_INVALID,
        // Biển số đen
        PLATE_NUMBER_BLACKLISTED,
        // Mở barrie bằng bàn phím
        BARRIER_OPENED_BY_KEYBOARD,
        // Mở barrie bằng nút nhấn
        BARRIER_OPENED_BY_BUTTON,
        // Ghi vé
        ENTRY_CREATED_MANUALLY,
        // Xe đã vào bãi
        ENTRY_DUPLICATED,
        // Xe chưa vào bãi
        ENTRY_NOT_FOUND,
        // Xe đè vòng loop
        VEHICLE_ON_LOOP,
        BLACKLIST,
        // Xóa sự kiện xe đang trong bãi
        ENTRY_DELETED,
        UNRETURNED_CARD,
        ACCESS_KEY_COLLECTION_CHANGED,
        PEDESTRIAN_PASSAGE,
        VEHICLE_PASSAGE,
        // Lỗi hệ thống
        SYSTEM_ERROR,
        NONE,
    }


    public static class AlarmCodeExtension
    {
        public static string ToString(this EmAlarmCode code)
        {
            switch (code)
            {
                case EmAlarmCode.ACCESS_KEY_NOT_FOUND:
                    return "Mã thẻ không có trong hệ thống";
                case EmAlarmCode.ACCESS_KEY_INVALID:
                    return "Thẻ / Xe không hợp lệ";
                case EmAlarmCode.ACCESS_KEY_LOCKED:
                    return "Định danh bị khóa";
                case EmAlarmCode.ACCESS_KEY_EXPIRED:
                    return "Phương tiện hết hạn sử dụng";
                case EmAlarmCode.ACCESS_KEY_COLLECTION_NOT_ALLOWED_FOR_LANE:
                    return "Nhóm định danh không được phép sử dụng với làn";
                case EmAlarmCode.PLATE_NUMBER_DIFFERENCE_SYSTEM:
                    return "Biển số khác với biển đăng ký";
                case EmAlarmCode.PLATE_NUMBER_INVALID:
                    return "Biển số vào ra không khớp";
                case EmAlarmCode.PLATE_NUMBER_BLACKLISTED:
                    return "Biển số đen";
                case EmAlarmCode.BARRIER_OPENED_BY_KEYBOARD:
                    return "Mở barrie bằng bàn phím";
                case EmAlarmCode.BARRIER_OPENED_BY_BUTTON:
                    return "Mở barrie bằng nút nhấn";
                case EmAlarmCode.ENTRY_CREATED_MANUALLY:
                    return "Ghi vé";
                case EmAlarmCode.ENTRY_DUPLICATED:
                    return "Xe đã vào bãi";
                case EmAlarmCode.ENTRY_NOT_FOUND:
                    return "Xe chưa vào bãi";
                case EmAlarmCode.VEHICLE_ON_LOOP:
                    return "Xe đè vòng loop";
                case EmAlarmCode.ENTRY_DELETED:
                    return "Xóa sự kiện xe trong bãi";
                case EmAlarmCode.UNRETURNED_CARD:
                    return "Nợ thẻ";
                case EmAlarmCode.ACCESS_KEY_COLLECTION_CHANGED:
                        return "Đổi nhóm thẻ";
                default:
                    return "";
            }
        }

        public static string ToDisplayMessage(this BaseErrorData? errorData, string fallbackMessage = "")
        {
            if (errorData is null)
            {
                return fallbackMessage;
            }

            var baseMessage = errorData.ToString();
            if (errorData.Fields is { Count: > 0 })
            {
                var rawFieldMessage = $"{errorData.Fields[0].Name} - {errorData.Fields[0].ErrorCode}";
                if (!string.IsNullOrWhiteSpace(baseMessage) &&
                    !string.Equals(baseMessage, rawFieldMessage, StringComparison.Ordinal))
                {
                    return baseMessage;
                }
            }
            else if (!string.IsNullOrWhiteSpace(baseMessage))
            {
                return baseMessage;
            }

            if (errorData.Fields is { Count: > 0 })
            {
                string name = errorData.Fields[0].Name;
                string errorCode = errorData.Fields[0].ErrorCode;

                var translated = (errorData.Route, name, errorCode) switch
                {
                    ("discounts", "VoucherCode", "ERROR.ENTITY.VALIDATION.FIELD_DUPLICATED") => "Voucher đã được sử dụng",
                    ("discounts", "VoucherCode", "ERROR.ENTITY.VALIDATION.FIELD_NOT_FOUND") => "Không tìm thấy voucher",
                    ("discounts", "VoucherCode", "ERROR.ENTITY.VALIDATION.FIELD_NOT_ACTIVE") => "Voucher chưa được kích hoạt",
                    ("discounts", "FromDate", "ERROR.ENTITY.VALIDATION.FIELD_INVALID") => "Voucher chưa được kích hoạt",
                    ("discounts", "ToDate", "ERROR.ENTITY.VALIDATION.FIELD_INVALID") => "Voucher đã hết hạn",
                    ("discounts", "TimeOfDay", "ERROR.ENTITY.VALIDATION.FIELD_INVALID") => "Voucher không hợp lệ trong khung giờ hiện tại",
                    ("discounts", "AccessKeyCode", "ERROR.ENTITY.VALIDATION.FIELD_NOT_FOUND") => EmAlarmCode.ACCESS_KEY_NOT_FOUND.ToString(),
                    ("discounts", "AccessKeyCode", "ERROR.ENTITY.VALIDATION.FIELD_NOT_ACTIVE") => EmAlarmCode.ACCESS_KEY_LOCKED.ToString(),
                    ("discounts", "Collection", "ERROR.ENTITY.VALIDATION.FIELD_NOT_ACTIVE") => "Nhóm định danh đang bị khóa",
                    ("discounts", "collectionId", "ERROR.ENTITY.VALIDATION.FIELD_NOT_MATCH_WITH_ATTRIBUTES") => "Voucher không đúng loại áp dụng",
                    ("discounts", "Entry", "ERROR.ENTITY.VALIDATION.FIELD_NOT_FOUND") => EmAlarmCode.ENTRY_NOT_FOUND.ToString(),
                    ("discounts", "Entry", "ERROR.ENTITY.VALIDATION.FIELD_DUPLICATED") => "Voucher đã được sử dụng",
                    ("discounts", "Exit", "ERROR.ENTITY.VALIDATION.FIELD_DUPLICATED") => "Voucher đã được sử dụng",
                    (_, "Exit", "ERROR.ENTITY.VALIDATION.EVENT_IN_COOLDOWN") => "Sự kiện ra đang trong thời gian chờ xử lý",
                    (_, "Entry", "ERROR.ENTITY.VALIDATION.EVENT_IN_COOLDOWN") => "Sự kiện vào đang trong thời gian chờ xử lý",
                    _ => string.Empty,
                };

                if (!string.IsNullOrWhiteSpace(translated))
                {
                    return translated;
                }
            }

            var alarmMessage = errorData.GetAbnormalCode().ToString();
            return string.IsNullOrWhiteSpace(alarmMessage)
                ? fallbackMessage
                : alarmMessage;
        }
    }
}
