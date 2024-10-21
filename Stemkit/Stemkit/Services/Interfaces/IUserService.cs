using Stemkit.Models;
using System.Threading.Tasks;

namespace Stemkit.Services.Interfaces
{
    public interface IUserService
    {
        Task<User> GetUserByEmailAsync(string email);
        Task<User> CreateUserAsync(User user);
        Task AssignRoleAsync(int userId, string roleName);
        Task<List<string>> GetUserRolesAsync(int userId);
    }
}
