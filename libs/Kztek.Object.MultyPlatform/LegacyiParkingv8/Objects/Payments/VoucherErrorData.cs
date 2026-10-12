namespace iParkingv8.Object.Objects.Payments
{
    public class VoucherErrorData
    {
        public List<VoucherErrorDetail> Fields { get; set; } = [];
        public bool IsSuccess { get; set; } = true;
        public string Message { get; set; } = string.Empty;
        public string ErrorCode { get; set; } = string.Empty;
        public string DetailCode { get; set; } = string.Empty;
        public string GetErrorVi()
        {
            return Fields.Count == 0 ? "Không đọc được thông tin voucher" : Fields[0].ToStringVi();
        }
        public string GetErrorEn()
        {
            return Fields.Count == 0 ? "Can't get voucher info" : Fields[0].ToStringEn();
        }
    }
    public class VoucherErrorDetail
    {
        public string Name { get; set; } = string.Empty;
        public string ErrorCode { get; set; } = string.Empty;
        public string ToStringVi()
        {
            if (Name.Equals("EventInVoucher", StringComparison.CurrentCultureIgnoreCase))
            {
                if (ErrorCode == "ERROR.ENTITY.VALIDATION.FIELD_DUPLICATED")
                {
                    return "Bạn đã áp dụng voucher này trước đó".ToUpper();
                }
            }
            if (Name.Equals("Code", StringComparison.CurrentCultureIgnoreCase))
            {
                if (ErrorCode == "ERROR.ENTITY.VALIDATION.FIELD_NOT_FOUND")
                {
                    return "Voucher không tồn tại trong hệ thống".ToUpper();
                }
            }
            return "Không đọc được thông tin voucher";
        }
        public string ToStringEn()
        {
            if (Name.Equals("EventInVoucher", StringComparison.CurrentCultureIgnoreCase))
            {
                if (ErrorCode == "ERROR.ENTITY.VALIDATION.FIELD_DUPLICATED")
                {
                    return "You have used this voucher".ToUpper();
                }
            }

            if (Name.Equals("Code", StringComparison.CurrentCultureIgnoreCase))
            {
                if (ErrorCode == "ERROR.ENTITY.VALIDATION.FIELD_NOT_FOUND")
                {
                    return "Voucher is not exist in system".ToUpper();
                }
            }
            return "Can't get voucher info";
        }
    }
}
