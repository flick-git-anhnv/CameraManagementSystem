namespace Kztek.Object.MultyPlatform.LegacyiParkingv8.Enums.Sounds
{
    public enum EmSystemSoundType
    {
        ACCESS_KEY_EXPIRED,//--OK Phương tiện của bạn đã hết hạn sử dụng
        ACCESS_KEY_LOCKED,//--OK Thẻ của bạn đã bị khóa
        ACCESS_KEY_MONTH_NO_VEHICLE,//Thẻ của bạn chưa được đăng ký phương tiện

        MAT_KET_NOI_DEN_MAY_CHU,
        GAP_LOI_TRONG_QUA_TRINH_XU_LY,

        //--Kiểm tra thông tin thẻ
        VUI_LONG_QUET_THE_THANG_HOAC_NHAN_NUT_LAY_THE,//--OK
        VUI_LONG_QUET_THE_THANG_HOAC_TRA_THE,
        DINH_DANH_KHONG_CO_TRONG_HE_THONG,//--OK Thẻ không có trong hệ thống
        SAI_QUYEN_TRUY_CAP,//--OK Phương tiện của bạn không được phép sử dụng ở làn xe này

        WAIT_CONFIRM_OPEN_BARRIE,//--OK Vui lòng chờ nhân viên trực xác nhận mở barrie
        KHONG_NHAN_DIEN_DUOC_BIEN_SO,//--OK Không nhận diện được biển số vui lòng chờ nhân viên trực xác nhạn mở barrie
        BIEN_SO_VAO_RA_KHONG_KHOP_VUI_LONG_CHO_XAC_NHAN_BARRIE,//--OK
        BIEN_SO_DANG_KY_KHONG_KHOP_VUI_LONG_CHO_XAC_NHAN_BARRIE,//--OK

        NOT_CONFIRM_OPEN_BARRIE,//--OK
        XE_DA_VAO_BAI,//-- Xe đã vào bãi
        XIN_MOI_QUA,//--OK Xin mời qua
        HEN_GAP_LAI,//--OK hẹn gặp lại

        XIN_MOI_LAY_THE,
        XE_CHUA_VAO_BAI,

        INVALID_READER_DAILY,
        INVALID_READER_MONTHLY,

        BLACK_LIST,
        VEHICLE_LOCKED,
        VUI_LONG_THANH_TOAN_PHI_GUI_XE,
        VUI_LONG_CHON_HINH_THUC_THANH_TOAN,

        CASH_WARNING,
        RUN_OUT_OF_CARD,
    }
}
