using Stemkit.DTOs.User;

namespace Stemkit.Services.Interfaces
{
    public interface IUserPermissionService
    {
        Task<IEnumerable<UserPermissionDto>> GetCurrentUserPermissionsAsync(string userName);
    }
}
