using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Moq;
using Xunit;
using Microsoft.Extensions.Logging;
using SWP391__StempedeKit.Services.Implementation;
using SWP391__StempedeKit.Services.Interfaces;
using SWP391__StempedeKit.Repositories.Interfaces;
using SWP391__StempedeKit.DTOs;
using SWP391__StempedeKit.Models;
using SWP391__StempedeKit.Data;
using Microsoft.EntityFrameworkCore.Storage;
using SWP391__StempedeKit.Utils;
using System.Linq.Expressions;
using Microsoft.Extensions.Configuration;


namespace SWP391__StempedeKit.Tests
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
        private readonly IAuthService _authService;
        private readonly IConfiguration _configuration;

        public AuthServiceTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _userRepositoryMock = new Mock<IGenericRepository<User>>();
            _roleRepositoryMock = new Mock<IGenericRepository<Role>>();
            _userRoleRepositoryMock = new Mock<IGenericRepository<UserRole>>();
            _customerRepositoryMock = new Mock<IGenericRepository<Customer>>();
            _staffRepositoryMock = new Mock<IGenericRepository<Staff>>();
            _loggerMock = new Mock<ILogger<AuthService>>();
            // Create mocks or instances of the constructor parameters
            _jwtTokenGeneratorMock = new Mock<IJwtTokenGenerator>();

            // Setup the unit of work to return the mocked repositories
            _unitOfWorkMock.Setup(uow => uow.GetRepository<User>()).Returns(_userRepositoryMock.Object);
            _unitOfWorkMock.Setup(uow => uow.GetRepository<Role>()).Returns(_roleRepositoryMock.Object);
            _unitOfWorkMock.Setup(uow => uow.GetRepository<UserRole>()).Returns(_userRoleRepositoryMock.Object);
            _unitOfWorkMock.Setup(uow => uow.GetRepository<Customer>()).Returns(_customerRepositoryMock.Object);
            _unitOfWorkMock.Setup(uow => uow.GetRepository<Staff>()).Returns(_staffRepositoryMock.Object);

            _authService = new AuthService(_unitOfWorkMock.Object, _jwtTokenGeneratorMock.Object, _loggerMock.Object);
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
                Role = "Customer"
            };

            // Mock the user does not exist
            _userRepositoryMock.Setup(repo => repo.AnyAsync(It.IsAny<Expression<Func<User, bool>>>()))
                .ReturnsAsync(false);

            // Mock role exists
            var role = new Role { RoleId = 1, RoleName = "Customer" };
            _roleRepositoryMock.Setup(repo => repo.GetAsync(It.IsAny<Expression<Func<Role, bool>>>()))
                .ReturnsAsync(role);

            // Mock adding user and saving changes
            _userRepositoryMock.Setup(repo => repo.AddAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(uow => uow.CompleteAsync()).ReturnsAsync(1);

            // Mock transaction
            var transactionMock = new Mock<IDbContextTransaction>();
            transactionMock.Setup(t => t.CommitAsync(It.IsAny<System.Threading.CancellationToken>())).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(uow => uow.BeginTransactionAsync()).ReturnsAsync(transactionMock.Object);

            // Act
            var result = await _authService.RegisterAsync(registrationDto);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Registration successful.", result.Message);
            _userRepositoryMock.Verify(repo => repo.AddAsync(It.IsAny<User>()), Times.Once);
            _unitOfWorkMock.Verify(uow => uow.CompleteAsync(), Times.Exactly(2)); // After adding user and after adding customer
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
            _roleRepositoryMock.Setup(repo => repo.GetAsync(It.IsAny<Expression<Func<Role, bool>>>()))
                .ReturnsAsync(role);

            // Act
            var result = await _authService.RegisterAsync(registrationDto);

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
            // Updated code
            _userRepositoryMock.Setup(repo => repo.AnyAsync(It.IsAny<Expression<Func<User, bool>>>()))
                .ReturnsAsync(false);


            // Mock role does not exist
            _roleRepositoryMock.Setup(repo => repo.GetAsync(It.IsAny<Expression<Func<Role, bool>>>()))
                .ReturnsAsync((Role)null);

            // Act
            var result = await _authService.RegisterAsync(registrationDto);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Invalid role provided.", result.Message);
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

            // Act
            var result = await _authService.RegisterAsync(registrationDto);

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
            _roleRepositoryMock.Setup(repo => repo.GetAsync(It.IsAny<Expression<Func<Role, bool>>>()))
                .ReturnsAsync(role);

            // Mock adding user throws exception
            _userRepositoryMock.Setup(repo => repo.AddAsync(It.IsAny<User>()))
                .ThrowsAsync(new Exception("Database error"));

            // Mock transaction
            var transactionMock = new Mock<IDbContextTransaction>();
            transactionMock.Setup(t => t.RollbackAsync(It.IsAny<System.Threading.CancellationToken>())).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(uow => uow.BeginTransactionAsync()).ReturnsAsync(transactionMock.Object);

            // Act
            var result = await _authService.RegisterAsync(registrationDto);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Registration failed. Please try again.", result.Message);
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

            // Hash the password
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword("Test@123", workFactor: 12);

            // Mock the user exists with the correct password
            var user = new User
            {
                UserId = 1,
                Username = "testuser",
                Email = "testuser@example.com",
                Password = hashedPassword
            };

            _userRepositoryMock.Setup(repo => repo.GetAsync(It.IsAny<Expression<Func<User, bool>>>()))
                .ReturnsAsync(user);

            // Mock user roles
            var userRoles = new List<UserRole>
    {
        new UserRole { UserRoleId = 1, UserId = 1, RoleId = 1 }
    };
            _userRoleRepositoryMock.Setup(repo => repo.FindAsync(It.IsAny<Expression<Func<UserRole, bool>>>()))
                .ReturnsAsync(userRoles);

            // Mock roles
            var roles = new List<Role>
    {
        new Role { RoleId = 1, RoleName = "Customer" }
    };
            _roleRepositoryMock.Setup(repo => repo.FindAsync(It.IsAny<Expression<Func<Role, bool>>>()))
                .ReturnsAsync(roles);

            // Mock JWT token generation
            _jwtTokenGeneratorMock.Setup(generator => generator.GenerateJwtToken(It.IsAny<int>(), It.IsAny<List<string>>()))
                .Returns("MockedJwtToken");

            // Act
            var result = await _authService.LoginAsync(loginDto);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Login successful.", result.Message);
            Assert.Equal("MockedJwtToken", result.Token);
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
            _jwtTokenGeneratorMock.Setup(generator => generator.GenerateJwtToken(It.IsAny<int>(), It.IsAny<List<string>>()))
                .Returns("MockedJwtToken");

            // Act
            var result = await _authService.LoginAsync(loginDto);

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

            _userRepositoryMock.Setup(repo => repo.GetAsync(It.IsAny<Expression<Func<User, bool>>>()))
                .ReturnsAsync(user);

            // Act
            var result = await _authService.LoginAsync(loginDto);

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

            // Act
            var result = await _authService.LoginAsync(loginDto);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Invalid login data.", result.Message);
            Assert.Null(result.Token);
            _userRepositoryMock.Verify(repo => repo.GetAsync(It.IsAny<Expression<Func<User, bool>>>()), Times.Never);
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
            _userRepositoryMock.Setup(repo => repo.GetAsync(It.IsAny<Expression<Func<User, bool>>>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _authService.LoginAsync(loginDto);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Login failed. Please try again.", result.Message);
            Assert.Null(result.Token);
        }

    }
}
