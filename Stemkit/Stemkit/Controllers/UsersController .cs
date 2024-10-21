using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stemkit.Data;
using Stemkit.DTOs;
using Stemkit.Models;
using System.Security.Claims;

namespace Stemkit.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]  // Only authenticated users can access these endpoints
    public class UsersController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;

        public UsersController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [HttpGet("profile")]
        public async Task<IActionResult> GetUserProfile()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized(new ApiResponse<string>
                {
                    Success = false,
                    Message = "Invalid user ID."
                });
            }

            var user = await _unitOfWork.GetRepository<User>().GetByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new ApiResponse<string>
                {
                    Success = false,
                    Message = "User not found."
                });
            }

            var userProfile = new UserProfileDto
            {
                UserID = user.UserId,
                FullName = user.FullName,
                Username = user.Username,
                Email = user.Email,
                Phone = user.Phone,
                Address = user.Address,
                Status = user.Status
            };

            return Ok(new ApiResponse<UserProfileDto>
            {
                Success = true,
                Data = userProfile,
                Message = "User profile retrieved successfully."
            });
        }
    }
}
