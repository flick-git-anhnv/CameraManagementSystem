namespace ParkingHelper.ModelSystem
{
    public class TokenResponse
    {
        private string identifier = "";
        private string expires_In = "";
        private string token = "";

        public string Identifier { get => identifier; set => identifier = value; }
        public string Expires_In { get => expires_In; set => expires_In = value; }
        public string Token { get => token; set => token = value; }
    }
}
