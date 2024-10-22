namespace Stemkit.DTOs.AuthDTO
{
    public class AuthResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string Token { get; set; }// Access Token
        public string RefreshToken { get; set; } // Refresh Token
    }
}
