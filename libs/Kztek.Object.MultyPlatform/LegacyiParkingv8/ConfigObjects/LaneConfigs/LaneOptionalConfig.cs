namespace iParkingv8.Object.ConfigObjects.LaneConfigs
{
    public class LaneOptionalConfig
    {
        public bool IsRegisterTurnVehicle { get; set; } = false;
        public string TurnCollectionId { get; set; } = string.Empty;
        public bool IsUseLoopImageForCardEvent { get; set; } = false;
        public List<AccessKeyCollectionShortcut> AccesskeyCollectionShortcuts { get; set; } = new List<AccessKeyCollectionShortcut>();

        #region Hộp thoại cảnh báo
        public bool IsUseAlarmDialog { get; set; } = true;
        public int AutoRejectDialogTime { get; set; } = 0;
        public bool AutoRejectDialogResult { get; set; } = false;
        #endregion

        #region QR tĩnh
        public string StaticQRComport { get; set; } = string.Empty;
        public int StaticQRBaudrate { get; set; } = 115200;
        public int StaticQRType { get; set; } = 0;

        public string AccountNumber { get; set; } = string.Empty;
        public string BankName { get; set; } = string.Empty;
        #endregion
    }
    public class AccessKeyCollectionShortcut
    {
        public string CollectionId { get; set; } = string.Empty;
        public int? Keys { get; set; }
        public bool IsUse { get; set; } = false;
        public bool IsDefault { get; set; }
    }
}
