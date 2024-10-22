using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Stemkit.Data;
using Stemkit.DTOs;
using Stemkit.Models;
using Stemkit.Utils.Interfaces;
using System.Security.Claims;
using Stemkit.Auth.Services.Interfaces;

namespace Stemkit.Auth.Services.Implementation
{
    public class AuthService : IAuthService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IRefreshTokenService _refreshTokenService;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            IUnitOfWork unitOfWork,
            IRefreshTokenService refreshTokenService,
            IJwtTokenService jwtTokenService,
            ILogger<AuthService> logger)
        {
            _unitOfWork = unitOfWork;
            _refreshTokenService = refreshTokenService;
            _jwtTokenService = jwtTokenService;
            _logger = logger;
        }

        public async Task<AuthResponse> RegisterAsync(UserRegistrationDto registrationDto, string ipAddress)
        {
            _logger.LogInformation("Starting registration process from IP: {IpAddress}", ipAddress);

            // Validate inputs
            if (string.IsNullOrWhiteSpace(registrationDto.Email) ||
                string.IsNullOrWhiteSpace(registrationDto.Username) ||
                string.IsNullOrWhiteSpace(registrationDto.Password))
            {
                return new AuthResponse { Success = false, Message = "Invalid registration data." };
            }

            // Check if user already exists (by Email or Username)
            var userExists = await _unitOfWork.GetRepository<User>().AnyAsync(u =>
                EF.Functions.Collate(u.Email, "SQL_Latin1_General_CP1_CI_AS") == registrationDto.Email ||
                EF.Functions.Collate(u.Username, "SQL_Latin1_General_CP1_CI_AS") == registrationDto.Username);

            if (userExists)
            {
                _logger.LogWarning("User already exists with the provided email or username.");
                return new AuthResponse { Success = false, Message = "User already exists." };
            }

            // Validate role
            var allowedRoles = new List<string> { "Customer", "Staff" };
            if (string.IsNullOrWhiteSpace(registrationDto.Role) ||
                !allowedRoles.Contains(registrationDto.Role, StringComparer.OrdinalIgnoreCase))
            {
                _logger.LogError("Invalid or missing role provided.");
                return new AuthResponse { Success = false, Message = "Invalid or missing role provided." };
            }

            // Retrieve role from the database
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
                        Phone = registrationDto.Phone ?? "N/A",
                        Address = registrationDto.Address ?? "N/A",
                        Status = true,
                        IsExternal = registrationDto.IsExternal,
                        ExternalProvider = registrationDto.IsExternal ? registrationDto.ExternalProvider : null
                    };

                    await _unitOfWork.GetRepository<User>().AddAsync(user);
                    await _unitOfWork.CompleteAsync();

                    // Assign Role to the User via UserRoles
                    var userRole = new UserRole
                    {
                        UserId = user.UserId,
                        RoleId = role.RoleId
                    };
                    await _unitOfWork.GetRepository<UserRole>().AddAsync(userRole);

                    // Save role assignment
                    await _unitOfWork.CompleteAsync();

                    // Generate JWT Access Token
                    var accessToken = _jwtTokenService.GenerateJwtToken(user.UserId, new List<string> { role.RoleName });

                    // Generate Refresh Token
                    var refreshToken = _refreshTokenService.GenerateRefreshToken(user.UserId, ipAddress);

                    // Save the Refresh Token
                    await _refreshTokenService.SaveRefreshTokenAsync(refreshToken);

                    // Commit transaction
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

        public async Task<AuthResponse> LoginAsync(UserLoginDto loginDto, string ipAddress)
        {
            _logger.LogInformation("User login attempt from IP: {IpAddress}", ipAddress);

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
                    .FindAsync(ur => ur.UserId == user.UserId, includeProperties: "Role");

                var roleNames = userRoles.Select(ur => ur.Role.RoleName).ToList();

                // Generate JWT token
                var accessToken = _jwtTokenService.GenerateJwtToken(user.UserId, roleNames);

                // Generate Refresh Token
                var refreshToken = _refreshTokenService.GenerateRefreshToken(user.UserId, ipAddress);

                // Save the Refresh Token
                await _refreshTokenService.SaveRefreshTokenAsync(refreshToken);

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

        public async Task<AuthResponse> LogoutAsync(string refreshToken, string ipAddress)
        {
            _logger.LogInformation("Logout attempt from IP: {IpAddress}", ipAddress);

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

            // Remove the Refresh Token via RefreshTokenService
            await _refreshTokenService.InvalidateRefreshTokenAsync(refreshToken, ipAddress);

            _logger.LogInformation("User logged out successfully for UserID: {UserId}", existingRefreshToken.UserId);

            return new AuthResponse { Success = true, Message = "Logout successful." };
        }

        public async Task<AuthResponse> RefreshTokenAsync(string token, string ipAddress)
        {
            _logger.LogInformation("Refresh token attempt from IP: {IpAddress}", ipAddress);

            if (string.IsNullOrWhiteSpace(token))
            {
                _logger.LogWarning("Refresh token is null or empty.");
                return new AuthResponse { Success = false, Message = "Invalid refresh token." };
            }

            // Delegate the entire refresh process to RefreshTokenService
            var refreshResponse = await _refreshTokenService.RefreshTokensAsync(token, ipAddress);

            return refreshResponse;
        }
    }
}

