using System;

namespace ParkingHelper.ModelSystem
{
    public class VouchersAvailable
    {
        private string id;
        private string groupId;
        private string code;
        private VoucherTypeBaseInfo voucherType;
        private DateTime dateCreated = DateTime.Now;
        private string user;
        private ObjectBase releaseUnit;
        private int codeFormat = 0;
        private DateTime effectiveDate;
        private DateTime expirationDate;
        private string token;
        private int voucherReleaseType = 0;

        public long DiscountAmount { get; set; } = 0;

        public string Id
        {
            get => id;
            set => id = value;
        }

        public string GroupId
        {
            get => groupId;
            set => groupId = value;
        }

        public string Code
        {
            get => code;
            set => code = value;
        }

        public VoucherTypeBaseInfo VoucherType
        {
            get => voucherType;
            set => voucherType = value;
        }

        public DateTime DateCreated
        {
            get => dateCreated;
            set => dateCreated = value;
        }

        public string User
        {
            get => user;
            set => user = value;
        }

        public ObjectBase ReleaseUnit
        {
            get => releaseUnit;
            set => releaseUnit = value;
        }

        public int CodeFormat
        {
            get => codeFormat;
            set => codeFormat = value;
        }

        public DateTime EffectiveDate
        {
            get => effectiveDate;
            set => effectiveDate = value;
        }

        public DateTime ExpirationDate
        {
            get => expirationDate;
            set => expirationDate = value;
        }

        public string Token
        {
            get => token;
            set => token = value;
        }

        /// <summary>
        /// Hình thức phát hành voucher:
        /// [0] - Fixed số lượng, phát hành bởi chính hệ thống này
        /// [1] - sử dụng trong ngày, được phát hành bởi đơn vị khác nhưng chịu sự quản lý bởi hệ thống này (format, price,.. voucher)
        /// [2] - Bên thứ 3, không thuộc sự quản lý của hệ thống
        /// </summary>
        public int VoucherReleaseType
        {
            get => voucherReleaseType;
            set => voucherReleaseType = value;
        }

        public int ReleaseQuantity { get; set; }

        public int ConsumptionQuantity { get; set; }
        public string ErrorMessageVi { get; set; } = string.Empty;
        public string ErrorMessageEn { get; set; } = string.Empty;
    }
    public class VoucherTypeBaseInfo : ObjectBase
    {
        private int type = 0;
        private float reduceAmount;

        public int Type { get => type; set => type = value; }
        public float ReduceAmount { get => reduceAmount; set => reduceAmount = value; }
    }

    public class VoucherTypeDetail : VoucherTypeBaseInfo
    {
        private DateTime dateCreated = DateTime.Now;
        private string user;
        private bool inActive = false;
        private bool isDelete = false;
        private string description;

        public DateTime DateCreated { get => dateCreated; set => dateCreated = value; }
        public string User { get => user; set => user = value; }
        public bool InActive { get => inActive; set => inActive = value; }
        public bool IsDelete { get => isDelete; set => isDelete = value; }
        public string Description { get => description; set => description = value; }

        public string UserName { get; set; }
    }
}
