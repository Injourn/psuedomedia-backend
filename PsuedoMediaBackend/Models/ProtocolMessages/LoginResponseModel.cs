namespace PsuedoMediaBackend.Models.ProtocolMessages {
    public class LoginResponseModel {
        public string? OAuthToken { get; set; }
        public string? RefreshToken { get; set; }
    }
}
