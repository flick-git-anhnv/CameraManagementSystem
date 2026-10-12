using iParkingv8.Object.Objects.Bases;
using Kztek.Object.MultyPlatform.LegacyiParkingv8.Enums.Bases;

namespace iParkingv8.Object.Objects.Events
{
    public class CheckEventResponse
    {
        public EmAlarmCode AlarmCode { get; set; } = EmAlarmCode.NONE;
        public bool IsValidEvent { get; set; }
        public bool IsNeedConfirm { get; set; }
        public EntryData? EventData { get; set; }

        /// <summary>
        /// Thông tin lỗi, để hiển thị lên giao diện cho bảo vệ xem
        /// </summary>
        public string ErrorMessage { get; set; } = "";
        public BaseErrorData? ErrorData { get; set; } = null;

        public static CheckEventResponse CreateDefault()
        {
            return new CheckEventResponse()
            {
                IsNeedConfirm = false,
                IsValidEvent = false,
                EventData = null,
                ErrorMessage = string.Empty,
                ErrorData = null,
                AlarmCode = EmAlarmCode.SYSTEM_ERROR
            };
        }
    }

}
