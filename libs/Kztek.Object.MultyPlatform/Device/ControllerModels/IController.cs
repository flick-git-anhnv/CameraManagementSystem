using System;
using System.Collections.Generic;
using static Kztek.Object.Events;

namespace Kztek.Object
{

    /// <summary>
    /// Quản lý hành động đối với nhóm các thiết bị kiểm soát ra vào (controller).
    /// Một <see cref="IController"/> có thể quản lý nhiều <see cref="IControllerControl"/> cùng lúc.
    /// </summary>
    public interface IController
    {
        /// <summary>Sự kiện khi trạng thái kết nối với thiết bị thay đổi (kết nối / mất kết nối).</summary>
        event ConnectStatusChangeEventHandler? onConnectStatusChangeEvent;

        /// <summary>Sự kiện khi quá trình tải thông tin nhân viên xuống thiết bị hoàn tất.</summary>
        event OnDownloadEmployee? onDownloadEmployeeEvent;

        /// <summary>Sự kiện khi quá trình xóa nhân viên khỏi thiết bị hoàn tất.</summary>
        event OnDeleteEmployee? onDeleteEmployeeEvent;

        /// <summary>Sự kiện khi thiết bị phát sinh sự kiện mới (quẹt thẻ, điểm danh, v.v.).</summary>
        event OnEventComeHandler? onEventComeEvent;

        /// <summary>Sự kiện khi thiết bị báo lỗi.</summary>
        event OnErrorEventHandler? onErrorEventHandler;

        /// <summary>
        /// Khởi tạo danh sách control tương ứng với từng controller được cung cấp.
        /// </summary>
        /// <param name="controllers">Danh sách thông tin các thiết bị cần khởi tạo.</param>
        /// <returns>Danh sách <see cref="IControllerControl"/> đã được khởi tạo.</returns>
        List<IControllerControl> Init(List<Controller> controllers);

        /// <summary>Kết nối tới toàn bộ thiết bị trong nhóm. Trả về <c>true</c> nếu tất cả kết nối thành công.</summary>
        bool Connect();

        /// <summary>Ngắt kết nối toàn bộ thiết bị trong nhóm. Trả về <c>true</c> nếu ngắt thành công.</summary>
        bool Disconnect();

        /// <summary>Bắt đầu vòng lặp polling để lấy sự kiện từ thiết bị.</summary>
        void PollingStart();

        /// <summary>Dừng vòng lặp polling.</summary>
        void PollingStop();

        /// <summary>
        /// Đồng bộ thời gian hệ thống xuống toàn bộ thiết bị trong nhóm.
        /// </summary>
        /// <param name="time">Thời gian cần đồng bộ.</param>
        void SynchronousTime(DateTime time);

        /// <summary>
        /// Tải thông tin nhân viên xuống danh sách thiết bị chỉ định (dùng chế độ đăng ký mặc định của thiết bị).
        /// </summary>
        /// <param name="employee">Thông tin nhân viên cần tải xuống.</param>
        /// <param name="controllers">Danh sách thiết bị cần tải.</param>
        void DownloadUser(Staff employee, List<Controller> controllers);

        /// <summary>
        /// Tải thông tin nhân viên xuống danh sách thiết bị với chế độ đăng ký nhận dạng chỉ định.
        /// </summary>
        /// <param name="employee">Thông tin nhân viên cần tải xuống.</param>
        /// <param name="mode">Chế độ nhận dạng (thẻ, vân tay, khuôn mặt).</param>
        /// <param name="controllers">Danh sách thiết bị cần tải.</param>
        void DownloadUser(Staff employee, RegisterMode.EmRegisterMode mode, List<Controller> controllers);

        /// <summary>
        /// Xóa thông tin nhân viên khỏi danh sách thiết bị chỉ định.
        /// </summary>
        /// <param name="employee">Thông tin nhân viên cần xóa.</param>
        /// <param name="controllers">Danh sách thiết bị cần xóa.</param>
        void DeleteUser(Staff employee, List<Controller> controllers);

        /// <summary>
        /// Xóa một controller khỏi nhóm quản lý và giải phóng tài nguyên liên quan.
        /// </summary>
        /// <param name="controller">Thiết bị cần xóa.</param>
        void DeleteController(Controller controller);
    }
}
