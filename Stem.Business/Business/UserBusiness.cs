using Stem.Business.Base;
using Stem.Common;
using Stem.Data;
using Stem.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stem.Data.Repository;
using Microsoft.EntityFrameworkCore;
using Stem.Data.DTO;
using Microsoft.AspNetCore.Identity;
namespace Stem.Business.Business
{
    public interface IUserBusiness
    {
        Task<ServiceResponse> RegisterAsync(UserRegisterDTO requestBody, string role);
        Task<ServiceResponse> LoginAsync(UserLoginDTO requestBody);
        Task<ServiceResponse> GetAsync(string userName);
        Task<ServiceResponse> UpdateAsync(string userName, UserUpdateDTO userUpdateDTO);
        Task<ServiceResponse> RefreshTokenAsync(int refreshTokenReq);
       

        /*   Task<bool> ValidateUserAsync(string username, string password);*/
        Task<User> LoginAsync(string username, string password);
    }
    public class UserBusiness : IUserBusiness
    {
        /*private readonly UnitOfWork _unitOfWork;*/
        private readonly UserManager<User> _userManager;
        private readonly ITokenRepository _tokenRepository;
        private readonly STEMKITshopDBContext _dbContext;
        
        /* public UserBusiness(UnitOfWork unitOfWork)
         {
             _unitOfWork = unitOfWork;
         }*/
        public async Task<ServiceResponse> GetAsync(string userName)
        {
            try
            {
                var user = await _userManager.FindByNameAsync(userName);

                if (user == null)
                {
                    return new ServiceResponse()
                        .SetSucceeded(false)
                        .AddDetail("notFound", "Không tìm thấy user ngay lúc này!");
                }

                var userProfileDTO = new UserProfileDTO()
                {
                    UserName = user.Username,
                    FullName = user.FullName,
                    PhoneNumber = user.Phone,
                    Address = user.Address,                  
                };

                return new ServiceResponse()
                    .SetSucceeded(true)
                    .AddDetail("message", "Lấy thông tin tài khoản thành công!")
                    .AddDetail("data", new { userProfileDTO });
            }
            catch
            {
                return new ServiceResponse()
                    .SetSucceeded(false)
                    .AddDetail("message", "Lấy thông tin tài khoản thất bại!")
                    .AddError("outOutService", "Không thể lấy hồ sơ của tài khoản ngay lúc này!");
            }

        }

        public async Task<ServiceResponse> LoginAsync(UserLoginDTO requestBody)
        {
            var user = await _userManager.FindByNameAsync(requestBody.Email!);
            if (user == null || !await _userManager.CheckPasswordAsync(user, requestBody.Password!))
            {
                return new ServiceResponse()
                            .SetSucceeded(false)
                            .AddDetail("message", "Đăng nhập thất bại!")
                            .AddError("invalidCredentials", "Tên đăng nhập hoặc mật khẩu không chính xác!");
            }

            var role = (await _userManager.GetRolesAsync(user))[0];
            var refreshToken = (await _tokenRepository.CreateOrUpdateRefreshTokenAsync(user)).Id;
            var accessToken = _tokenRepository.GenerateJwtToken(user, role);

            return new ServiceResponse()
                        .SetSucceeded(true)
                        .AddDetail("message", "Đăng nhập thành công!")
                        .AddDetail("accessToken", accessToken)
                        .AddDetail("refreshToken", refreshToken);
        }
        public async Task<ServiceResponse> RegisterAsync(UserRegisterDTO requestBody, string role)
        {
            var user = await _userManager.FindByNameAsync(requestBody.Email!);
            if (user != null)
            {
                return new ServiceResponse()
                            .SetSucceeded(false)
                            .AddDetail("message", "Tạo tài khoản thất bại")
                            .AddError("unavailableUsername", "Tên tài khoản đã tồn tại!");
            }
            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                user = new User()
                {
                    Username = requestBody.Email,
                    Email = requestBody.Email
                };
                var identityResult = await _userManager.CreateAsync(user, requestBody.Password!);
                if (!identityResult.Succeeded)
                {
                    await transaction.RollbackAsync();
                    return new ServiceResponse()
                            .SetSucceeded(false)
                            .AddDetail("message", "Tạo tài khoản thất bại")
                            .AddError("unavailableUsername", "Tên tài khoản đã tồn tại!");
                }

                identityResult = await _userManager.AddToRoleAsync(user, role);
                if (!identityResult.Succeeded)
                {
                    await transaction.RollbackAsync();
                    return new ServiceResponse()
                                .SetSucceeded(false)
                                .AddDetail("message", "Tạo tài khoản thất bại!")
                                .AddError("invalidCredentials", "Vai trò yêu cầu không tồn tại!");
                }

                await transaction.CommitAsync();
                return new ServiceResponse()
                            .SetSucceeded(true)
                            .AddDetail("message", "Tạo mới tài khoản thành công!");
            }
            catch
            {
                await transaction.RollbackAsync();
                return new ServiceResponse()
                            .SetSucceeded(false)
                            .AddDetail("message", "Tạo tài khoản thất bại!")
                            .AddError("outOfService", "Không thể tạo tài khoản ngay lúc này!");
            }
        }

        public async Task<ServiceResponse> UpdateAsync(string userName, UserUpdateDTO userUpdateDTO)
        {
            try
            {
                var user = await _userManager.FindByNameAsync(userName);

                if (user == null)
                {
                    return new ServiceResponse()
                        .SetSucceeded(false)
                        .AddDetail("message", "Chỉnh sửa thông tin tài khoản thất bại!")
                        .AddError("notFound", "Không thể chỉnh sửa thông tin tài khoản ngay lúc này!");
                }
                user.FullName = userUpdateDTO.FullName;
                user.Address = userUpdateDTO.Address;
                user.Phone = userUpdateDTO.PhoneNumber;

                await _userManager.UpdateAsync(user);
                return new ServiceResponse()
                    .SetSucceeded(true)
                    .AddDetail("message", "Chỉnh sửa thông tin tài khoản thành công!");

            }
            catch
            {
                return new ServiceResponse()
                    .SetSucceeded(false)
                    .AddDetail("message", "Chỉnh sửa thông tin tài khoản thất bại!")
                    .AddError("invalidCredentials", "Token yêu cầu đã hết hạn hoặc không hợp lệ");
            }
        }

        public async Task<ServiceResponse> RefreshTokenAsync(int refreshTokenReq)
        {
            var userId = await _tokenRepository.GetUserIdByRefreshTokenAsync(refreshTokenReq);
            if (userId == null)
            {
                return new ServiceResponse()
                        .SetSucceeded(false)
                        .AddDetail("message", "Làm mới phiên đăng nhập không thành công!")
                        .AddError("invalidCredentials", "Token yêu cầu đã hết hạn hoặc không hợp lệ!");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return new ServiceResponse()
                        .SetSucceeded(false)
                        .AddDetail("message", "Làm mới phiên đăng nhập không thành công!")
                        .AddError("invalidCredentials", "Token yêu cầu đã hết hạn hoặc không hợp lệ!");
            }

            var roles = await _userManager.GetRolesAsync(user);
            var refreshToken = (await _tokenRepository.CreateOrUpdateRefreshTokenAsync(user)).Id;
            var accessToken = _tokenRepository.GenerateJwtToken(user, roles.FirstOrDefault()!);
            return new ServiceResponse()
                    .SetSucceeded(true)
                    .AddDetail("message", "Làm mới phiên đăng nhập thành công!")
                    .AddDetail("accessToken", accessToken)
                    .AddDetail("refreshToken", refreshToken);
        }
    }

      
}
