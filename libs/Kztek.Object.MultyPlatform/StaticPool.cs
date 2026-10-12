using iParkingv8.Object.Objects.Parkings;
using iParkingv8.Object.Objects.Users;

namespace Kztek.Object.MultyPlatform
{
    public static class StaticPool
    {
        public static User? SelectedUser = null;
        public static ClientDTO? SelectedClient = null;

        public static string GetSelectedUserName()
        {
            if (string.IsNullOrEmpty(SelectedUser?.Upn))
            {
                return SelectedClient?.ClientName ?? "";
            }
            return SelectedUser.Upn;
        }

        public static DateTime LoginTime = DateTime.Now;
        public static List<SystemConfig> configs = new List<SystemConfig>();
        //public static LicenseExpire? LicenseExpire;
    }
}
