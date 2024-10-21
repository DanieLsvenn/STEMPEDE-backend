using Stemkit.Data;
using Stemkit.Models;
using Stemkit.Services.Interfaces;
using System.Threading.Tasks;


namespace Stemkit.Services.Implementation
{
    public class UserService : IUserService
    {
        private readonly IUnitOfWork _unitOfWork;

        public UserService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
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
    }
}
