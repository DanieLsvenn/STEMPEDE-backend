using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Stemkit.Data;
using Stemkit.DTOs.User;
using Stemkit.DTOs;
using Stemkit.Models;
using Stemkit.Services.Interfaces;
using System.Threading.Tasks;


namespace Stemkit.Services.Implementation
{
    public class UserService : IUserService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<UserService> _logger;

        public UserService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<UserService> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<User> GetUserByEmailAsync(string email)
        {
            var userRepository = _unitOfWork.GetRepository<User>();
            return await userRepository.GetAsync(u => u.Email == email);
        }

        public async Task<User> CreateUserAsync(User user)
        {
            if (string.IsNullOrWhiteSpace(user.Email))
                throw new ArgumentException("Email is required.");

            if (string.IsNullOrWhiteSpace(user.Username))
                throw new ArgumentException("Username is required.");

            var userRepository = _unitOfWork.GetRepository<User>();
            await userRepository.AddAsync(user);
            await _unitOfWork.CompleteAsync();
            return user;
        }

        public async Task AssignRoleAsync(int userId, string roleName)
        {
            var roleRepository = _unitOfWork.GetRepository<Role>();
            var userRoleRepository = _unitOfWork.GetRepository<UserRole>();

            var role = await roleRepository.GetAsync(r => r.RoleName == roleName);
            if (role == null)
            {
                throw new Exception("Role not found.");
            }

            var userRole = new UserRole
            {
                UserId = userId,
                RoleId = role.RoleId
            };

            await userRoleRepository.AddAsync(userRole);
            await _unitOfWork.CompleteAsync();
        }

        public async Task<List<string>> GetUserRolesAsync(int userId)
        {
            var userRoleRepository = _unitOfWork.GetRepository<UserRole>();
            var roleRepository = _unitOfWork.GetRepository<Role>();

            var userRoles = await userRoleRepository.FindAsync(ur => ur.UserId == userId);
            var roleIds = userRoles.Select(ur => ur.RoleId).ToList();
            var roles = await roleRepository.FindAsync(r => roleIds.Contains(r.RoleId));

            return roles.Select(r => r.RoleName).ToList();
        }

        public async Task<ApiResponse<IEnumerable<ReadUserDto>>> GetAllUsersAsync()
        {
            var users = await _unitOfWork.GetRepository<User>().Query(includeProperties: "UserRoles.Role")
                .AsNoTracking()
                .ToListAsync();

            var userDtos = users.Select(u => new ReadUserDto
            {
                UserID = u.UserId,
                FullName = u.FullName,
                Username = u.Username,
                Status = u.Status ? "Active" : "Banned",
                IsExternal = u.IsExternal,
                ExternalProvider = u.ExternalProvider,
                Roles = u.UserRoles.Select(ur => ur.Role.RoleName).ToList()
            });

            return new ApiResponse<IEnumerable<ReadUserDto>>
            {
                Success = true,
                Data = userDtos,
                Message = "Users retrieved successfully."
            };
        }

        public async Task<User?> GetUserByIdAsync(int userId)
        {
            return await _unitOfWork.GetRepository<User>().GetByIdAsync(userId);
        }

        public async Task<ApiResponse<string>> BanUserAsync(int userId)
        {
            _logger.LogInformation("Attempting to ban UserID: {UserId}", userId);

            var user = await _unitOfWork.GetRepository<User>().GetByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User with ID {UserId} not found.", userId);
                return ApiResponse<string>.FailureResponse("User not found.");
            }

            if (!user.Status)
            {
                _logger.LogWarning("User with ID {UserId} is already banned.", userId);
                return ApiResponse<string>.FailureResponse("User is already banned.");
            }

            user.Status = false; // Set status to false to ban the user
            _unitOfWork.GetRepository<User>().Update(user);
            await _unitOfWork.CompleteAsync();

            // Revoke all refresh tokens for this user
            var refreshTokens = await _unitOfWork.GetRepository<RefreshToken>()
                .FindAsync(rt => rt.UserId == userId && rt.Revoked == null);

            foreach (var token in refreshTokens)
            {
                token.Revoked = DateTime.UtcNow;
                token.RevokedByIp = "System"; // Or your system's IP
                _unitOfWork.GetRepository<RefreshToken>().Update(token);
            }

            await _unitOfWork.CompleteAsync();

            _logger.LogInformation("User with ID {UserId} has been banned and all refresh tokens revoked.", userId);
            return ApiResponse<string>.SuccessResponse("User has been banned successfully and all tokens revoked.");
        }

        public async Task<ApiResponse<string>> UnbanUserAsync(int userId)
        {
            _logger.LogInformation("Attempting to unban UserID: {UserId}", userId);

            var user = await _unitOfWork.GetRepository<User>().GetByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User with ID {UserId} not found.", userId);
                return ApiResponse<string>.FailureResponse("User not found.");
            }

            if (user.Status)
            {
                _logger.LogWarning("User with ID {UserId} is not banned.", userId);
                return ApiResponse<string>.FailureResponse("User is not banned.");
            }

            user.Status = true; // Set status to true to unban the user
            _unitOfWork.GetRepository<User>().Update(user);
            await _unitOfWork.CompleteAsync();

            _logger.LogInformation("User with ID {UserId} has been unbanned successfully.", userId);
            return ApiResponse<string>.SuccessResponse("User has been unbanned successfully.");
        }
    }
}
