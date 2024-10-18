using Stemkit.Data;
using Stemkit.DTOs;
using Stemkit.Models;
using Stemkit.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Stemkit.Utils.Interfaces;
using Stemkit.Utils.Implementation;
using System.Data;

namespace Stemkit.Services.Implementation
{
    public class AuthService : IAuthService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            IUnitOfWork unitOfWork,
            IJwtTokenGenerator jwtTokenGenerator,
            IDateTimeProvider dateTimeProvider,
            ILogger<AuthService> logger)
        {
            _unitOfWork = unitOfWork;
            _jwtTokenGenerator = jwtTokenGenerator;
            _dateTimeProvider = dateTimeProvider;
            _logger = logger;
        }

        public async Task<AuthResponse> RegisterAsync(UserRegistrationDto registrationDto)
        {
            _logger.LogInformation("Starting registration process.");

            // Validate inputs
            if (string.IsNullOrWhiteSpace(registrationDto.Email) ||
                string.IsNullOrWhiteSpace(registrationDto.Username) ||
                string.IsNullOrWhiteSpace(registrationDto.Password))
            {
                return new AuthResponse { Success = false, Message = "Invalid registration data." };
            }

            var userExists = await _unitOfWork.GetRepository<User>().AnyAsync(u =>
    EF.Functions.Collate(u.Email, "SQL_Latin1_General_CP1_CI_AS") == registrationDto.Email ||
    EF.Functions.Collate(u.Username, "SQL_Latin1_General_CP1_CI_AS") == registrationDto.Username);

            if (userExists)
            {
                _logger.LogWarning("User already exists with the provided email or username.");
                return new AuthResponse { Success = false, Message = "User already exists." };
            }

            var allowedRoles = new List<string> { "Customer", "Staff" };
            if (string.IsNullOrWhiteSpace(registrationDto.Role) ||
                !allowedRoles.Contains(registrationDto.Role, StringComparer.OrdinalIgnoreCase))
            {
                _logger.LogError("Invalid or missing role provided.");
                return new AuthResponse { Success = false, Message = "Invalid or missing role provided." };
            }

            var role = await _unitOfWork.GetRepository<Role>()
                .GetAsync(r => EF.Functions.Collate(r.RoleName, "SQL_Latin1_General_CP1_CI_AS") == registrationDto.Role);

            if (role == null)
            {
                _logger.LogError("Role not found in the database.");
                return new AuthResponse { Success = false, Message = "Role not found." };
            }

            using (var transaction = await _unitOfWork.BeginTransactionAsync())
            {
                try
                {
                    // Create a new user
                    var user = new User
                    {
                        Username = registrationDto.Username,
                        Email = registrationDto.Email,
                        Password = BCrypt.Net.BCrypt.HashPassword(registrationDto.Password, workFactor: 12),
                        Phone = registrationDto.Phone?.Trim(),
                        Address = registrationDto.Address?.Trim()
                    };

                    await _unitOfWork.GetRepository<User>().AddAsync(user);
                    await _unitOfWork.CompleteAsync();

                    // Assign Role to the User
                    var userRole = new UserRole
                    {
                        UserId = user.UserId,
                        RoleId = role.RoleId
                    };
                    await _unitOfWork.GetRepository<UserRole>().AddAsync(userRole);

                    // Add Customer or Staff Record Based on Roles
                    if (registrationDto.Role.Equals("Customer", StringComparison.OrdinalIgnoreCase))
                    {
                        var customer = new Customer
                        {
                            UserId = user.UserId,
                            RegistrationDate = DateTime.UtcNow,
                            CustomerPoint = 0
                        };
                        await _unitOfWork.GetRepository<Customer>().AddAsync(customer);
                    }

                    else if (registrationDto.Role.Equals("Staff", StringComparison.OrdinalIgnoreCase))
                    {
                        var staff = new Staff
                        {
                            UserId = user.UserId,
                            StaffPoint = 0
                        };
                        await _unitOfWork.GetRepository<Staff>().AddAsync(staff);
                    }

                    // Generate JWT Access Token
                    var accessToken = _jwtTokenGenerator.GenerateJwtToken(user.UserId, new List<string> { "Customer" });

                    // Generate Refresh Token
                    var refreshToken = _jwtTokenGenerator.GenerateRefreshToken(user.UserId, "127.0.0.1"); // Replace with actual IP

                    // Save all changes to the database
                    await _unitOfWork.RefreshTokens.AddAsync(refreshToken);
                    await _unitOfWork.CompleteAsync();

                    // Commit transaction
                    await transaction.CommitAsync();
                    _logger.LogInformation("User registration successful.");
                    return new AuthResponse
                    {
                        Success = true,
                        Message = "Registration successful.",
                        Token = accessToken,
                        RefreshToken = refreshToken.Token
                    };
                }
                catch (SqlException ex)
                {
                    _logger.LogError(ex, "Database connection error.");
                    await transaction.RollbackAsync();
                    return new AuthResponse { Success = false, Message = "A database connection error occurred. Please try again later." };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An unexpected error occurred during registration.");
                    await transaction.RollbackAsync();
                    return new AuthResponse { Success = false, Message = "An unexpected error occurred. Please try again." };
                }
            }
        }

        public async Task<AuthResponse> LoginAsync(UserLoginDto loginDto)
        {
            _logger.LogInformation("User login attempt.");

            // Validate inputs
            if (string.IsNullOrWhiteSpace(loginDto.EmailOrUsername) || string.IsNullOrWhiteSpace(loginDto.Password))
            {
                return new AuthResponse { Success = false, Message = "Invalid login data." };
            }

            var emailOrUsername = loginDto.EmailOrUsername.Trim();

            try
            {
                var user = await _unitOfWork.GetRepository<User>().GetAsync(u =>
            EF.Functions.Collate(u.Email, "SQL_Latin1_General_CP1_CI_AS") == emailOrUsername ||
            EF.Functions.Collate(u.Username, "SQL_Latin1_General_CP1_CI_AS") == emailOrUsername);

                if (user == null)
                {
                    _logger.LogWarning("Invalid credentials provided.");
                    return new AuthResponse { Success = false, Message = "Invalid credentials." };
                }

                if (!BCrypt.Net.BCrypt.Verify(loginDto.Password, user.Password))
                {
                    _logger.LogWarning("Invalid credentials provided.");
                    return new AuthResponse { Success = false, Message = "Invalid credentials." };
                }

                // Retrieve user roles
                var userRoles = await _unitOfWork.GetRepository<UserRole>()
                    .FindAsync(ur => ur.UserId == user.UserId);

                var roleIds = userRoles.Select(ur => ur.RoleId).ToList();

                var roles = await _unitOfWork.GetRepository<Role>()
                    .FindAsync(r => roleIds.Contains(r.RoleId));

                var roleNames = roles.Select(r => r.RoleName).ToList();

                // Generate JWT token
                var accessToken = _jwtTokenGenerator.GenerateJwtToken(user.UserId, roleNames);

                // Generate Refresh Token
                var refreshToken = _jwtTokenGenerator.GenerateRefreshToken(user.UserId, "127.0.0.1"); // Replace with actual IP

                _logger.LogInformation("User login successful for UserID: {UserId}", user.UserId);

                return new AuthResponse
                {
                    Success = true,
                    Token = accessToken,
                    RefreshToken = refreshToken.Token,
                    Message = "Login successful."
                };
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, "Database connection error occurred during login.");
                return new AuthResponse { Success = false, Message = "Login failed due to a database connection issue." };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during login.");
                return new AuthResponse { Success = false, Message = "Login failed. Please try again." };
            }
        }

        public async Task<AuthResponse> LogoutAsync(string refreshToken)
        {
            if (string.IsNullOrEmpty(refreshToken))
            {
                _logger.LogWarning("Refresh token is null or empty during logout.");
                return new AuthResponse { Success = false, Message = "Invalid refresh token." };
            }

            var existingRefreshToken = await _unitOfWork.RefreshTokens.GetByTokenAsync(refreshToken);

            if (existingRefreshToken == null)
            {
                _logger.LogWarning("Refresh token not found during logout: {Token}", refreshToken);
                return new AuthResponse { Success = false, Message = "Invalid refresh token." };
            }

            // Remove the Refresh Token
            _unitOfWork.RefreshTokens.Delete(existingRefreshToken);
            await _unitOfWork.CompleteAsync();

            _logger.LogInformation("User logged out successfully for UserID: {UserId}", existingRefreshToken.UserId);

            return new AuthResponse { Success = true, Message = "Logout successful." };
        }


        public async Task<AuthResponse> RefreshTokenAsync(string token, string ipAddress)
        {
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("Refresh token is null or empty.");
                return new AuthResponse { Success = false, Message = "Invalid refresh token." };
            }

            // Retrieve the refresh token from the database
            var existingRefreshToken = await _unitOfWork.RefreshTokens.GetAsync(
        rt => rt.Token == token,
        includeProperties: ""
    );

            if (existingRefreshToken == null)
            {
                _logger.LogWarning("Refresh token not found: {Token}", token);
                return new AuthResponse { Success = false, Message = "Invalid refresh token." };
            }

            if (existingRefreshToken.Expires < _dateTimeProvider.UtcNow)
            {
                _logger.LogWarning("Refresh token expired: {Token}", token);
                return new AuthResponse { Success = false, Message = "Expired refresh token." };
            }

            // Optionally: Check if the token has been revoked or used (for token rotation)

            if (!existingRefreshToken.UserId.HasValue)
            {
                _logger.LogWarning("Refresh token does not have a valid UserID.");
                return new AuthResponse { Success = false, Message = "Invalid refresh token." };
            }

            var user = await _unitOfWork.GetRepository<User>().GetByIdAsync(existingRefreshToken.UserId.Value);
            if (user == null)
            {
                _logger.LogWarning("User not found for Refresh Token: {Token}", token);
                return new AuthResponse { Success = false, Message = "Invalid refresh token." };
            }

            // Retrieve user roles
            var roles = await GetUserRolesAsync(user.UserId);

            // Generate new Access Token
            var newAccessToken = _jwtTokenGenerator.GenerateJwtToken(user.UserId, roles);

            // Generate new Refresh Token
            var newRefreshToken = _jwtTokenGenerator.GenerateRefreshToken(user.UserId, ipAddress);

            // Remove the old Refresh Token (token rotation)
            _unitOfWork.RefreshTokens.Delete(existingRefreshToken);

            // Add the new Refresh Token
            await _unitOfWork.RefreshTokens.AddAsync(newRefreshToken);

            // Commit the changes
            await _unitOfWork.CompleteAsync();

            _logger.LogInformation("Refresh token successfully refreshed for UserID: {UserId}", user.UserId);

            // Return the new tokens
            return new AuthResponse
            {
                Success = true,
                Message = "Token refreshed successfully.",
                Token = newAccessToken,
                RefreshToken = newRefreshToken.Token
            };
        }

        private async Task<List<string>> GetUserRolesAsync(int userId)
        {
            var userRoles = await _unitOfWork.GetRepository<UserRole>()
                                         .FindAsync(ur => ur.UserId == userId, includeProperties: "Role");
            return userRoles.Select(ur => ur.Role.RoleName).ToList();
        }
    }
}
