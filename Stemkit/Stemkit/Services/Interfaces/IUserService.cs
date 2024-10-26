using Stemkit.DTOs.User;
using Stemkit.DTOs;
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

        /// <summary>
        /// Retrieves a user by their unique ID.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <returns>The user object if found; otherwise, null.</returns>
        Task<User?> GetUserByIdAsync(int userId);

        Task<ApiResponse<IEnumerable<ReadUserDto>>> GetAllUsersAsync();
        /// <summary>
        /// Bans a user by setting their status to false.
        /// </summary>
        /// <param name="userId">The unique identifier of the user to ban.</param>
        /// <returns>An ApiResponse indicating success or failure.</returns>
        Task<ApiResponse<string>> BanUserAsync(int userId);

        /// <summary>
        /// Unbans a user by setting their status to true.
        /// </summary>
        /// <param name="userId">The unique identifier of the user to unban.</param>
        /// <returns>An ApiResponse indicating success or failure.</returns>
        Task<ApiResponse<string>> UnbanUserAsync(int userId);
    }
}
