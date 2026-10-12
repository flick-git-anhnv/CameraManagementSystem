namespace iParkingv8.Object.Objects.Users
{
    public class User
    {
        public string Id { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Upn { get; set; } = string.Empty;

        //CŨ
        public List<string> Rights { get; set; } = [];
        public List<string> screenFeatures { get; set; } = [];

        //MỚI
        public List<Client8> Clients { get; set; } = [];

        public bool IsValidWindowsAppPermission() => this.Clients.Where(e => e.Name == "Windows app").FirstOrDefault() != null;
    }
    public class ClientDTO
    {
        public string ClientId { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;
    }
    public class Client8
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }
}
