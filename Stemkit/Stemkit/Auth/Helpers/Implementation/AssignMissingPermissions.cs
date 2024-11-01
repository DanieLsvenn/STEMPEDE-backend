using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stemkit.Auth.Helpers.Interfaces;
using Stemkit.Auth.Services.Implementation;
using Stemkit.Auth.Services.Interfaces;
using Stemkit.Configurations;
using Stemkit.Data;
using Stemkit.Models;

namespace Stemkit.Auth.Helpers.Implementation
{
    public class AssignMissingPermissions : IAssignMissingPermissions
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<AssignMissingPermissions> _logger;
        private readonly DatabaseSettings _dbSettings;

        public AssignMissingPermissions(
            IUnitOfWork unitOfWork,
            ILogger<AssignMissingPermissions> logger,
            IOptions<DatabaseSettings> dbSettings)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _dbSettings = dbSettings.Value;
        }

        public async Task AssignMissingPermissionsAsync(int userId, List<string> roleNames)
        {
            if (roleNames == null || !roleNames.Any())
            {
                _logger.LogWarning("No roles provided for UserID: {UserId}. Skipping permission assignment.", userId);
                return;
            }

            try
            {
                // Step 1: Retrieve corresponding permissions based on roles
                var permissionsToAssign = new List<Permission>();

                foreach (var roleName in roleNames)
                {
                    // Fetch the permission that matches the role name
                    var permission = await _unitOfWork.GetRepository<Permission>()
                        .GetAsync(p => EF.Functions.Collate(p.PermissionName, _dbSettings.Collation) == roleName);

                    if (permission != null)
                    {
                        permissionsToAssign.Add(permission);
                    }
                    else
                    {
                        _logger.LogWarning("No corresponding permission found for role: {RoleName}", roleName);
                    }
                }

                if (!permissionsToAssign.Any())
                {
                    _logger.LogWarning("No permissions to assign based on roles: {RoleNames} for UserID: {UserId}",
                        string.Join(", ", roleNames), userId);
                    return;
                }

                // Step 2: Retrieve existing permissions of the user
                var existingUserPermissions = await _unitOfWork.GetRepository<UserPermission>()
                    .FindAsync(up => up.UserId == userId, includeProperties: "Permission");

                var existingPermissionIds = existingUserPermissions.Select(up => up.PermissionId).ToHashSet();

                // Step 3: Identify missing permissions
                var missingPermissions = permissionsToAssign
                    .Where(p => !existingPermissionIds.Contains(p.PermissionId))
                    .ToList();

                if (!missingPermissions.Any())
                {
                    _logger.LogInformation("UserID: {UserId} already has all corresponding permissions.", userId);
                    return;
                }

                // Step 4: Assign missing permissions
                foreach (var permission in missingPermissions)
                {
                    var userPermission = new UserPermission
                    {
                        UserId = userId,
                        PermissionId = permission.PermissionId,
                        AssignedBy = userId,
                    };

                    await _unitOfWork.GetRepository<UserPermission>().AddAsync(userPermission);
                    _logger.LogInformation("Assigned permission '{PermissionName}' to UserID: {UserId}",
                        permission.PermissionName, userId);
                }

                // Step 5: Save changes
                await _unitOfWork.CompleteAsync();
                _logger.LogInformation("Assigned {Count} missing permissions to UserID: {UserId}",
                    missingPermissions.Count, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while assigning permissions to UserID: {UserId}", userId);
                throw; // Rethrow to let the caller handle the exception
            }
        }
    }
}