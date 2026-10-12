using Avalonia.Media.Imaging;
using ParkingHelper.ModelSystem;

namespace Kztek.Object.MultyPlatform.ModelSystem
{
    public class tblCardEvent1
    {
        public string ID { get; set; }
        public int EventCode { get; set; }
        public string CardNumber { get; set; }
        public DateTime DatetimeIn { get; set; }
        public DateTime? DateTimeOut { get; set; }
        public List<ImageData> ImagesIn { get; set; }
        public List<ImageData> ImagesOut { get; set; }
        public string LaneIDIn { get; set; }
        public string LaneIDOut { get; set; }
        public string UserIDIn { get; set; }
        public string UserIDOut { get; set; }
        public string PlateIn { get; set; }
        public string PlateOut { get; set; }
        public string RegistedPlate { get; set; }
        public double Moneys { get; set; }
        public string CardGroupID { get; set; }
        public string VehicleGroupID { get; set; }
        public string CustomerGroupID { get; set; }
        public string CustomerName { get; set; }
        public bool IsBlackList { get; set; }
        public bool IsFree { get; set; }
        public bool IsDelete { get; set; }
        public string FreeType { get; set; }
        public string CardNo { get; set; }
        public string Description { get; set; }
        public string PaperTicketNumber { get; set; }
        public bool IsIncludePaperTicket { get; set; }
        public string UnsignPlateIn { get; set; }
        public string UnsignPlateOut { get; set; }
        public string Token { get; set; }
        public string InUserName { get; set; }
        public string InLaneName { get; set; }
        public long FeePaid { get; set; }
        public long Discount { get; set; }

        public List<string> AppliedVoucher { get; set; } = new List<string>();
        public List<string> PaymentTransactions { get; set; } = new List<string>();
        public DateTime LastPaymentTime { get; set; }
        public string chargeRateId { get; set; } = string.Empty;

        public Bitmap? VehicleImage { get; set; }
    }

}
