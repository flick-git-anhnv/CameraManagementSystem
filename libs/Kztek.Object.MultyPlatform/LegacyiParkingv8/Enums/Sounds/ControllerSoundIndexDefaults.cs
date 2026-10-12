namespace Kztek.Object.MultyPlatform.LegacyiParkingv8.Enums.Sounds
{
    /// <summary>
    /// Mã âm thanh mặc định gửi xuống bộ điều khiển khi controller chưa được cấu hình riêng
    /// (dùng cho SoundMode = PlaySoundFromController).
    /// </summary>
    public static class ControllerSoundIndexDefaults
    {
        private static readonly Dictionary<EmSystemSoundType, int> Map = new()
        {
            { EmSystemSoundType.ACCESS_KEY_EXPIRED, 5 },
            { EmSystemSoundType.ACCESS_KEY_LOCKED, 5 },
            { EmSystemSoundType.ACCESS_KEY_MONTH_NO_VEHICLE, 5 },
            { EmSystemSoundType.VUI_LONG_QUET_THE_THANG_HOAC_NHAN_NUT_LAY_THE, 1 },
            { EmSystemSoundType.VUI_LONG_QUET_THE_THANG_HOAC_TRA_THE, 1 },
            { EmSystemSoundType.DINH_DANH_KHONG_CO_TRONG_HE_THONG, 5 },
            { EmSystemSoundType.SAI_QUYEN_TRUY_CAP, 5 },
            { EmSystemSoundType.KHONG_NHAN_DIEN_DUOC_BIEN_SO, 3 },
            { EmSystemSoundType.BIEN_SO_VAO_RA_KHONG_KHOP_VUI_LONG_CHO_XAC_NHAN_BARRIE, 3 },
            { EmSystemSoundType.BIEN_SO_DANG_KY_KHONG_KHOP_VUI_LONG_CHO_XAC_NHAN_BARRIE, 3 },
            { EmSystemSoundType.XE_DA_VAO_BAI, 6 },
            { EmSystemSoundType.XIN_MOI_QUA, 2 },
            { EmSystemSoundType.HEN_GAP_LAI, 1 },
            { EmSystemSoundType.XIN_MOI_LAY_THE, 1 },
        };

        public static int Get(EmSystemSoundType soundType) => Map.TryGetValue(soundType, out int index) ? index : 0;
    }
}
