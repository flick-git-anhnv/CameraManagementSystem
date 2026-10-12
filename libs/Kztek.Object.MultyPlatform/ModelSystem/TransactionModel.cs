using System.Collections.Generic;

namespace ParkingHelper.ModelSystem
{
    public class TransactionModel
    {
        private string id = "";
        private string parkingEventId = "";
        private int paymentMethod = 0;
        private int type = 0;
        private string user = "";
        private PayInfomations payInfomation = new PayInfomations();
        private List<string> linkedTransaction = new List<string>();
        private List<VoucherInfo> voucher = new List<VoucherInfo>();
        private List<ImageData> image = new List<ImageData>();
        private bool isDelete = false;
        private string note = "";
        private string token = "";

        public string Id { get => id; set => id = value; }

        public string ParkingEventId { get => parkingEventId; set => parkingEventId = value; }

        public int PaymentMethod { get => paymentMethod; set => paymentMethod = value; }

        public int Type { get => type; set => type = value; }

        public string User { get => user; set => user = value; }

        public PayInfomations PayInfomation { get => payInfomation; set => payInfomation = value; }

        public List<string> LinkedTransaction { get => linkedTransaction; set => linkedTransaction = value; }

        public List<VoucherInfo> Voucher { get => voucher; set => voucher = value; }

        public List<ImageData> Image { get => image; set => image = value; }

        public bool IsDelete { get => isDelete; set => isDelete = value; }

        public string Note { get => note; set => note = value; }

        public string Token { get => token; set => token = value; }
    }
}
