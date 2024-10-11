using SWP391__StempedeKit.Data;
using SWP391__StempedeKit.DTOs;
using SWP391__StempedeKit.Utils;
using SWP391__StempedeKit.Models;
using SWP391__StempedeKit.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace SWP391__StempedeKit.Services.Implementation
{
    public class AuthService : IAuthService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;
        private readonly ILogger<AuthService> _logger;

        public AuthService(IUnitOfWork unitOfWork, IJwtTokenGenerator jwtTokenGenerator, ILogger<AuthService> logger)
        {
            _unitOfWork = unitOfWork;
            _jwtTokenGenerator = jwtTokenGenerator;
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

            registrationDto.Email = registrationDto.Email.Trim();
            registrationDto.Username = registrationDto.Username.Trim();

            // Check if the user already exists
            var userExists = await _unitOfWork.GetRepository<User>().AnyAsync(u =>
                u.Email.Equals(registrationDto.Email, StringComparison.OrdinalIgnoreCase) ||
                u.Username.Equals(registrationDto.Username, StringComparison.OrdinalIgnoreCase));

            if (userExists)
            {
                _logger.LogWarning("User already exists with the provided email or username.");
                return new AuthResponse { Success = false, Message = "User already exists." };
            }

            // Validate role
            if (string.IsNullOrWhiteSpace(registrationDto.Role))
            {
                _logger.LogError("Role not provided.");
                return new AuthResponse { Success = false, Message = "Role is required." };
            }

            var role = await _unitOfWork.GetRepository<Role>()
                .GetAsync(r => r.RoleName.Equals(registrationDto.Role, StringComparison.OrdinalIgnoreCase));

            if (role == null)
            {
                _logger.LogError("Invalid role provided.");
                return new AuthResponse { Success = false, Message = "Invalid role provided." };
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

                    // Save all changes to the database
                    await _unitOfWork.CompleteAsync();

                    // Commit transaction
                    await transaction.CommitAsync();
                    _logger.LogInformation("User registration successful.");

                    return new AuthResponse { Success = true, Message = "Registration successful." };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred during registration.");
                    await transaction.RollbackAsync();
                    return new AuthResponse { Success = false, Message = "Registration failed. Please try again." };
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

            loginDto.EmailOrUsername = loginDto.EmailOrUsername.Trim();

            try
            {
                var user = await _unitOfWork.GetRepository<User>().GetAsync(u =>
                    u.Email.Equals(loginDto.EmailOrUsername, StringComparison.OrdinalIgnoreCase) ||
                    u.Username.Equals(loginDto.EmailOrUsername, StringComparison.OrdinalIgnoreCase));

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
                var token = _jwtTokenGenerator.GenerateJwtToken(user.UserId, roleNames);

                _logger.LogInformation("User login successful.");

                return new AuthResponse { Success = true, Token = token, Message = "Login successful." };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during login.");
                return new AuthResponse { Success = false, Message = "Login failed. Please try again." };
            }
        }
    }
}
