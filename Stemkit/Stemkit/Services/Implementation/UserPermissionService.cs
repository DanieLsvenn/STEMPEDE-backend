using Stemkit.Data;
using Stemkit.DTOs.User;
using Stemkit.Models;
using Stemkit.Services.Interfaces;

namespace Stemkit.Services.Implementation
{
    public class UserPermissionService : IUserPermissionService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<UserPermissionService> _logger;

        public UserPermissionService(IUnitOfWork unitOfWork, ILogger<UserPermissionService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<IEnumerable<UserPermissionDto>> GetCurrentUserPermissionsAsync(string userName)
        {
            _logger.LogInformation("Fetching permissions for user: {UserName}", userName);

            // Retrieve the user
            var userRepository = _unitOfWork.GetRepository<User>();
            var users = await userRepository.FindAsync(u => u.Username == userName);
            var user = users.FirstOrDefault();

            if (user == null)
            {
                _logger.LogWarning("User {UserName} not found.", userName);
                throw new ArgumentException("User not found.");
            }

            // Retrieve user permissions including Permission and AssignedByUser
            var userPermissionRepository = _unitOfWork.GetRepository<UserPermission>();
            var userPermissions = await userPermissionRepository.FindAsync(
                up => up.UserId == user.UserId,
                includeProperties: "Permission,AssignedByUser");

            // Map to DTO
            var permissionDtos = userPermissions.Select(up => new UserPermissionDto
            {
                PermissionID = up.Permission.PermissionId,
                PermissionName = up.Permission.PermissionName,
                Description = up.Permission.Description,
                AssignedBy = up.AssignedByNavigation.FullName,
            });

            _logger.LogInformation("Fetched {Count} permissions for user: {UserName}", permissionDtos.Count(), userName);

            return permissionDtos;
        }
    }
}
