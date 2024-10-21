using Microsoft.IdentityModel.Tokens;
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
                expires: _dateTimeProvider.UtcNow.AddHours(1), // Token valid for 1 hour
                signingCredentials: creds
            );

            _logger.LogInformation("JWT token generated successfully for UserID: {UserId}", userId);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public RefreshToken GenerateRefreshToken(int userId, string createdByIp)
        {
            if (string.IsNullOrWhiteSpace(createdByIp))
            {
                _logger.LogWarning("Refresh token creation attempted with null or empty IP address.");
                throw new ArgumentNullException(nameof(createdByIp), "IP address cannot be null or empty.");
            }

            var refreshToken = new RefreshToken
            {
                Token = GenerateSecureToken(),
                UserId = userId,
                ExpirationTime = _dateTimeProvider.UtcNow.AddDays(7), // Refresh token valid for 7 days
                Created = _dateTimeProvider.UtcNow,
                CreatedByIp = createdByIp,
                // Revoked, RevokedByIp, ReplacedByToken are null by default
            };

            _logger.LogInformation("Refresh token generated successfully for UserID: {UserId}", userId);

            return refreshToken;
        }

        /// <summary>
        /// Generates a secure random token string.
        /// </summary>
        /// <returns>A secure, base64-encoded token string.</returns>
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