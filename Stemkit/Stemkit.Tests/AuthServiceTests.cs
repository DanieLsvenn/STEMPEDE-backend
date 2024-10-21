using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Moq;
using Stemkit.Models;
using Stemkit.Repositories.Interfaces;
using Stemkit.Services.Implementation;
using Stemkit.Services.Interfaces;
using Stemkit.Utils.Interfaces;
using Microsoft.Extensions.Logging;
using Xunit;
using Microsoft.EntityFrameworkCore.Storage;
using Stemkit.Data;
using Stemkit.DTOs;
using Stemkit.Tests.Helpers;

namespace Stemkit.Tests
{
    public class AuthServiceTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IGenericRepository<User>> _userRepositoryMock;
        private readonly Mock<IGenericRepository<Role>> _roleRepositoryMock;
        private readonly Mock<IGenericRepository<UserRole>> _userRoleRepositoryMock;
        private readonly Mock<IGenericRepository<Customer>> _customerRepositoryMock;
        private readonly Mock<IGenericRepository<Staff>> _staffRepositoryMock;
        private readonly Mock<ILogger<AuthService>> _loggerMock;
        private readonly Mock<IJwtTokenGenerator> _jwtTokenGeneratorMock;

        private readonly Mock<IDateTimeProvider> _mockDateTimeProvider;
        private readonly Mock<IRefreshTokenRepository> _refreshTokenRepositoryMock;
        private readonly IAuthService _authService;

        public AuthServiceTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _userRepositoryMock = new Mock<IGenericRepository<User>>();
            _roleRepositoryMock = new Mock<IGenericRepository<Role>>();
            _userRoleRepositoryMock = new Mock<IGenericRepository<UserRole>>();
            _customerRepositoryMock = new Mock<IGenericRepository<Customer>>();
            _staffRepositoryMock = new Mock<IGenericRepository<Staff>>();
            _refreshTokenRepositoryMock = new Mock<IRefreshTokenRepository>();
            _loggerMock = new Mock<ILogger<AuthService>>();
            _jwtTokenGeneratorMock = new Mock<IJwtTokenGenerator>();
            _mockDateTimeProvider = new Mock<IDateTimeProvider>();

            // Setup the unit of work to return the mocked repositories via properties
            _unitOfWorkMock.Setup(uow => uow.GetRepository<User>()).Returns(_userRepositoryMock.Object);
            _unitOfWorkMock.Setup(uow => uow.GetRepository<Role>()).Returns(_roleRepositoryMock.Object);
            _unitOfWorkMock.Setup(uow => uow.GetRepository<UserRole>()).Returns(_userRoleRepositoryMock.Object);
            _unitOfWorkMock.Setup(uow => uow.GetRepository<Customer>()).Returns(_customerRepositoryMock.Object);
            _unitOfWorkMock.Setup(uow => uow.GetRepository<Staff>()).Returns(_staffRepositoryMock.Object);
            _unitOfWorkMock.Setup(uow => uow.RefreshTokens).Returns(_refreshTokenRepositoryMock.Object); // Correct Setup

            // **Setup the RefreshTokens property**
            _unitOfWorkMock.Setup(uow => uow.RefreshTokens).Returns(_refreshTokenRepositoryMock.Object);

            _authService = new AuthService(
        _unitOfWorkMock.Object,
        _jwtTokenGeneratorMock.Object,
        _mockDateTimeProvider.Object,
        _loggerMock.Object
    );
        }

        [Fact]
        public async Task RegisterAsync_WithValidData_ReturnsSuccess()
        {
            // Arrange
            var registrationDto = new UserRegistrationDto
            {
                Username = "testuser",
                Password = "Test@123",
                Email = "testuser@example.com",
                Role = "Customer",
                Phone = "123-456-7890",
                Address = "123 Test Street",
                IsExternal = false,
                ExternalProvider = null
            };

            // Mock the user does not exist
            _userRepositoryMock.Setup(repo => repo.AnyAsync(It.IsAny<Expression<Func<User, bool>>>()))
                .ReturnsAsync(false);

            // Mock role exists
            var role = new Role { RoleId = 1, RoleName = "Customer" };
            _roleRepositoryMock.Setup(repo => repo.GetAsync(
                    It.IsAny<Expression<Func<Role, bool>>>(),
                    It.IsAny<string>()))
                .ReturnsAsync(role);

            // Mock adding user
            _userRepositoryMock.Setup(repo => repo.AddAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

            // Mock adding UserRole
            _userRoleRepositoryMock.Setup(repo => repo.AddAsync(It.IsAny<UserRole>()))
                .Returns(Task.CompletedTask);

            // Mock adding RefreshToken
            _refreshTokenRepositoryMock.Setup(repo => repo.AddAsync(It.IsAny<RefreshToken>()))
                .Returns(Task.CompletedTask);

            // Mock CompleteAsync with SetupSequence
            _unitOfWorkMock.SetupSequence(uow => uow.CompleteAsync())
                .ReturnsAsync(1) // After adding user and UserRole
                .ReturnsAsync(1) // After adding refresh token
                .ReturnsAsync(1); // If needed, for any additional CompleteAsync calls

            // Mock BeginTransactionAsync
            var transactionMock = new Mock<IDbContextTransaction>();
            transactionMock.Setup(t => t.CommitAsync(It.IsAny<System.Threading.CancellationToken>()))
                .Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(uow => uow.BeginTransactionAsync()).ReturnsAsync(transactionMock.Object);

            // Mock IJwtTokenGenerator methods
            _jwtTokenGeneratorMock.Setup(jtg => jtg.GenerateJwtToken(
                    It.IsAny<int>(),
                    It.IsAny<List<string>>()))
                .Returns("MockedJwtToken");

            _jwtTokenGeneratorMock.Setup(jtg => jtg.GenerateRefreshToken(
                    It.IsAny<int>(),
                    It.IsAny<string>()))
                .Returns(new RefreshToken
                {
                    Token = "MockedRefreshToken",
                    UserId = 1,
                    ExpirationTime = DateTime.UtcNow.AddDays(7),
                    Created = DateTime.UtcNow,
                    CreatedByIp = "127.0.0.1"
                });

            // Act
            var result = await _authService.RegisterAsync(registrationDto, "127.0.0.1");

            // Assert
            Assert.True(result.Success, $"Registration failed with message: {result.Message}");
            Assert.Equal("Registration successful.", result.Message);
            Assert.Equal("MockedJwtToken", result.Token);
            Assert.Equal("MockedRefreshToken", result.RefreshToken);
            _userRepositoryMock.Verify(repo => repo.AnyAsync(It.IsAny<Expression<Func<User, bool>>>()), Times.Once);
            _userRepositoryMock.Verify(repo => repo.AddAsync(It.IsAny<User>()), Times.Once);
            _roleRepositoryMock.Verify(repo => repo.GetAsync(
                It.IsAny<Expression<Func<Role, bool>>>(),
                It.IsAny<string>()), Times.Once);
            _userRoleRepositoryMock.Verify(repo => repo.AddAsync(It.IsAny<UserRole>()), Times.Once);
            _refreshTokenRepositoryMock.Verify(repo => repo.AddAsync(It.IsAny<RefreshToken>()), Times.Once);
            _unitOfWorkMock.Verify(uow => uow.CompleteAsync(), Times.Exactly(3)); // Adjust based on actual calls
            transactionMock.Verify(t => t.CommitAsync(It.IsAny<System.Threading.CancellationToken>()), Times.Once);
        }


        [Fact]
        public async Task RegisterAsync_UserAlreadyExists_ReturnsFailure()
        {
            // Arrange
            var registrationDto = new UserRegistrationDto
            {
                Username = "existinguser",
                Password = "Test@123",
                Email = "existinguser@example.com",
                Role = "Customer"
            };

            // Mock the user already exists
            _userRepositoryMock.Setup(repo => repo.AnyAsync(It.IsAny<Expression<Func<User, bool>>>()))
                .ReturnsAsync(true);

            // Mock role exists
            var role = new Role { RoleId = 1, RoleName = "Customer" };
            _roleRepositoryMock.Setup(repo => repo.GetAsync(
                    It.IsAny<Expression<Func<Role, bool>>>(),
                    It.IsAny<string>()))
                .ReturnsAsync(role);

            var ipAddress = "192.168.1.1";
            // Act
            var result = await _authService.RegisterAsync(registrationDto, ipAddress);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("User already exists.", result.Message);
            _userRepositoryMock.Verify(repo => repo.AddAsync(It.IsAny<User>()), Times.Never);
            _unitOfWorkMock.Verify(uow => uow.CompleteAsync(), Times.Never);
        }

        [Fact]
        public async Task RegisterAsync_WithInvalidRole_ReturnsFailure()
        {
            // Arrange
            var registrationDto = new UserRegistrationDto
            {
                Username = "testuser2",
                Password = "Test@123",
                Email = "testuser2@example.com",
                Role = "InvalidRole"
            };

            // Mock the user does not exist
            _userRepositoryMock.Setup(repo => repo.AnyAsync(It.IsAny<Expression<Func<User, bool>>>()))
                .ReturnsAsync(false);


            // Mock role does not exist
            _roleRepositoryMock.Setup(repo => repo.GetAsync(
                    It.IsAny<Expression<Func<Role, bool>>>(),
                    It.IsAny<string>()))
                .ReturnsAsync((Role)null);

            var ipAddress = "192.168.1.1";
            // Act
            var result = await _authService.RegisterAsync(registrationDto, ipAddress);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Invalid or missing role provided.", result.Message);
            _userRepositoryMock.Verify(repo => repo.AddAsync(It.IsAny<User>()), Times.Never);
            _unitOfWorkMock.Verify(uow => uow.CompleteAsync(), Times.Never);
        }

        [Fact]
        public async Task RegisterAsync_WithMissingRequiredFields_ReturnsFailure()
        {
            // Arrange
            var registrationDto = new UserRegistrationDto
            {
                Username = "",
                Password = "",
                Email = "",
                Role = ""
            };

            var ipAddress = "192.168.1.1";
            // Act
            var result = await _authService.RegisterAsync(registrationDto, ipAddress);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Invalid registration data.", result.Message);
            _userRepositoryMock.Verify(repo => repo.AnyAsync(It.IsAny<Expression<Func<User, bool>>>()), Times.Never);
            _userRepositoryMock.Verify(repo => repo.AddAsync(It.IsAny<User>()), Times.Never);
            _unitOfWorkMock.Verify(uow => uow.CompleteAsync(), Times.Never);
        }

        [Fact]
        public async Task RegisterAsync_ExceptionThrown_ReturnsFailure()
        {
            // Arrange
            var registrationDto = new UserRegistrationDto
            {
                Username = "testuser3",
                Password = "Test@123",
                Email = "testuser3@example.com",
                Role = "Customer"
            };

            // Mock the user does not exist
            _userRepositoryMock.Setup(repo => repo.AnyAsync(It.IsAny<Expression<Func<User, bool>>>()))
                .ReturnsAsync(false);

            // Mock role exists
            var role = new Role { RoleId = 1, RoleName = "Customer" };
            _roleRepositoryMock.Setup(repo => repo.GetAsync(
                    It.IsAny<Expression<Func<Role, bool>>>(),
                    It.IsAny<string>()))
                .ReturnsAsync(role);

            // Mock adding user throws exception
            _userRepositoryMock.Setup(repo => repo.AddAsync(It.IsAny<User>()))
                .ThrowsAsync(new Exception("Database error"));

            // Mock transaction
            var transactionMock = new Mock<IDbContextTransaction>();
            transactionMock.Setup(t => t.RollbackAsync(It.IsAny<System.Threading.CancellationToken>())).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(uow => uow.BeginTransactionAsync()).ReturnsAsync(transactionMock.Object);

            var ipAddress = "192.168.1.1";
            // Act
            var result = await _authService.RegisterAsync(registrationDto, ipAddress);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("An unexpected error occurred. Please try again.", result.Message);
            transactionMock.Verify(t => t.RollbackAsync(It.IsAny<System.Threading.CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task LoginAsync_WithValidCredentials_ReturnsSuccess()
        {
            // Arrange
            var loginDto = new UserLoginDto
            {
                EmailOrUsername = "testuser",
                Password = "Test@123"
            };

            // Hash the password as it would be stored in the database
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword("Test@123", workFactor: 12);

            // Define Role
            var role = new Role
            {
                RoleId = 1,
                RoleName = "Customer"
            };

            // Define UserRole with populated Role
            var userRole = new UserRole
            {
                UserRoleId = 1,
                UserId = 1,
                RoleId = role.RoleId,
                Role = role // Populate navigation property
            };

            var userRoles = new List<UserRole> { userRole };

            // Define User
            var user = new User
            {
                UserId = 1,
                Username = "testuser",
                Email = "testuser@example.com",
                Password = hashedPassword,
                UserRoles = userRoles // Assign the userRoles to the user
            };

            // Setup mock repositories using helper methods
            _userRepositoryMock.SetupUserRepository(user);
            _userRoleRepositoryMock.SetupUserRoleRepository(userRoles);
            _roleRepositoryMock.SetupRoleRepository(role);
            _jwtTokenGeneratorMock.SetupJwtTokenGenerator(
                userId: user.UserId,
                roles: new List<string> { role.RoleName },
                jwtToken: "MockedJwtToken",
                refreshToken: "MockedRefreshToken");
            _refreshTokenRepositoryMock.SetupRefreshTokenRepository(); // Setup specific repository
            _unitOfWorkMock.SetupUnitOfWorkCompleteAsync();

            // Act
            var result = await _authService.LoginAsync(loginDto, "192.168.1.1");

            // Assert
            Assert.True(result.Success, $"Login failed with message: {result.Message}");
            Assert.Equal("Login successful.", result.Message);
            Assert.Equal("MockedJwtToken", result.Token);
            Assert.Equal("MockedRefreshToken", result.RefreshToken);

            // Verify that GetAsync was called once for User
            _userRepositoryMock.Verify(repo => repo.GetAsync(
                    It.IsAny<Expression<Func<User, bool>>>(),
                    It.IsAny<string>()),
                Times.Once);

            // Verify that FindAsync for UserRole was called once
            _userRoleRepositoryMock.Verify(repo => repo.FindAsync(
                    It.IsAny<Expression<Func<UserRole, bool>>>(),
                    It.IsAny<string>()),
                Times.Once);

            // Verify that GenerateJwtToken was called once
            _jwtTokenGeneratorMock.Verify(generator => generator.GenerateJwtToken(
                    user.UserId,
                    It.IsAny<List<string>>()),
                Times.Once);

            // Verify that GenerateRefreshToken was called once
            _jwtTokenGeneratorMock.Verify(generator => generator.GenerateRefreshToken(
                    user.UserId,
                    It.IsAny<string>()),
                Times.Once);

            // Verify that RefreshToken was added once
            _refreshTokenRepositoryMock.Verify(repo => repo.AddAsync(It.IsAny<RefreshToken>()), Times.Once);

            // Verify that CompleteAsync was called once
            _unitOfWorkMock.Verify(uow => uow.CompleteAsync(), Times.Once);
        }

        [Fact]
        public async Task LoginAsync_UserDoesNotExist_ReturnsFailure()
        {
            // Arrange
            var loginDto = new UserLoginDto
            {
                EmailOrUsername = "nonexistentuser",
                Password = "Test@123"
            };

            // Mock the user does not exist
            _userRepositoryMock.Setup(repo => repo.GetAsync(
                    It.IsAny<Expression<Func<User, bool>>>(),
                    It.IsAny<string>()))
                .ReturnsAsync((User)null);

            var ipAddress = "192.168.1.1";
            // Act
            var result = await _authService.LoginAsync(loginDto, ipAddress);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Invalid credentials.", result.Message);
            Assert.Null(result.Token);
        }

        [Fact]
        public async Task LoginAsync_IncorrectPassword_ReturnsFailure()
        {
            // Arrange
            var loginDto = new UserLoginDto
            {
                EmailOrUsername = "testuser",
                Password = "WrongPassword"
            };

            // Hash a different password
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword("CorrectPassword", workFactor: 12);

            // Mock the user exists with a different password
            var user = new User
            {
                UserId = 1,
                Username = "testuser",
                Email = "testuser@example.com",
                Password = hashedPassword
            };

            _userRepositoryMock.Setup(repo => repo.GetAsync(
                    It.IsAny<Expression<Func<User, bool>>>(),
                    It.IsAny<string>()))
                .ReturnsAsync(user);

            var ipAddress = "192.168.1.1";
            // Act
            var result = await _authService.LoginAsync(loginDto, ipAddress);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Invalid credentials.", result.Message);
            Assert.Null(result.Token);
        }

        [Fact]
        public async Task LoginAsync_MissingRequiredFields_ReturnsFailure()
        {
            // Arrange
            var loginDto = new UserLoginDto
            {
                EmailOrUsername = "",
                Password = ""
            };

            var ipAddress = "192.168.1.1";
            // Act
            var result = await _authService.LoginAsync(loginDto, ipAddress);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Invalid login data.", result.Message);
            Assert.Null(result.Token);
            _userRepositoryMock.Verify(repo => repo.GetAsync(
                    It.IsAny<Expression<Func<User, bool>>>(),
                    It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task LoginAsync_ExceptionThrown_ReturnsFailure()
        {
            // Arrange
            var loginDto = new UserLoginDto
            {
                EmailOrUsername = "testuser",
                Password = "Test@123"
            };

            // Mock an exception when trying to get the user
            _userRepositoryMock.Setup(repo => repo.GetAsync(
                    It.IsAny<Expression<Func<User, bool>>>(),
                    It.IsAny<string>()))
                .ThrowsAsync(new Exception("Database error"));

            var ipAddress = "192.168.1.1";
            // Act
            var result = await _authService.LoginAsync(loginDto, ipAddress);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Login failed. Please try again.", result.Message);
            Assert.Null(result.Token);
        }

        [Fact]
        public async Task RefreshTokenAsync_ValidToken_ReturnsNewTokens()
        {
            // Arrange
            var existingRefreshToken = new RefreshToken
            {
                Id = 1,
                Token = "valid-refresh-token",
                UserId = 1,
                ExpirationTime = DateTime.UtcNow.AddDays(7),
                Created = DateTime.UtcNow,
                CreatedByIp = "127.0.0.1"
            };

            var user = new User
            {
                UserId = 1,
                Username = "testuser",
                Email = "test@example.com",
                // Other properties...
            };

            var role = new Role
            {
                RoleId = 1,
                RoleName = "Customer"
            };

            var userRole = new UserRole
            {
                UserRoleId = 1,
                UserId = user.UserId,
                RoleId = role.RoleId,
                Role = role // Populate navigation property
            };

            var userRoles = new List<UserRole> { userRole };

            var newAccessToken = "new-access-token";
            var newRefreshToken = new RefreshToken
            {
                Token = "new-refresh-token",
                UserId = 1,
                ExpirationTime = DateTime.UtcNow.AddDays(7),
                Created = DateTime.UtcNow,
                CreatedByIp = "127.0.0.1"
            };

            // Mock GetAsync for RefreshTokens to return existingRefreshToken
            _refreshTokenRepositoryMock.Setup(repo => repo.GetAsync(
                    It.IsAny<Expression<Func<RefreshToken, bool>>>(),
                    It.IsAny<string>()))
                .ReturnsAsync(existingRefreshToken);

            // Mock GetByIdAsync for User repository to return user
            _userRepositoryMock.Setup(repo => repo.GetByIdAsync(existingRefreshToken.UserId))
                .ReturnsAsync(user);

            // Mock FindAsync for UserRole repository to return userRoles with Role populated
            _userRoleRepositoryMock.Setup(repo => repo.FindAsync(
                    It.IsAny<Expression<Func<UserRole, bool>>>(),
                    It.IsAny<string>()))
                .ReturnsAsync(userRoles);

            // Mock JWT token generation
            _jwtTokenGeneratorMock.Setup(jtg => jtg.GenerateJwtToken(
                    user.UserId,
                    It.IsAny<List<string>>()))
                .Returns(newAccessToken);

            // Mock Refresh Token generation
            _jwtTokenGeneratorMock.Setup(jtg => jtg.GenerateRefreshToken(
                    user.UserId,
                    It.IsAny<string>()))
                .Returns(newRefreshToken);

            // Mock Delete for RefreshToken repository
            _refreshTokenRepositoryMock.Setup(repo => repo.Delete(existingRefreshToken))
                .Verifiable();

            // Mock AddAsync for RefreshToken repository
            _refreshTokenRepositoryMock.Setup(repo => repo.AddAsync(newRefreshToken))
                .Returns(Task.CompletedTask);

            // Mock CompleteAsync to return 1 when called
            _unitOfWorkMock.Setup(uow => uow.CompleteAsync()).ReturnsAsync(1);

            // Mock DateTimeProvider
            _mockDateTimeProvider.Setup(dp => dp.UtcNow).Returns(DateTime.UtcNow);

            // Act
            var result = await _authService.RefreshTokenAsync(existingRefreshToken.Token, "127.0.0.1");

            // Assert
            Assert.True(result.Success, $"Refresh token failed with message: {result.Message}");
            Assert.Equal("Token refreshed successfully.", result.Message);
            Assert.Equal(newAccessToken, result.Token);
            Assert.Equal(newRefreshToken.Token, result.RefreshToken);

            // Verify that Delete and AddAsync were called once each
            _refreshTokenRepositoryMock.Verify(repo => repo.Delete(existingRefreshToken), Times.Once);
            _refreshTokenRepositoryMock.Verify(repo => repo.AddAsync(newRefreshToken), Times.Once);
            _unitOfWorkMock.Verify(uow => uow.CompleteAsync(), Times.Once);
        }



        [Fact]
        public async Task RefreshTokenAsync_InvalidToken_ReturnsFailure()
        {
            // Arrange
            string invalidToken = "invalid-refresh-token";

            _refreshTokenRepositoryMock.Setup(repo => repo.GetAsync(
                    It.IsAny<Expression<Func<RefreshToken, bool>>>(),
                    It.IsAny<string>()))
                .ReturnsAsync((RefreshToken)null); // No tokens found

            // Act
            var result = await _authService.RefreshTokenAsync(invalidToken, "127.0.0.1");

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Invalid refresh token.", result.Message);
            Assert.Null(result.Token);
            Assert.Null(result.RefreshToken);
        }

        [Fact]
        public async Task RefreshTokenAsync_ExpiredToken_ReturnsFailure()
        {
            // Arrange
            var expiredRefreshToken = new RefreshToken
            {
                Id = 2,
                Token = "expired-refresh-token",
                UserId = 2,
                ExpirationTime = DateTime.UtcNow.AddDays(-1), // Expired
                Created = DateTime.UtcNow.AddDays(-8),
                CreatedByIp = "192.168.1.1"
            };

            _refreshTokenRepositoryMock.Setup(repo => repo.GetAsync(
                    It.IsAny<Expression<Func<RefreshToken, bool>>>(),
                    It.IsAny<string>()))
                .ReturnsAsync(expiredRefreshToken);

            // Act
            var result = await _authService.RefreshTokenAsync(expiredRefreshToken.Token, "192.168.1.1");

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Invalid refresh token.", result.Message);
            Assert.Null(result.Token);
            Assert.Null(result.RefreshToken);
        }

        [Fact]
        public async Task RefreshTokenAsync_TokenWithNonExistentUser_ReturnsFailure()
        {
            // Arrange
            var refreshToken = new RefreshToken
            {
                Id = 3,
                Token = "token-with-no-user",
                UserId = 999, // Non-existent user
                ExpirationTime = DateTime.UtcNow.AddDays(7),
                Created = DateTime.UtcNow,
                CreatedByIp = "10.0.0.1"
            };

            _refreshTokenRepositoryMock.Setup(repo => repo.GetAsync(
                    It.IsAny<Expression<Func<RefreshToken, bool>>>(),
                    It.IsAny<string>()))
                .ReturnsAsync(refreshToken);

            _userRepositoryMock.Setup(repo => repo.GetByIdAsync(refreshToken.UserId))
                .ReturnsAsync((User)null); // User not found

            // Act
            var result = await _authService.RefreshTokenAsync(refreshToken.Token, "10.0.0.1");

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Invalid refresh token.", result.Message);
            Assert.Null(result.Token);
            Assert.Null(result.RefreshToken);
        }
    }
}
