using iParkingv8.Object.Objects.Devices;
using iParkingv8.Object.Objects.Parkings;
using System.ComponentModel.DataAnnotations;

namespace iParkingv8.Object.Objects.Events
{
    public class EntryData : BaseEvent
    {
        [Display(AutoGenerateField = false)]
        public string Id { get; set; } = string.Empty;

        [Display(Name = "Biển Số Xe", Order = 1)]
        public string PlateNumber { get; set; } = string.Empty;

        [Display(Name = "Giờ Vào", Order = 2)]
        [DisplayFormat(DataFormatString = "{HH:mm:ss}", ApplyFormatInEditMode = true)]
        public DateTime DateTimeIn
        {
            get
            {
                try
                {
                    if (CreatedUtc.Contains('T'))
                    {
                        return DateTime.ParseExact(CreatedUtc[.."yyyy-MM-ddTHH:mm:ss".Length], "yyyy-MM-ddTHH:mm:ss", null).AddHours(7);
                    }
                    else
                    {
                        try
                        {
                            return DateTime.Parse(CreatedUtc).AddHours(7);
                        }
                        catch (Exception ex)
                        {
                        }
                        return DateTime.Now;
                    }
                }
                catch
                {
                    return DateTime.Now;
                }
            }
        }

        [Display(Name = "Ghi Chú", Order = 3)]
        public string Note { get; set; } = string.Empty;

        [Display(Name = "Người Dùng", Order = 4)]
        public string CreatedBy { get; set; } = string.Empty;

        [Display(Name = "Làn", Order = 5)]
        public string LaneName { get => this.Device?.Name ?? ""; }

        [Display(Name = "Tên Thẻ", Order = 6)]
        public string AccessKeyName { get => this.AccessKey?.Name ?? ""; }

        [Display(Name = "Mã Thẻ", Order = 7)]
        public string AccessKeyCode { get => this.AccessKey?.Name ?? ""; }

        [Display(Name = "Biển Số Đăng Ký", Order = 7)]
        public string RegisterPlate { get => this.RegisterVehicle?.Code ?? ""; }

        [Display(Name = "Khách Hàng ", Order = 7)]
        public string CustomerName { get => this.RegisterVehicle?.Customer?.Name ?? ""; }

        [Display(AutoGenerateField = false)]
        public AccessKey? RegisterVehicle
        {
            get
            {
                if (this.AccessKey != null && this.AccessKey.Metrics != null && this.AccessKey.Metrics.Count > 0)
                {
                    return this.AccessKey.Metrics[0]; ;
                }
                return null;
            }
        }

        [Display(AutoGenerateField = false)]
        public bool Exited { get; set; } = false;

        [Display(AutoGenerateField = false)]
        public bool IgnoreValidator { get; set; } = false;

        [Display(AutoGenerateField = false)]
        public string CreatedUtc { get; set; } = string.Empty;

        [Display(AutoGenerateField = false)]
        public string UpdatedUtc { get; set; } = string.Empty;

        public List<EventImageDto> Images { get; set; } = [];
        public AccessKey? AccessKey { get; set; }
        public BaseDevice? Device { get; set; }

        [Display(AutoGenerateField = false)]
        public bool OpenBarrier { get; set; } = false;

        /// <summary>
        /// Số tiền thanh toán trước, chưa bao gồm discount amount
        /// </summary>
        [Display(Name = "Trả Trước")]
        [DisplayFormat(DataFormatString = "{0:N0}", ApplyFormatInEditMode = true)]
        public long Amount { get; set; } = 0;

        [Display(AutoGenerateField = false)]
        [DisplayFormat(DataFormatString = "{0:C0}", ApplyFormatInEditMode = true)]
        public long DiscountAmount { get; set; }

        public Collection? Collection { get; set; }




        [Display(Name = "Nhóm", Order = 3)]
        public string CollecionName { get => Collection?.Name ?? this.AccessKey?.Collection?.Name ?? ""; }

        [Display(Name = "Id Nhóm", Order = 3)]
        public string CollecionID { get => Collection?.Id ?? this.AccessKey?.Collection?.Id ?? ""; }
    }
}
