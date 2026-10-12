namespace iParkingv8.Object.Objects.Systems
{
    public class ClientLoginResponse
    {
        public string Id_token { get; set; } = string.Empty;
        public string AccessToken { get; set; } = string.Empty;
        public int Expires_in { get; set; } = 0;
        public string Token_type { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public string Scope { get; set; } = string.Empty;
    }
}
