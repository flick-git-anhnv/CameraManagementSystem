using iParkingv8.Object.Objects.Devices;
using iParkingv8.Object.Objects.Invoices;
using iParkingv8.Object.Objects.Parkings;
using iParkingv8.Object.Objects.Payments;
using System.ComponentModel.DataAnnotations;

namespace iParkingv8.Object.Objects.Events
{
    public class ExitData : BaseEvent
    {
        public EntryData? Entry { get; set; }
        [Display(AutoGenerateField = false, Description = "OrderID field is not generated in UI")]
        public string Id { get; set; } = string.Empty;
        public string PlateNumber { get; set; } = string.Empty;
        public bool Exited { get; set; } = false;
        public string Note { get; set; } = string.Empty;
        public bool IgnoreValidator { get; set; } = false;
        public string CreatedBy { get; set; } = string.Empty;
        [Display(AutoGenerateField = false, Description = "OrderID field is not generated in UI")]
        public string CreatedUtc { get; set; } = string.Empty;
        [Display(AutoGenerateField = false, Description = "OrderID field is not generated in UI")]
        public string UpdatedUtc { get; set; } = string.Empty;
        public List<EventImageDto> Images { get; set; } = [];
        public AccessKey? AccessKey { get; set; }
        public BaseDevice? Device { get; set; }
        public bool OpenBarrier { get; set; } = false;
        [DataType(DataType.Currency)]
        public long Amount { get; set; } = 0;
        /// <summary>
        /// Sau khi xe ra, toàn bộ discount áp dụng khi xe chưa ra khỏi bãi sẽ được tổng hợp tại đây
        /// </summary>
        public long DiscountAmount { get; set; }
        [Display(Name = "Giờ Ra")]
        public DateTime DatetimeOut
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
                        return DateTime.Parse(CreatedUtc).AddHours(7);
                    }
                }
                catch
                {
                    return DateTime.Now;
                }
            }
        }
        public List<VoucherApply> Vouchers { get; set; } = [];
        public AccessKey? UnreturnedAccessKey { get; set; }

        public InvoiceData? Invoice { get; set; }

        public Collection? Collection { get; set; }

        [Display(Name = "Nhóm", Order = 3)]
        public string CollecionName { get => Collection?.Name ?? this.AccessKey?.Collection?.Name ?? ""; }

        [Display(Name = "Id Nhóm", Order = 3)]
        public string CollecionID { get => Collection?.Id ?? this.AccessKey?.Collection?.Id ?? ""; }
    }
}
