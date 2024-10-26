namespace Stemkit.DTOs.Auth
{
    public class AuthResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;// Access Token
        public string RefreshToken { get; set; } = string.Empty; // Refresh Token
    }
}
