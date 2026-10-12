using System;
using static Kztek.Object.Events;

namespace Kztek.Object
{
    /// <summary>
    /// Quản lý các hành động của một thiết bị kiểm soát ra vào đơn lẻ.
    /// Mỗi implementation tương ứng với một loại phần cứng cụ thể (AOPU, ZKTeco, Dahua, v.v.).
    /// </summary>
    public interface IControllerControl
    {
        /// <summary>Sự kiện khi trạng thái kết nối với thiết bị này thay đổi.</summary>
        public event ConnectStatusChangeEventHandler? onConnectStatusChangeEvent;

        /// <summary>Sự kiện khi tải thông tin nhân viên xuống thiết bị hoàn tất.</summary>
        public event OnDownloadEmployee? onDownloadEmployeeEvent;

        /// <summary>Sự kiện khi xóa nhân viên khỏi thiết bị hoàn tất.</summary>
        public event OnDeleteEmployee? onDeleteEmployeeEvent;

        /// <summary>Sự kiện khi thiết bị phát sinh sự kiện mới (quẹt thẻ, nhận dạng, v.v.).</summary>
        public event OnEventComeHandler? onEventComeEvent;

        /// <summary>Sự kiện khi thiết bị báo lỗi.</summary>
        public event OnErrorEventHandler? onErrorEventHandler;

        /// <summary>Sự kiện khi trạng thái đầu vào (input) của thiết bị thay đổi (vòng từ, nút bấm, v.v.).</summary>
        public event OnInputEventHandler? OnInputEvent;

        /// <summary>Thông tin cấu hình của thiết bị này (địa chỉ IP, port, loại thiết bị, v.v.).</summary>
        Controller controller { get; set; }

        /// <summary>
        /// Khởi tạo control với thông tin thiết bị được cung cấp.
        /// </summary>
        /// <param name="controller">Thông tin thiết bị cần khởi tạo.</param>
        void Init(Controller controller);

        /// <summary>Kết nối tới thiết bị. Trả về <c>true</c> nếu kết nối thành công.</summary>
        bool Connect();

        /// <summary>Ngắt kết nối thiết bị. Trả về <c>true</c> nếu ngắt thành công.</summary>
        bool Disconnect();

        /// <summary>Bắt đầu vòng lặp polling lấy sự kiện từ thiết bị.</summary>
        void PollingStart();

        /// <summary>Dừng vòng lặp polling.</summary>
        void PollingStop();

        /// <summary>
        /// Đồng bộ thời gian hệ thống xuống thiết bị.
        /// </summary>
        /// <param name="time">Thời gian cần đồng bộ.</param>
        void SynchronousTime(DateTime time);

        /// <summary>Lấy toàn bộ sự kiện mới nhất từ thiết bị (không giới hạn khoảng thời gian).</summary>
        void GetEvent();

        /// <summary>
        /// Lấy sự kiện từ thiết bị trong khoảng thời gian chỉ định.
        /// </summary>
        /// <param name="startTime">Thời điểm bắt đầu truy vấn.</param>
        /// <param name="endTime">Thời điểm kết thúc truy vấn.</param>
        void GetEvent(DateTime startTime, DateTime endTime);

        /// <summary>
        /// Tải thông tin nhân viên xuống thiết bị với chế độ đăng ký mặc định.
        /// </summary>
        /// <param name="employee">Thông tin nhân viên cần tải.</param>
        /// <returns>Kết quả tải xuống.</returns>
        EmployeeResult DownloadUser(Staff employee);

        /// <summary>
        /// Tải thông tin nhân viên xuống thiết bị với chế độ đăng ký nhận dạng chỉ định.
        /// </summary>
        /// <param name="employee">Thông tin nhân viên cần tải.</param>
        /// <param name="mode">Chế độ nhận dạng (thẻ, vân tay, khuôn mặt, hoặc kết hợp).</param>
        /// <returns>Kết quả tải xuống.</returns>
        EmployeeResult DownloadUser(Staff employee, RegisterMode.EmRegisterMode mode);

        /// <summary>
        /// Xóa thông tin nhân viên khỏi thiết bị.
        /// </summary>
        /// <param name="employee">Thông tin nhân viên cần xóa.</param>
        /// <returns>Kết quả xóa.</returns>
        EmployeeResult DeleteUser(Staff employee);

        /// <summary>
        /// Kích hoạt relay (mở barrier/cửa) trên thiết bị.
        /// </summary>
        /// <param name="relay">Chỉ số relay cần kích hoạt (bắt đầu từ 0).</param>
        /// <returns><c>true</c> nếu thành công.</returns>
        bool SetRelay(int relay);
    }
}
