using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Stemkit.Utils.Interfaces;
using Stemkit.Utils.Implementation;
using Xunit;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;

namespace Stemkit.Tests
{
    public class JwtTokenGeneratorTests
    {
        public static string GenerateSecureKey(int byteLength = 32)
        {
            byte[] keyBytes = new byte[byteLength];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(keyBytes);
            }
            return Convert.ToBase64String(keyBytes);
        }

        [Fact]
        public void GenerateJwtToken_ShouldReturnToken_WhenInputsAreValid()
        {
            // Arrange
            var mockConfig = new Mock<IConfiguration>();
            var secureKey = GenerateSecureKey(32);
            mockConfig.Setup(c => c["Authentication:Jwt:Secret"]).Returns(secureKey);
            mockConfig.Setup(c => c["Authentication:Jwt:Issuer"]).Returns("StemkitApp");
            mockConfig.Setup(c => c["Authentication:Jwt:Audience"]).Returns("StemkitUsers");

            var mockDateTime = new Mock<IDateTimeProvider>();
            var fixedDateTime = new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            mockDateTime.Setup(dp => dp.UtcNow).Returns(fixedDateTime);

            var mockLogger = new Mock<ILogger<JwtTokenGenerator>>();

            var jwtGenerator = new JwtTokenGenerator(mockConfig.Object, mockDateTime.Object, mockLogger.Object);
            int userId = 1;
            var roles = new List<string> { "Customer" };

            // Act
            var token = jwtGenerator.GenerateJwtToken(userId, roles);

            // Assert
            Assert.False(string.IsNullOrEmpty(token));

            // Additional Assert: Validate token structure
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            Assert.Equal("StemkitApp", jwtToken.Issuer);

            // Access Audience via Claims
            var audienceClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Aud)?.Value;
            Assert.Equal("StemkitUsers", audienceClaim);

            Assert.Equal(userId.ToString(), jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value);
            Assert.NotNull(jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value);
            Assert.Contains(jwtToken.Claims, c => c.Type == ClaimTypes.Role && c.Value == "Customer");
        }

        [Fact]
        public void GenerateJwtToken_ShouldThrowArgumentNullException_WhenRolesAreNull()
        {
            // Arrange
            var mockConfig = new Mock<IConfiguration>();
            var secureKey = GenerateSecureKey(32);
            mockConfig.Setup(c => c["Authentication:Jwt:Secret"]).Returns(secureKey);
            mockConfig.Setup(c => c["Authentication:Jwt:Issuer"]).Returns("StemkitApp");
            mockConfig.Setup(c => c["Authentication:Jwt:Audience"]).Returns("StemkitUsers");

            var mockDateTime = new Mock<IDateTimeProvider>();
            var fixedDateTime = new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            mockDateTime.Setup(dp => dp.UtcNow).Returns(fixedDateTime);

            var mockLogger = new Mock<ILogger<JwtTokenGenerator>>();

            var jwtGenerator = new JwtTokenGenerator(mockConfig.Object, mockDateTime.Object, mockLogger.Object);
            int userId = 1;
            List<string> roles = null;

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => jwtGenerator.GenerateJwtToken(userId, roles));
            Assert.Equal("roles", exception.ParamName);
        }

        [Fact]
        public void GenerateJwtToken_ShouldThrowArgumentNullException_WhenSecretIsMissing()
        {
            // Arrange
            var mockConfig = new Mock<IConfiguration>();
            mockConfig.Setup(c => c["Authentication:Jwt:Secret"]).Returns((string)null);
            mockConfig.Setup(c => c["Authentication:Jwt:Issuer"]).Returns("StemkitApp");
            mockConfig.Setup(c => c["Authentication:Jwt:Audience"]).Returns("StemkitUsers");

            var mockDateTime = new Mock<IDateTimeProvider>();
            var fixedDateTime = new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            mockDateTime.Setup(dp => dp.UtcNow).Returns(fixedDateTime);

            var mockLogger = new Mock<ILogger<JwtTokenGenerator>>();

            var jwtGenerator = new JwtTokenGenerator(mockConfig.Object, mockDateTime.Object, mockLogger.Object);
            int userId = 1;
            var roles = new List<string> { "Customer" };

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => jwtGenerator.GenerateJwtToken(userId, roles));
            Assert.Equal("secret", exception.ParamName);
        }

        [Fact]
        public void GenerateJwtToken_ShouldThrowArgumentNullException_WhenIssuerIsMissing()
        {
            // Arrange
            var mockConfig = new Mock<IConfiguration>();
            var secureKey = GenerateSecureKey(32);
            mockConfig.Setup(c => c["Authentication:Jwt:Secret"]).Returns(secureKey);
            mockConfig.Setup(c => c["Authentication:Jwt:Issuer"]).Returns((string)null);
            mockConfig.Setup(c => c["Authentication:Jwt:Audience"]).Returns("StemkitUsers");

            var mockDateTime = new Mock<IDateTimeProvider>();
            var fixedDateTime = new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            mockDateTime.Setup(dp => dp.UtcNow).Returns(fixedDateTime);

            var mockLogger = new Mock<ILogger<JwtTokenGenerator>>();

            var jwtGenerator = new JwtTokenGenerator(mockConfig.Object, mockDateTime.Object, mockLogger.Object);
            int userId = 1;
            var roles = new List<string> { "Customer" };

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => jwtGenerator.GenerateJwtToken(userId, roles));
            Assert.Equal("issuer", exception.ParamName);
        }

        [Fact]
        public void GenerateJwtToken_ShouldThrowArgumentNullException_WhenAudienceIsMissing()
        {
            // Arrange
            var mockConfig = new Mock<IConfiguration>();
            var secureKey = GenerateSecureKey(32);
            mockConfig.Setup(c => c["Authentication:Jwt:Secret"]).Returns(secureKey);
            mockConfig.Setup(c => c["Authentication:Jwt:Issuer"]).Returns("StemkitApp");
            mockConfig.Setup(c => c["Authentication:Jwt:Audience"]).Returns((string)null);

            var mockDateTime = new Mock<IDateTimeProvider>();
            var fixedDateTime = new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            mockDateTime.Setup(dp => dp.UtcNow).Returns(fixedDateTime);

            var mockLogger = new Mock<ILogger<JwtTokenGenerator>>();

            var jwtGenerator = new JwtTokenGenerator(mockConfig.Object, mockDateTime.Object, mockLogger.Object);
            int userId = 1;
            var roles = new List<string> { "Customer" };

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => jwtGenerator.GenerateJwtToken(userId, roles));
            Assert.Equal("audience", exception.ParamName);
        }

        [Fact]
        public void GenerateJwtToken_ShouldIncludeAllRoles_InGeneratedToken()
        {
            // Arrange
            var mockConfig = new Mock<IConfiguration>();
            var secureKey = GenerateSecureKey(32);
            mockConfig.Setup(c => c["Authentication:Jwt:Secret"]).Returns(secureKey);
            mockConfig.Setup(c => c["Authentication:Jwt:Issuer"]).Returns("StemkitApp");
            mockConfig.Setup(c => c["Authentication:Jwt:Audience"]).Returns("StemkitUsers");

            var mockDateTime = new Mock<IDateTimeProvider>();
            var fixedDateTime = new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            mockDateTime.Setup(dp => dp.UtcNow).Returns(fixedDateTime);

            var mockLogger = new Mock<ILogger<JwtTokenGenerator>>();

            var jwtGenerator = new JwtTokenGenerator(mockConfig.Object, mockDateTime.Object, mockLogger.Object);
            int userId = 1;
            var roles = new List<string> { "Customer", "Admin" };

            // Act
            var token = jwtGenerator.GenerateJwtToken(userId, roles);
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            // Assert
            var roleClaims = jwtToken.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();
            Assert.Equal(2, roleClaims.Count);
            Assert.Contains("Customer", roleClaims);
            Assert.Contains("Admin", roleClaims);
        }

        [Fact]
        public void GenerateJwtToken_ShouldSetCorrectExpirationTime()
        {
            // Arrange
            var mockConfig = new Mock<IConfiguration>();
            var secureKey = GenerateSecureKey(32);
            mockConfig.Setup(c => c["Authentication:Jwt:Secret"]).Returns(secureKey);
            mockConfig.Setup(c => c["Authentication:Jwt:Issuer"]).Returns("StemkitApp");
            mockConfig.Setup(c => c["Authentication:Jwt:Audience"]).Returns("StemkitUsers");

            var mockDateTime = new Mock<IDateTimeProvider>();
            var fixedDateTime = new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            mockDateTime.Setup(dp => dp.UtcNow).Returns(fixedDateTime);

            var mockLogger = new Mock<ILogger<JwtTokenGenerator>>();

            var jwtGenerator = new JwtTokenGenerator(mockConfig.Object, mockDateTime.Object, mockLogger.Object);
            int userId = 1;
            var roles = new List<string> { "Customer" };

            // Act
            var token = jwtGenerator.GenerateJwtToken(userId, roles);
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            // Assert
            var expiration = jwtToken.ValidTo;
            Assert.Equal(fixedDateTime.AddHours(1), expiration);
        }
    }
}
