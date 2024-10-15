using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Moq;
using Xunit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Stemkit.Services.Interfaces;
using Stemkit.Services.Implementation;
using Stemkit.DTOs;
using Stemkit.Models;
using Google.Apis.Auth;
using Stemkit.Utils.Interfaces;


namespace Stemkit.Tests
{
    public class ExternalAuthServiceTests
    {
        // Mocks
        private readonly Mock<IUserService> _userServiceMock;
        private readonly Mock<IJwtTokenGenerator> _jwtTokenGeneratorMock;
        private readonly Mock<ILogger<ExternalAuthService>> _loggerMock;
        private readonly Mock<IGoogleTokenValidator> _googleTokenValidatorMock;

        // Service under test
        private readonly ExternalAuthService _externalAuthService;

        public ExternalAuthServiceTests()
        {
            _userServiceMock = new Mock<IUserService>();
            _jwtTokenGeneratorMock = new Mock<IJwtTokenGenerator>();
            _googleTokenValidatorMock = new Mock<IGoogleTokenValidator>();
            _loggerMock = new Mock<ILogger<ExternalAuthService>>();

            // Initialize the ExternalAuthService with mocks
            _externalAuthService = new ExternalAuthService(
                _userServiceMock.Object,
                _jwtTokenGeneratorMock.Object,
                _googleTokenValidatorMock.Object,
                _loggerMock.Object
            );
        }


        [Fact]
        public async Task GoogleLoginAsync_ValidToken_NewUser_ReturnsSuccess()
        {
            // Arrange
            var idToken = "VALID_ID_TOKEN";
            var email = "newuser@example.com";
            var name = "New User";

            // Mock the Google token validation to return a valid payload
            var payload = new GoogleJsonWebSignature.Payload
            {
                Email = email,
                Name = name
            };

            var externalAuthServiceMock = new Mock<ExternalAuthService>(
                _userServiceMock.Object,
                _jwtTokenGeneratorMock.Object,
                _googleTokenValidatorMock.Object,
                _loggerMock.Object
            );
            externalAuthServiceMock.CallBase = true;

            _googleTokenValidatorMock.Setup(validator => validator.ValidateAsync(idToken)).ReturnsAsync(payload);

            // Mock user does not exist
            _userServiceMock.Setup(service => service.GetUserByEmailAsync(email)).ReturnsAsync((User)null);

            // Mock user creation
            _userServiceMock.Setup(service => service.CreateUserAsync(It.IsAny<User>())).ReturnsAsync((User user) =>
        {
            user.UserId = 1; // Set the UserId after creation
            return user;
        });

            // Mock role assignment
            _userServiceMock.Setup(service => service.AssignRoleAsync(1, "Customer"))
                .Returns(Task.CompletedTask);

            // Mock customer record creation
            _userServiceMock.Setup(service => service.CreateCustomerRecordAsync(1))
                .Returns(Task.CompletedTask);

            // Mock retrieving user roles
            _userServiceMock.Setup(service => service.GetUserRolesAsync(1))
                .ReturnsAsync(new List<string> { "Customer" });

            // Mock JWT token generation
            _jwtTokenGeneratorMock.Setup(generator => generator.GenerateJwtToken(1, It.IsAny<List<string>>()))
                .Returns("MockedJwtToken");

            // Act
            var result = await externalAuthServiceMock.Object.GoogleLoginAsync(idToken);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Login successful.", result.Message);
            Assert.Equal("MockedJwtToken", result.Token);
        }

        [Fact]
        public async Task GoogleLoginAsync_ValidToken_ExistingUser_ReturnsSuccess()
        {
            // Arrange
            var idToken = "VALID_ID_TOKEN";
            var email = "existinguser@example.com";
            var name = "Existing User";

            // Mock the Google token validation to return a valid payload
            var payload = new GoogleJsonWebSignature.Payload
            {
                Email = email,
                Name = name
            };

            var externalAuthServiceMock = new Mock<ExternalAuthService>(
                _userServiceMock.Object,
                _jwtTokenGeneratorMock.Object,
                _googleTokenValidatorMock.Object,
                _loggerMock.Object
            );
            externalAuthServiceMock.CallBase = true;

            _googleTokenValidatorMock.Setup(validator => validator.ValidateAsync(idToken))
    .ReturnsAsync(payload);

            // Mock user exists
            var existingUser = new User { UserId = 1, Email = email, Username = name };
            _userServiceMock.Setup(service => service.GetUserByEmailAsync(email))
                .ReturnsAsync(existingUser);

            // Mock retrieving user roles
            _userServiceMock.Setup(service => service.GetUserRolesAsync(1))
                .ReturnsAsync(new List<string> { "Customer" });

            // Mock JWT token generation
            _jwtTokenGeneratorMock.Setup(generator => generator.GenerateJwtToken(1, It.IsAny<List<string>>()))
                .Returns("MockedJwtToken");

            // Act
            var result = await externalAuthServiceMock.Object.GoogleLoginAsync(idToken);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Login successful.", result.Message);
            Assert.Equal("MockedJwtToken", result.Token);

            // Verify that CreateUserAsync and AssignRoleAsync were not called
            _userServiceMock.Verify(service => service.CreateUserAsync(It.IsAny<User>()), Times.Never);
            _userServiceMock.Verify(service => service.AssignRoleAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
            _userServiceMock.Verify(service => service.CreateCustomerRecordAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task GoogleLoginAsync_InvalidToken_ReturnsFailure()
        {
            // Arrange
            var idToken = "INVALID_ID_TOKEN";

            var externalAuthServiceMock = new Mock<ExternalAuthService>(
                _userServiceMock.Object,
                _jwtTokenGeneratorMock.Object,
                _googleTokenValidatorMock.Object,
                _loggerMock.Object
            );
            externalAuthServiceMock.CallBase = true;

            // Mock token validation to return null (invalid token)
            _googleTokenValidatorMock.Setup(validator => validator.ValidateAsync(idToken))
    .ReturnsAsync((GoogleJsonWebSignature.Payload)null);

            // Act
            var result = await externalAuthServiceMock.Object.GoogleLoginAsync(idToken);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Invalid Google token.", result.Message);
            Assert.Null(result.Token);
        }

        [Fact]
        public async Task GoogleLoginAsync_TokenValidationException_ReturnsFailure()
        {
            // Arrange
            var idToken = "VALID_ID_TOKEN";

            var externalAuthServiceMock = new Mock<ExternalAuthService>(
                _userServiceMock.Object,
                _jwtTokenGeneratorMock.Object,
                _googleTokenValidatorMock.Object,
                _loggerMock.Object
            );
            externalAuthServiceMock.CallBase = true;

            // Mock token validation to throw an exception
            _googleTokenValidatorMock.Setup(validator => validator.ValidateAsync(idToken))
        .ThrowsAsync(new Exception("Token validation failed"));

            // Act
            var result = await externalAuthServiceMock.Object.GoogleLoginAsync(idToken);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Login failed. Please try again.", result.Message);
            Assert.Null(result.Token);
        }

        [Fact]
        public async Task GoogleLoginAsync_DefaultRoleNotFound_ReturnsFailure()
        {
            // Arrange
            var idToken = "VALID_ID_TOKEN";
            var email = "newuser@example.com";
            var name = "New User";

            // Mock the Google token validation to return a valid payload
            var payload = new GoogleJsonWebSignature.Payload
            {
                Email = email,
                Name = name
            };

            var externalAuthServiceMock = new Mock<ExternalAuthService>(
                _userServiceMock.Object,
                _jwtTokenGeneratorMock.Object,
                _googleTokenValidatorMock.Object,
                _loggerMock.Object
            );
            externalAuthServiceMock.CallBase = true;

            _googleTokenValidatorMock.Setup(validator => validator.ValidateAsync(idToken))
    .ReturnsAsync(payload);

            // Mock user does not exist
            _userServiceMock.Setup(service => service.GetUserByEmailAsync(email))
                .ReturnsAsync((User)null);

            // Mock user creation
            _userServiceMock.Setup(service => service.CreateUserAsync(It.IsAny<User>()))
        .ReturnsAsync((User user) =>
        {
            user.UserId = 1; // Set the UserId after creation
            return user;
        });

            // Mock role assignment to throw an exception (role not found)
            _userServiceMock.Setup(service => service.AssignRoleAsync(1, "Customer"))
                .ThrowsAsync(new Exception("Role not found."));

            // Act
            var result = await externalAuthServiceMock.Object.GoogleLoginAsync(idToken);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Login failed. Please try again.", result.Message);
            Assert.Null(result.Token);
        }

        [Fact]
        public async Task GoogleLoginAsync_UserCreationFails_ReturnsFailure()
        {
            // Arrange
            var idToken = "VALID_ID_TOKEN";
            var email = "newuser@example.com";
            var name = "New User";

            // Mock the Google token validation to return a valid payload
            var payload = new GoogleJsonWebSignature.Payload
            {
                Email = email,
                Name = name
            };

            var externalAuthServiceMock = new Mock<ExternalAuthService>(
                _userServiceMock.Object,
                _jwtTokenGeneratorMock.Object,
                _googleTokenValidatorMock.Object,
                _loggerMock.Object
            );
            externalAuthServiceMock.CallBase = true;

            _googleTokenValidatorMock.Setup(validator => validator.ValidateAsync(idToken))
    .ReturnsAsync(payload);

            // Mock user does not exist
            _userServiceMock.Setup(service => service.GetUserByEmailAsync(email))
                .ReturnsAsync((User)null);

            // Mock user creation to throw an exception
            _userServiceMock.Setup(service => service.CreateUserAsync(It.IsAny<User>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await externalAuthServiceMock.Object.GoogleLoginAsync(idToken);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Login failed. Please try again.", result.Message);
            Assert.Null(result.Token);
        }

    }
}
