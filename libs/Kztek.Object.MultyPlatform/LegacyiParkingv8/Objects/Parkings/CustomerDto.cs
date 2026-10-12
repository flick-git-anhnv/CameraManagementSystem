namespace iParkingv8.Object.Objects.Parkings
{
    public class CustomerDto
    {
        public string Id { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public CustomerCollectionDto? Collection { get; set; }
        public string VisitorRegistrationId { get; set; } = string.Empty;
        public Dictionary<string, string>? AdditionalData { get; set; } = [];
    }
}
