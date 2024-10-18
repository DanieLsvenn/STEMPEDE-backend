using Stemkit.DTOs;
using Stemkit.Models;
using Stemkit.Services.Interfaces;
using Google.Apis.Auth;
using Stemkit.Utils.Interfaces;

namespace Stemkit.Services.Implementation
{
    public class ExternalAuthService : IExternalAuthService
    {
        private readonly IUserService _userService;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;
        private readonly IGoogleTokenValidator _googleTokenValidator;
        private readonly ILogger<ExternalAuthService> _logger;


        public ExternalAuthService(
    IUserService userService,
    IJwtTokenGenerator jwtTokenGenerator,
    IGoogleTokenValidator googleTokenValidator,
    ILogger<ExternalAuthService> logger)
        {
            _userService = userService;
            _jwtTokenGenerator = jwtTokenGenerator;
            _googleTokenValidator = googleTokenValidator;
            _logger = logger;
        }

        public async Task<AuthResponse> GoogleLoginAsync(string idToken)
        {
            try
            {
                var payload = await _googleTokenValidator.ValidateAsync(idToken);
                if (payload == null)
                {
                    return new AuthResponse { Success = false, Message = "Invalid Google token." };
                }

                // Check if user exists
                var user = await _userService.GetUserByEmailAsync(payload.Email);
                if (user == null)
                {
                    // Create a new user
                    user = new User
                    {
                        Email = payload.Email,
                        Username = payload.Name ?? payload.Email,
                        Password = null,
                        Phone = null,
                        Address = null
                    };
                    await _userService.CreateUserAsync(user);

                    // Assign default role
                    await _userService.AssignRoleAsync(user.UserId, "Customer");
                }

                // Retrieve user roles
                var roles = await _userService.GetUserRolesAsync(user.UserId);

                // Generate JWT token
                var token = _jwtTokenGenerator.GenerateJwtToken(user.UserId, roles);

                return new AuthResponse { Success = true, Token = token, Message = "Login successful." };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during Google login.");
                return new AuthResponse { Success = false, Message = "Login failed. Please try again." };
            }
        }
    }
}
