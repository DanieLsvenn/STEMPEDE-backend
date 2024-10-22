using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Moq;
using Xunit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Stemkit.Services.Interfaces;
using Stemkit.DTOs;
using Stemkit.Models;
using Google.Apis.Auth;
using Stemkit.Utils.Interfaces;
using Stemkit.Data;
using Stemkit.Tests.Helpers;
using Stemkit.Repositories.Interfaces;
using Stemkit.Auth.Services.Implementation;
using Stemkit.Auth.Helpers.Interfaces;


namespace Stemkit.Tests
{
    public class ExternalAuthServiceTests
    {
        // Mocks
        private readonly Mock<IUserService> _userServiceMock;
        private readonly Mock<IJwtTokenGenerator> _jwtTokenGeneratorMock;
        private readonly Mock<ILogger<ExternalAuthService>> _loggerMock;
        private readonly Mock<IGoogleTokenValidator> _googleTokenValidatorMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IRefreshTokenRepository> _refreshTokenRepositoryMock;

        // Service under test
        private readonly ExternalAuthService _externalAuthService;

        public ExternalAuthServiceTests()
        {
            _userServiceMock = new Mock<IUserService>();
            _jwtTokenGeneratorMock = new Mock<IJwtTokenGenerator>();
            _googleTokenValidatorMock = new Mock<IGoogleTokenValidator>();
            _loggerMock = new Mock<ILogger<ExternalAuthService>>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _refreshTokenRepositoryMock = new Mock<IRefreshTokenRepository>();

            // Setup the unit of work to return the appropriate repositories
            _unitOfWorkMock.Setup(uow => uow.GetRepository<RefreshToken>()).Returns(_refreshTokenRepositoryMock.Object);

            _externalAuthService = new ExternalAuthService(
                _unitOfWorkMock.Object,
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

            _googleTokenValidatorMock.SetupValidateAsync(idToken, payload);
            _userServiceMock.SetupGetUserByEmailAsync(email, null);
            _userServiceMock.SetupCreateUserAsync(user =>
            {
                user.UserId = 1; // Simulate user ID assignment after creation
                return Task.FromResult(user);
            });
            _userServiceMock.SetupAssignRoleAsync(1, "Customer");
            _userServiceMock.SetupGetUserRolesAsync(1, new List<string> { "Customer" });
            _jwtTokenGeneratorMock.SetupJwtTokenGenerator(
                userId: 1,
                roles: new List<string> { "Customer" },
                jwtToken: "MockedJwtToken",
                refreshToken: "MockedRefreshToken"
            );
            _refreshTokenRepositoryMock.Setup(repo => repo.AddAsync(It.IsAny<RefreshToken>()))
                                       .Returns(Task.CompletedTask);
            _unitOfWorkMock.SetupUnitOfWorkCompleteAsync();

            // Act
            var result = await _externalAuthService.GoogleLoginAsync(idToken, "192.168.1.1");

            // Assert
            Assert.True(result.Success, $"Login failed with message: {result.Message}");
            Assert.Equal("Login successful.", result.Message);
            Assert.Equal("MockedJwtToken", result.Token);
            Assert.Equal("MockedRefreshToken", result.RefreshToken);

            // Verify that the necessary methods were called
            _googleTokenValidatorMock.Verify(validator => validator.ValidateAsync(idToken), Times.Once);
            _userServiceMock.Verify(service => service.GetUserByEmailAsync(email), Times.Once);
            _userServiceMock.Verify(service => service.CreateUserAsync(It.Is<User>(u => u.Email == email && u.Username == name)), Times.Once);
            _userServiceMock.Verify(service => service.AssignRoleAsync(1, "Customer"), Times.Once);
            _userServiceMock.Verify(service => service.GetUserRolesAsync(1), Times.Once);
            _jwtTokenGeneratorMock.Verify(generator => generator.GenerateJwtToken(1, It.IsAny<List<string>>()), Times.Once);
            _refreshTokenRepositoryMock.Verify(repo => repo.AddAsync(It.IsAny<RefreshToken>()), Times.Once);
            _unitOfWorkMock.Verify(uow => uow.CompleteAsync(), Times.Once);
        }

        [Fact]
        public async Task GoogleLoginAsync_ValidToken_ExistingUser_ReturnsSuccess()
        {
            // Arrange
            var idToken = "VALID_ID_TOKEN";
            var email = "existinguser@example.com";
            var name = "Existing User";

            // Existing user
            var existingUser = new User { UserId = 1, Email = email, Username = name };

            // Mock the Google token validation to return a valid payload
            var payload = new GoogleJsonWebSignature.Payload
            {
                Email = email,
                Name = name
            };

            // Setup mocks using helper methods
            _googleTokenValidatorMock.SetupValidateAsync(idToken, payload);
            _userServiceMock.SetupGetUserByEmailAsync(email, existingUser);
            _userServiceMock.SetupGetUserRolesAsync(1, new List<string> { "Customer" });
            _jwtTokenGeneratorMock.SetupJwtTokenGenerator(
                userId: 1,
                roles: new List<string> { "Customer" },
                jwtToken: "MockedJwtToken",
                refreshToken: "MockedRefreshToken"
            );
            _unitOfWorkMock.SetupUnitOfWorkCompleteAsync();

            // Act
            var result = await _externalAuthService.GoogleLoginAsync(idToken, "192.168.1.1");

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Login successful.", result.Message);
            Assert.Equal("MockedJwtToken", result.Token);
            Assert.Equal("MockedRefreshToken", result.RefreshToken);

            // Verify that the necessary methods were called
            _googleTokenValidatorMock.Verify(validator => validator.ValidateAsync(idToken), Times.Once);
            _userServiceMock.Verify(service => service.GetUserByEmailAsync(email), Times.Once);
            _userServiceMock.Verify(service => service.CreateUserAsync(It.IsAny<User>()), Times.Never);
            _userServiceMock.Verify(service => service.AssignRoleAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
            _userServiceMock.Verify(service => service.GetUserRolesAsync(1), Times.Once);
            _jwtTokenGeneratorMock.Verify(generator => generator.GenerateJwtToken(1, It.IsAny<List<string>>()), Times.Once);
            _refreshTokenRepositoryMock.Verify(repo => repo.AddAsync(It.IsAny<RefreshToken>()), Times.Once);
            _unitOfWorkMock.Verify(uow => uow.CompleteAsync(), Times.Once);
        }

        [Fact]
        public async Task GoogleLoginAsync_InvalidToken_ReturnsFailure()
        {
            // Arrange
            var idToken = "INVALID_ID_TOKEN";

            // Mock token validation to return null (invalid token)
            _googleTokenValidatorMock.SetupValidateAsync(idToken, null);

            // Act
            var result = await _externalAuthService.GoogleLoginAsync(idToken, "192.168.1.1");

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Invalid Google token.", result.Message);
            Assert.Null(result.Token);

            // Verify that only ValidateAsync was called
            _googleTokenValidatorMock.Verify(validator => validator.ValidateAsync(idToken), Times.Once);
            _userServiceMock.Verify(service => service.GetUserByEmailAsync(It.IsAny<string>()), Times.Never);
            _userServiceMock.Verify(service => service.CreateUserAsync(It.IsAny<User>()), Times.Never);
            _userServiceMock.Verify(service => service.AssignRoleAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
            _userServiceMock.Verify(service => service.GetUserRolesAsync(It.IsAny<int>()), Times.Never);
            _jwtTokenGeneratorMock.Verify(generator => generator.GenerateJwtToken(It.IsAny<int>(), It.IsAny<List<string>>()), Times.Never);
            _refreshTokenRepositoryMock.Verify(repo => repo.AddAsync(It.IsAny<RefreshToken>()), Times.Never);
            _unitOfWorkMock.Verify(uow => uow.CompleteAsync(), Times.Never);
        }

        [Fact]
        public async Task GoogleLoginAsync_TokenValidationException_ReturnsFailure()
        {
            // Arrange
            var idToken = "VALID_ID_TOKEN";

            // Mock token validation to throw an exception
            _googleTokenValidatorMock.Setup(validator => validator.ValidateAsync(idToken))
                .ThrowsAsync(new Exception("Token validation failed"));

            // Act
            var result = await _externalAuthService.GoogleLoginAsync(idToken, "192.168.1.1");

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Login failed. Please try again.", result.Message);
            Assert.Null(result.Token);

            // Verify that ValidateAsync was called
            _googleTokenValidatorMock.Verify(validator => validator.ValidateAsync(idToken), Times.Once);
            // Ensure no further calls were made
            _userServiceMock.Verify(service => service.GetUserByEmailAsync(It.IsAny<string>()), Times.Never);
            _userServiceMock.Verify(service => service.CreateUserAsync(It.IsAny<User>()), Times.Never);
            _userServiceMock.Verify(service => service.AssignRoleAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
            _userServiceMock.Verify(service => service.GetUserRolesAsync(It.IsAny<int>()), Times.Never);
            _jwtTokenGeneratorMock.Verify(generator => generator.GenerateJwtToken(It.IsAny<int>(), It.IsAny<List<string>>()), Times.Never);
            _refreshTokenRepositoryMock.Verify(repo => repo.AddAsync(It.IsAny<RefreshToken>()), Times.Never);
            _unitOfWorkMock.Verify(uow => uow.CompleteAsync(), Times.Never);
        }

        //[Fact]
        //public async Task GoogleLoginAsync_DefaultRoleNotFound_ReturnsFailure()
        //{
        //    // Arrange
        //    var idToken = "VALID_ID_TOKEN";
        //    var email = "newuser@example.com";
        //    var name = "New User";

        //    // Mock the Google token validation to return a valid payload
        //    var payload = new GoogleJsonWebSignature.Payload
        //    {
        //        Email = email,
        //        Name = name
        //    };

        //    // Create a new user (does not exist)
        //    User nullUser = null;

        //    // Setup mocks using helper methods
        //    _googleTokenValidatorMock.SetupValidateAsync(idToken, payload);
        //    _userServiceMock.SetupGetUserByEmailAsync(email, nullUser);
        //    _userServiceMock.SetupCreateUserAsync(user =>
        //    {
        //        user.UserId = 1; // Simulate user ID assignment after creation
        //        return Task.FromResult(user);
        //    });
        //    _userServiceMock.SetupAssignRoleAsync(1, "Customer");
        //    // Since role assignment fails, other methods shouldn't be called

        //    // Act
        //    var result = await _externalAuthService.GoogleLoginAsync(idToken, "192.168.1.1");

        //    // Assert
        //    Assert.False(result.Success);
        //    Assert.Equal("Login failed. Please try again.", result.Message);
        //    Assert.Null(result.Token);

        //    // Verify method calls
        //    _googleTokenValidatorMock.Verify(validator => validator.ValidateAsync(idToken), Times.Once);
        //    _userServiceMock.Verify(service => service.GetUserByEmailAsync(email), Times.Once);
        //    _userServiceMock.Verify(service => service.CreateUserAsync(It.Is<User>(u => u.Email == email && u.Username == name)), Times.Once);
        //    _userServiceMock.Verify(service => service.AssignRoleAsync(1, "Customer"), Times.Once);
        //    _userServiceMock.Verify(service => service.CreateCustomerRecordAsync(It.IsAny<int>()), Times.Never);
        //    _userServiceMock.Verify(service => service.GetUserRolesAsync(It.IsAny<int>()), Times.Never);
        //    _jwtTokenGeneratorMock.Verify(generator => generator.GenerateJwtToken(It.IsAny<int>(), It.IsAny<List<string>>()), Times.Never);
        //    _refreshTokenRepositoryMock.Verify(repo => repo.AddAsync(It.IsAny<RefreshToken>()), Times.Never);
        //    _unitOfWorkMock.Verify(uow => uow.CompleteAsync(), Times.Never);
        //}

        //[Fact]
        //public async Task GoogleLoginAsync_UserCreationFails_ReturnsFailure()
        //{
        //    // Arrange
        //    var idToken = "VALID_ID_TOKEN";
        //    var email = "newuser@example.com";
        //    var name = "New User";

        //    // Mock the Google token validation to return a valid payload
        //    var payload = new GoogleJsonWebSignature.Payload
        //    {
        //        Email = email,
        //        Name = name
        //    };

        //    // Create a new user (does not exist)
        //    User nullUser = null;

        //    // Setup mocks using helper methods
        //    _googleTokenValidatorMock.SetupValidateAsync(idToken, payload);
        //    _userServiceMock.SetupGetUserByEmailAsync(email, nullUser);
        //    _userServiceMock.SetupCreateUserAsync(user =>
        //        Task.FromException<User>(new Exception("Database error")));
        //    // No need to setup AssignRoleAsync or CreateCustomerRecordAsync as user creation fails

        //    // Act
        //    var result = await _externalAuthService.GoogleLoginAsync(idToken, "192.168.1.1");

        //    // Assert
        //    Assert.False(result.Success);
        //    Assert.Equal("Login failed. Please try again.", result.Message);
        //    Assert.Null(result.Token);

        //    // Verify method calls
        //    _googleTokenValidatorMock.Verify(validator => validator.ValidateAsync(idToken), Times.Once);
        //    _userServiceMock.Verify(service => service.GetUserByEmailAsync(email), Times.Once);
        //    _userServiceMock.Verify(service => service.CreateUserAsync(It.Is<User>(u => u.Email == email && u.Username == name)), Times.Once);
        //    _userServiceMock.Verify(service => service.AssignRoleAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
        //    _userServiceMock.Verify(service => service.CreateCustomerRecordAsync(It.IsAny<int>()), Times.Never);
        //    _userServiceMock.Verify(service => service.GetUserRolesAsync(It.IsAny<int>()), Times.Never);
        //    _jwtTokenGeneratorMock.Verify(generator => generator.GenerateJwtToken(It.IsAny<int>(), It.IsAny<List<string>>()), Times.Never);
        //    _refreshTokenRepositoryMock.Verify(repo => repo.AddAsync(It.IsAny<RefreshToken>()), Times.Never);
        //    _unitOfWorkMock.Verify(uow => uow.CompleteAsync(), Times.Never);
        //}
    }
}
