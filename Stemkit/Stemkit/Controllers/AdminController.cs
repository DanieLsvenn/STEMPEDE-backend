using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stemkit.DTOs;
using Stemkit.Services.Interfaces;

namespace Stemkit.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Manager")] // Ensure only Managers can access these endpoints
    public class AdminController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<UsersController> _logger;

        public AdminController(IUserService userService, ILogger<UsersController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        /// <summary>
        /// Retrieves a list of all users with non-sensitive information.
        /// </summary>
        /// <returns>An ApiResponse containing the list of users.</returns>
        [HttpGet("/get-all-user")]
        public async Task<IActionResult> GetAllUsers()
        {
            var response = await _userService.GetAllUsersAsync();
            return Ok(response);
        }

        /// <summary>
        /// Bans a user by setting their status to false.
        /// </summary>
        /// <param name="userId">The unique identifier of the user to ban.</param>
        /// <returns>An ApiResponse indicating the outcome of the ban operation.</returns>
        [HttpPost("{userId}/ban")]
        public async Task<IActionResult> BanUser(int userId)
        {
            _logger.LogInformation("Manager {ManagerId} is attempting to ban UserID: {UserId}", User.Identity.Name, userId);

            var result = await _userService.BanUserAsync(userId);
            if (!result.Success)
            {
                return BadRequest(new ApiResponse<string>
                {
                    Success = false,
                    Message = result.Message
                });
            }

            return Ok(new ApiResponse<string>
            {
                Success = true,
                Message = "User has been banned successfully."
            });
        }

        /// <summary>
        /// Unbans a user by setting their status to true.
        /// </summary>
        /// <param name="userId">The unique identifier of the user to unban.</param>
        /// <returns>An ApiResponse indicating the outcome of the operation.</returns>
        [HttpPost("{userId}/unban-user")]
        public async Task<IActionResult> UnbanUser(int userId)
        {
            _logger.LogInformation("Manager {ManagerId} is attempting to unban UserID: {UserId}", User.Identity.Name, userId);

            var result = await _userService.UnbanUserAsync(userId);
            if (!result.Success)
            {
                return BadRequest(new ApiResponse<string>
                {
                    Success = false,
                    Message = result.Message
                });
            }

            return Ok(new ApiResponse<string>
            {
                Success = true,
                Message = "User has been unbanned successfully."
            });
        }
    }
}
