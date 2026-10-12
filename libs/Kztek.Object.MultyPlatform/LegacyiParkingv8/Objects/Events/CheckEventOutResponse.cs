using iParkingv8.Object.Objects.Bases;
using Kztek.Object.MultyPlatform.LegacyiParkingv8.Enums.Bases;

namespace iParkingv8.Object.Objects.Events
{
    public class CheckEventOutResponse
    {
        public EmAlarmCode AlarmCode { get; set; }
        public bool IsValidEvent { get; set; }
        public bool IsNeedConfirm { get; set; }
        public ExitData? EventData { get; set; }

        /// <summary>
        /// Thông tin lỗi, để hiển thị lên giao diện cho bảo vệ xem
        /// </summary>
        public string ErrorMessage { get; set; } = "";
        public BaseErrorData? ErrorData { get; set; } = null;

        public static CheckEventOutResponse CreateDefault()
        {
            return new CheckEventOutResponse()
            {
                IsNeedConfirm = false,
                IsValidEvent = false,
                EventData = null,
                ErrorMessage = string.Empty,
                ErrorData = null,
                AlarmCode = EmAlarmCode.NONE,
            };
        }
    }

}
