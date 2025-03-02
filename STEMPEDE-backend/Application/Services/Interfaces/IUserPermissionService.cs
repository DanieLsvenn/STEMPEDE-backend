using Application.DTOs.User;

namespace Application.Services.Interfaces
{
    public interface IUserPermissionService
    {
        Task<IEnumerable<UserPermissionDto>> GetCurrentUserPermissionsAsync(string userName);
    }
}
