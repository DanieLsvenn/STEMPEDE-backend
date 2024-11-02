﻿namespace Stemkit.DTOs.Auth
{
    public class LoginResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;// Access Token
        public string RefreshToken { get; set; } = string.Empty; // Refresh Token
        public List<string> Roles { get; set; }
    }
}