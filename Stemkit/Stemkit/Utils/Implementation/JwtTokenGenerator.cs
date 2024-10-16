﻿using Microsoft.IdentityModel.Tokens;
using Stemkit.Models;
using Stemkit.Utils.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Stemkit.Utils.Implementation
{
    public class JwtTokenGenerator : IJwtTokenGenerator
    {
        private readonly IConfiguration _configuration;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly ILogger<JwtTokenGenerator> _logger;

        public JwtTokenGenerator(IConfiguration configuration, IDateTimeProvider dateTimeProvider, ILogger<JwtTokenGenerator> logger)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public string GenerateJwtToken(int userId, List<string> roles)
        {
            if (roles == null || roles.Count == 0)
            {
                _logger.LogWarning("Attempted to generate JWT token with null or empty roles.");
                throw new ArgumentNullException(nameof(roles), "Roles cannot be null or empty.");
            }

            var secret = _configuration["Authentication:Jwt:Secret"];
            var issuer = _configuration["Authentication:Jwt:Issuer"];
            var audience = _configuration["Authentication:Jwt:Audience"];

            if (string.IsNullOrEmpty(secret))
            {
                _logger.LogError("JWT Secret is not configured.");
                throw new ArgumentNullException(nameof(secret), "JWT Secret is not configured.");
            }

            if (string.IsNullOrEmpty(issuer))
            {
                _logger.LogError("JWT Issuer is not configured.");
                throw new ArgumentNullException(nameof(issuer), "JWT Issuer is not configured.");
            }

            if (string.IsNullOrEmpty(audience))
            {
                _logger.LogError("JWT Audience is not configured.");
                throw new ArgumentNullException(nameof(audience), "JWT Audience is not configured.");
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

            // Add roles as claims
            foreach (var role in roles)
            {
                if (!string.IsNullOrWhiteSpace(role))
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }
            }

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: _dateTimeProvider.UtcNow.AddHours(1),
                signingCredentials: creds
            );

            _logger.LogInformation("JWT token generated successfully for UserID: {UserId}", userId);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public RefreshToken GenerateRefreshToken(int userId, string createdByIp)
        {
            var refreshToken = new RefreshToken
            {
                Token = GenerateSecureToken(),
                UserId = userId,
                Expires = _dateTimeProvider.UtcNow.AddDays(7), // Set desired expiration
                Created = _dateTimeProvider.UtcNow,
                CreatedByIp = createdByIp
            };

            _logger.LogInformation("Refresh token generated successfully for UserID: {UserId}", userId);

            return refreshToken;
        }

        private string GenerateSecureToken()
        {
            var randomNumber = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
                return Convert.ToBase64String(randomNumber);
            }
        }
    }
}