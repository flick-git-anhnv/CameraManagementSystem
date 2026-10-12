namespace Kztek.Object.MultyPlatform.LegacyiParkingv8.Objects.Kiosk
{
    public class BaseKioskResult : BaseDialogResult
    {
        public EmKioskUserType kioskUserType;
        public bool DisableCashForCurrentSession { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public enum EmKioskUserType
    {
        Customer,
        System
    }
}
