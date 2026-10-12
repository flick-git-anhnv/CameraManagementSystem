using Kztek.Object.Entity.Device.ControllerModels;
using System;

namespace Kztek.Object
{
    /// <summary>
    /// Tập hợp các delegate, EventArgs và enum dùng cho hệ thống sự kiện của thiết bị kiểm soát ra vào.
    /// </summary>
    public class Events
    {
        /// <summary>Delegate xử lý sự kiện lỗi từ thiết bị.</summary>
        public delegate void OnErrorEventHandler(Controller controller, ErrorEventArgs e);

        /// <summary>Thông tin lỗi phát sinh từ thiết bị.</summary>
        public class ErrorEventArgs : EventArgs
        {
            /// <summary>Mô tả lỗi.</summary>
            public string ErrorString { get; set; } = string.Empty;
            /// <summary>Tên hàm phát sinh lỗi.</summary>
            public string ErrorFunc { get; set; } = string.Empty;
            /// <summary>Dòng code phát sinh lỗi (nếu có).</summary>
            public string ErrorLine { get; set; } = string.Empty;
            /// <summary>Lệnh/command đang thực thi khi lỗi xảy ra.</summary>
            public string CMD { get; set; } = string.Empty;
        }

        #region Connect
        /// <summary>Delegate xử lý sự kiện thay đổi trạng thái kết nối thiết bị.</summary>
        public delegate void ConnectStatusChangeEventHandler(Controller controller, ConnectStatusCHangeEventArgs e);

        /// <summary>Thông tin thay đổi trạng thái kết nối.</summary>
        public class ConnectStatusCHangeEventArgs : EventArgs
        {
            /// <summary>Trạng thái kết nối hiện tại (<c>true</c> = đang kết nối).</summary>
            public bool CurrentStatus { get; set; }
            /// <summary>Lý do thay đổi trạng thái (ví dụ: "Timeout", "Connection refused").</summary>
            public string reason { get; set; } = string.Empty;
        }
        #endregion

        #region Employee
        /// <summary>Delegate xử lý sự kiện tải nhân viên xuống thiết bị hoàn tất.</summary>
        public delegate void OnDownloadEmployee(Controller controller, EmployeeEventArgs e);

        /// <summary>Delegate xử lý sự kiện xóa nhân viên khỏi thiết bị hoàn tất.</summary>
        public delegate void OnDeleteEmployee(Controller controller, EmployeeEventArgs e);

        /// <summary>Thông tin sự kiện thao tác nhân viên (tải xuống / xóa).</summary>
        public class EmployeeEventArgs : EventArgs
        {
            /// <summary>Loại hành động thực hiện (Add hoặc Delete).</summary>
            public EmEmployeeAction Action { get; set; } = EmEmployeeAction.Add;
            /// <summary>Thời điểm thực hiện hành động.</summary>
            public DateTime EventTime { get; set; }
            /// <summary>Thông tin nhân viên đầu vào (input).</summary>
            public Staff Input { get; set; } = new Staff();
            /// <summary>Kết quả trả về từ thiết bị sau khi thực hiện hành động.</summary>
            public EmployeeResult Output { get; set; } = new EmployeeResult();
        }

        /// <summary>Kết quả thực hiện thao tác nhân viên trên thiết bị.</summary>
        public class EmployeeResult
        {
            /// <summary>ID nhân viên do hệ thống cung cấp (dùng để đối chiếu với phản hồi từ thiết bị).</summary>
            public string ProvidedId { get; set; } = string.Empty;
            /// <summary>Mã kết quả trả về từ thiết bị.</summary>
            public EmEmployeeResult result { get; set; }

            /// <summary>
            /// Chuyển mã kết quả sang chuỗi mô tả tiếng Việt.
            /// </summary>
            /// <param name="result">Mã kết quả cần chuyển đổi.</param>
            /// <returns>Chuỗi mô tả (ví dụ: "Thành công", "Mất kết nối tới thiết bị").</returns>
            public static string toString(EmEmployeeResult result)
            {
                switch (result)
                {
                    case EmEmployeeResult.OK:
                        return "Thành công";
                    case EmEmployeeResult.DISCONNECT_WITH_DEVICE:
                        return "Mất kết nối tới thiết bị";
                    case EmEmployeeResult.INVALID_ACCESS_LEVEL:
                        return "Không có quyền sử dụng";
                    default:
                        return "Thất Bại";
                }
            }

        }
        /// <summary>Mã kết quả trả về sau khi thực hiện thao tác nhân viên trên thiết bị.</summary>
        public enum EmEmployeeResult
        {
            /// <summary>Thao tác thành công.</summary>
            OK,
            /// <summary>Mất kết nối với thiết bị trong lúc thực hiện.</summary>
            DISCONNECT_WITH_DEVICE,
            /// <summary>Nhân viên không có quyền truy cập cửa/làn được chỉ định.</summary>
            INVALID_ACCESS_LEVEL,
            /// <summary>Chỉ số cửa/làn không hợp lệ.</summary>
            INVALID_DOOR,
            /// <summary>Thiết bị không hỗ trợ thao tác này.</summary>
            NOT_SUPPORT,
            /// <summary>Nhân viên chưa được đăng ký trên thiết bị (khi xóa một user không tồn tại).</summary>
            NOT_REGISTERED,
            /// <summary>Lỗi không xác định từ thiết bị.</summary>
            ERROR,
            /// <summary>Thao tác thất bại (generic false).</summary>
            FALSE,
        }

        /// <summary>Loại hành động thực hiện với nhân viên trên thiết bị.</summary>
        public enum EmEmployeeAction
        {
            /// <summary>Thêm hoặc cập nhật nhân viên trên thiết bị.</summary>
            Add,
            /// <summary>Xóa nhân viên khỏi thiết bị.</summary>
            Delete
        }
        #endregion

        #region Event
        /// <summary>Delegate xử lý sự kiện nhận dạng từ thiết bị (quẹt thẻ, khuôn mặt, vân tay).</summary>
        public delegate void OnEventComeHandler(Controller controller, ControllerEventArgs e);

        /// <summary>Delegate xử lý sự kiện thay đổi trạng thái đầu vào (vòng từ, nút bấm).</summary>
        public delegate void OnInputEventHandler(Controller controller, InputEventArgs e);

        /// <summary>Thông tin sự kiện thay đổi trạng thái đầu vào trên thiết bị.</summary>
        public class InputEventArgs
        {
            /// <summary>ID thiết bị phát sinh sự kiện.</summary>
            public string ControllerId { get; set; } = string.Empty;
            /// <summary>Tên thiết bị phát sinh sự kiện.</summary>
            public string ControllerName { get; set; } = string.Empty;
            /// <summary>Thời điểm sự kiện theo đồng hồ của thiết bị.</summary>
            public DateTime ControllerEventTime { get; set; }
            /// <summary>Thời điểm sự kiện theo giờ máy tính cục bộ.</summary>
            public DateTime LocalTime { get; set; }
            /// <summary>Chỉ số của đầu vào thay đổi (bắt đầu từ 0).</summary>
            public int InputIndex { get; set; }
            /// <summary>Trạng thái mới của đầu vào (Open/Close/Disconnected/Error).</summary>
            public InputState InputState { get; set; } = InputState.Open;
        }

        /// <summary>
        /// Thông tin sự kiện nhận dạng từ thiết bị (quẹt thẻ, khuôn mặt, vân tay).
        /// Bao gồm đầy đủ dữ liệu để hệ thống xử lý nghiệp vụ (vào/ra, chấm công).
        /// </summary>
        public class ControllerEventArgs
        {
            /// <summary>ID người dùng do hệ thống cung cấp (từ database), dùng để đối chiếu.</summary>
            public string ProvidedId { get; set; } = string.Empty;
            /// <summary>ID thiết bị phát sinh sự kiện.</summary>
            public string ControllerId { get; set; } = string.Empty;
            /// <summary>Tên thiết bị phát sinh sự kiện.</summary>
            public string ControllerName { get; set; } = string.Empty;
            /// <summary>Phương thức nhận dạng: 1 = thẻ, 2 = QR, 3 = vân tay, 4 = khuôn mặt.</summary>
            public int IdentityMethod { get; set; }
            /// <summary>Thời điểm sự kiện theo đồng hồ của thiết bị.</summary>
            public DateTime ControllerEventTime { get; set; }
            /// <summary>Thời điểm sự kiện theo giờ máy tính cục bộ.</summary>
            public DateTime LocalTime { get; set; }
            /// <summary><c>true</c> nếu sự kiện hợp lệ và được phép xử lý nghiệp vụ.</summary>
            public bool IsValidEvent { get; set; }
            /// <summary>Chỉ số đầu vào (cửa/reader) phát sinh sự kiện.</summary>
            public int InputIndex { get; set; }
            /// <summary>Dữ liệu thẻ từ (có giá trị khi nhận dạng bằng thẻ).</summary>
            public ControllerCardData? CardData { get; set; }
            /// <summary>Dữ liệu khuôn mặt (có giá trị khi nhận dạng bằng khuôn mặt).</summary>
            public ControllerFaceData? FaceData { get; set; }
            /// <summary>Dữ liệu vân tay (có giá trị khi nhận dạng bằng vân tay).</summary>
            public ControllerFingerData? FingerData { get; set; }
        }

        /// <summary>Dữ liệu thẻ từ được đọc từ thiết bị.</summary>
        public class ControllerCardData
        {
            /// <summary>Số thẻ đã được chuyển đổi theo định dạng cấu hình của đầu đọc.</summary>
            public string CardNumber { get; set; } = string.Empty;
        }

        /// <summary>Dữ liệu vân tay được nhận dạng từ thiết bị.</summary>
        public class ControllerFingerData
        {
            /// <summary>ID vân tay được nhận dạng trên thiết bị.</summary>
            public string FingerId { get; set; } = string.Empty;
        }

        /// <summary>Dữ liệu khuôn mặt được nhận dạng từ thiết bị.</summary>
        public class ControllerFaceData
        {
            /// <summary>ID khuôn mặt được nhận dạng trên thiết bị.</summary>
            public string FaceId { get; set; } = string.Empty;
        }
        #endregion
    }
}
