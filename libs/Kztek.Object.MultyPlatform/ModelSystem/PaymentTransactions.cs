using System;
using System.Collections.Generic;

namespace ParkingHelper.ModelSystem
{
    public class PaymentTransactions
    {
        private string id = string.Empty;
        private string parkingEventId = "";
        private int paymentMethod = 0;
        private int type = 0;
        private DateTime dateCreated = DateTime.MinValue;
        private string user = "";
        private PayInfomations payInfomation = new PayInfomations();
        private List<string> linkedTransaction = new List<string>();
        private string voucher = string.Empty;
        private List<string> image = null;
        private bool isDelete = false;
        private string note = "";
        private string token = "";

        public string Id { get => id; set => id = value; }
        public string ParkingEventId { get => parkingEventId; set => parkingEventId = value; }
        public int PaymentMethod { get => paymentMethod; set => paymentMethod = value; }
        public int Type { get => type; set => type = value; }
        public DateTime DateCreated { get => dateCreated; set => dateCreated = value; }
        public string User { get => user; set => user = value; }

        public PayInfomations PayInformation { get => payInfomation; set => payInfomation = value; }
        public List<string> LinkedTransaction { get => linkedTransaction; set => linkedTransaction = value; }

        public string Voucher { get => voucher; set => voucher = value; }
        public List<string> Image { get => image; set => image = value; }
        public bool IsDelete { get => isDelete; set => isDelete = value; }
        public string Note { get => note; set => note = value; }
        public string Token { get => token; set => token = value; }
        public string DiscountCode { get; set; } = string.Empty;
        public long DiscountAmount { get; set; } = 0;

    }

    public class ObjectBase
    {
        private string id;
        private string name;
        private string code;

        public string Id { get => id; set => id = value; }

        public string Name
        {
            get => name;
            set
            {
                if (value.IndexOfAny("!@#$%^&*()".ToCharArray()) != -1)
                {
                    throw new ArgumentException("Không được sử dụng ký tự đặc biệt");
                }
                name = value;
            }
        }

        public string Code
        {
            get => code;
            set
            {
                if (value.IndexOfAny("!@#$%^&*()".ToCharArray()) != -1)
                {
                    throw new ArgumentException("Không được sử dụng ký tự đặc biệt");
                }
                code = value;
            }
        }
    }



    public class PayInfomations
    {
        private long fee = 0;
        private long discount = 0;
        private long paid = 0;

        public long Fee { get => fee; set => fee = value; }
        public long Discount { get => discount; set => discount = value; }
        public long Paid { get => paid; set => paid = value; }
    }
    public class VoucherInfo
    {
        private string id = "";
        private string code = "";
        private string name = "";

        public string Id { get => id; set => id = value; }
        public string Code { get => code; set => code = value; }
        public string Name { get => name; set => name = value; }
    }
}
