using iParkingv8.Object.Objects.SystemObjs;

namespace iParkingv8.Object.Objects.Systems
{
    public class ServerConfig
    {
        public EmApiVersion ApiVersion { get; set; } = EmApiVersion.v8;
        public string ApiUrl { get; set; } = string.Empty;
        public string LoginUrl { get; set; } = string.Empty;
        public string Scope { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string ServerName { get; set; } = string.Empty;
        public string ServerIp { get; set; } = string.Empty;
    }
}
