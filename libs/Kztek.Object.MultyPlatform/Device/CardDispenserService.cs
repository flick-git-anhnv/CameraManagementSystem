using System.Collections.Generic;

namespace Kztek.Object
{
    /// <summary>
    /// Cấu hình ánh xạ tín hiệu I/O cho thiết bị phát thẻ tự động (Card Dispenser).
    /// Mỗi chức năng của dispenser được map với một chỉ số input/output cụ thể trên BĐK.
    /// </summary>
    public class CardDispenserService
    {
        /// <summary>Chỉ số nút bấm trên BĐK điều khiển dispenser.</summary>
        public int ButtonIndex { get; set; }

        /// <summary>Giá trị tín hiệu input khi thẻ đã được người dùng rút ra khỏi khe nhận thẻ.</summary>
        public string CardbeTaken { get; set; }

        /// <summary>Giá trị tín hiệu khi thẻ bị trả về khay chứa (bị từ chối hoặc hết giờ).</summary>
        public string CardRevertedInTrayValue { get; set; }

        /// <summary>Chỉ số input trên BĐK tương ứng với tín hiệu thẻ trả về khay.</summary>
        public string CardRevertedInTrayIndex { get; set; }

        /// <summary>Giá trị tín hiệu của nút bấm bất thường (sự cố, cần xử lý thủ công).</summary>
        public string ButtonAbnormalValue { get; set; }

        /// <summary>Chỉ số input trên BĐK của nút bấm bất thường.</summary>
        public string ButtonAbnormalIndex { get; set; }
    }

    /// <summary>
    /// Trạng thái thay đổi của thiết bị phát thẻ tự động — dùng trong sự kiện kết nối.
    /// </summary>
    public class CardDispenserOnChange
    {
        /// <summary><c>true</c> nếu dispenser đang trong quá trình xử lý (phát thẻ, thu thẻ).</summary>
        public bool StatusProcessing { get; set; } = true;

        /// <summary>Danh sách trạng thái của từng khay/ngăn dispenser.</summary>
        public List<StateCardDispenser> ListStatusDispenser { get; set; } = new List<StateCardDispenser>();

        /// <summary>Trạng thái tổng hợp các đầu vào digital của dispenser.</summary>
        public ArrayInputDispenser ArrayInputDispenser { get; set; }

        /// <summary>Chuỗi mô tả lỗi nếu có sự cố với dispenser.</summary>
        public string ErrorString { get; set; } = string.Empty;
    }

    /// <summary>Trạng thái của một ngăn chứa thẻ trong thiết bị phát thẻ.</summary>
    public class StateCardDispenser
    {
        /// <summary>Chỉ số của ngăn dispenser (bắt đầu từ 1).</summary>
        public int DispenserIndex { get; set; } = 1;

        /// <summary><c>true</c> nếu ngăn hết thẻ.</summary>
        public bool IsCardEmptyDispenser { get; set; } = false;

        /// <summary><c>true</c> nếu ngăn còn ít thẻ (cần bổ sung sớm).</summary>
        public bool IsLessCardDispenser { get; set; } = false;

        /// <summary><c>true</c> nếu ngăn có lỗi (kẹt thẻ, thiết bị hỏng).</summary>
        public bool IsCardErrorDispenser { get; set; } = false;
    }

    /// <summary>Mảng tín hiệu đầu vào digital của thiết bị phát thẻ.</summary>
    public class ArrayInputDispenser
    {
        /// <summary>Trạng thái báo động (0 = không báo động).</summary>
        public int Alarm { get; set; } = 0;

        /// <summary>Tín hiệu đầu vào digital DIn1.</summary>
        public bool DIn1 { get; set; } = false;

        /// <summary>Tín hiệu đầu vào digital DIn2.</summary>
        public bool DIn2 { get; set; } = false;

        /// <summary>Tín hiệu đầu vào digital DIn3.</summary>
        public bool DIn3 { get; set; } = false;

        /// <summary>Tín hiệu đầu vào digital DIn4.</summary>
        public bool DIn4 { get; set; } = false;
    }

}
