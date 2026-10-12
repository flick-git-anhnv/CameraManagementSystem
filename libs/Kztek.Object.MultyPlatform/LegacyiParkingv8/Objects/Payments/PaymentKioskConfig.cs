namespace iParkingv8.Object.Objects.Payments
{
    public class PaymentKioskConfig
    {
        #region Controller COnfig
        public string CashComport { get; set; } = "COM1";
        public int CashBaudrate { get; set; } = 115200;
        public bool IsUseCash { get; set; }
        public int CashDeviceType { get; set; } = 0;

        #endregion

        #region QR
        public string QRPosId { get; set; } = string.Empty;
        public EmQRProvider QRProvider { get; set; }
        public bool IsUseQR { get; set; }
        #endregion

        #region VISA
        public string VisaId { get; set; } = string.Empty;
        public bool IsUseVisa { get; set; }
        #endregion

        #region VOUCHER
        public string VoucherComport { get; set; } = string.Empty;
        public int VoucherBaudrate { get; set; } = 9600;
        public int VoucherDeviceType { get; set; } = 0;
        public bool IsUseVoucher { get; set; }
        #endregion

        #region EPASS
        public bool IsUseEpass { get; set; }
        #endregion
    }

    public enum EmQRProvider
    {
        VIMO,
        TINGEE,
    }
}
