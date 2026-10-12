namespace Kztek.Object.MultyPlatform.LegacyiParkingv8.Enums.Sounds;

/// <summary>
/// Extension methods cho <see cref="EmSystemSoundType"/>.
/// Cung cấp tên hiển thị tiếng Việt dùng trong UI (tab Controller LaneSettings).
/// Tương đương WinForms <c>UcControllerBarrieConfig.GetSoundTypeString()</c>.
/// </summary>
public static class EmSystemSoundTypeExtensions
{
    private static readonly Dictionary<EmSystemSoundType, string> DisplayNames = new()
    {
        { EmSystemSoundType.ACCESS_KEY_EXPIRED,                                          "Phương tiện hết hạn" },
        { EmSystemSoundType.ACCESS_KEY_LOCKED,                                           "Thẻ bị khóa" },
        { EmSystemSoundType.ACCESS_KEY_MONTH_NO_VEHICLE,                                 "Thẻ chưa đăng ký phương tiện" },
        { EmSystemSoundType.MAT_KET_NOI_DEN_MAY_CHU,                                    "Mất kết nối đến máy chủ" },
        { EmSystemSoundType.GAP_LOI_TRONG_QUA_TRINH_XU_LY,                             "Gặp lỗi trong quá trình xử lý" },
        { EmSystemSoundType.VUI_LONG_QUET_THE_THANG_HOAC_NHAN_NUT_LAY_THE,              "Vui lòng quẹt thẻ tháng / nhấn lấy thẻ" },
        { EmSystemSoundType.VUI_LONG_QUET_THE_THANG_HOAC_TRA_THE,                       "Vui lòng quẹt thẻ tháng / trả thẻ" },
        { EmSystemSoundType.DINH_DANH_KHONG_CO_TRONG_HE_THONG,                         "Định danh không có trong hệ thống" },
        { EmSystemSoundType.SAI_QUYEN_TRUY_CAP,                                         "Sai quyền truy cập" },
        { EmSystemSoundType.WAIT_CONFIRM_OPEN_BARRIE,                                    "Chờ xác nhận mở barrier" },
        { EmSystemSoundType.KHONG_NHAN_DIEN_DUOC_BIEN_SO,                               "Không nhận diện được biển số" },
        { EmSystemSoundType.BIEN_SO_VAO_RA_KHONG_KHOP_VUI_LONG_CHO_XAC_NHAN_BARRIE,    "Biển số vào/ra không khớp" },
        { EmSystemSoundType.BIEN_SO_DANG_KY_KHONG_KHOP_VUI_LONG_CHO_XAC_NHAN_BARRIE,  "Biển số đăng ký không khớp" },
        { EmSystemSoundType.NOT_CONFIRM_OPEN_BARRIE,                                     "Không xác nhận mở barrier" },
        { EmSystemSoundType.XE_DA_VAO_BAI,                                               "Xe đã vào bãi" },
        { EmSystemSoundType.XIN_MOI_QUA,                                                 "Xin mời qua" },
        { EmSystemSoundType.HEN_GAP_LAI,                                                 "Hẹn gặp lại" },
        { EmSystemSoundType.XIN_MOI_LAY_THE,                                             "Xin mời lấy thẻ" },
        { EmSystemSoundType.XE_CHUA_VAO_BAI,                                             "Xe chưa vào bãi" },
        { EmSystemSoundType.INVALID_READER_DAILY,                                        "Thẻ ngày không hợp lệ" },
        { EmSystemSoundType.INVALID_READER_MONTHLY,                                      "Thẻ tháng không hợp lệ" },
        { EmSystemSoundType.BLACK_LIST,                                                   "Danh sách đen" },
        { EmSystemSoundType.VEHICLE_LOCKED,                                               "Phương tiện bị khóa" },
        { EmSystemSoundType.VUI_LONG_THANH_TOAN_PHI_GUI_XE,                             "Vui lòng thanh toán phí gửi xe" },
        { EmSystemSoundType.VUI_LONG_CHON_HINH_THUC_THANH_TOAN,                         "Vui lòng chọn hình thức thanh toán" },
        { EmSystemSoundType.CASH_WARNING,                                                 "Cảnh báo tiền mặt" },
        { EmSystemSoundType.RUN_OUT_OF_CARD,                                             "Hết thẻ" },
    };

    /// <summary>
    /// Trả về tên hiển thị tiếng Việt của <paramref name="soundType"/>.
    /// Nếu không có mapping (enum mới thêm mà chưa cập nhật dict) — fallback về <c>ToString()</c>.
    /// </summary>
    public static string ToDisplayName(this EmSystemSoundType soundType) =>
        DisplayNames.TryGetValue(soundType, out var name) ? name : soundType.ToString();
}
