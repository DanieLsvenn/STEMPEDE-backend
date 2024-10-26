using Stemkit.Services.Implementation;
using Stemkit.Services.Interfaces;
using System.IdentityModel.Tokens.Jwt;

namespace Stemkit.Middleware
{
    public class ValidateUserStatusMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ValidateUserStatusMiddleware> _logger;

        public ValidateUserStatusMiddleware(RequestDelegate next, ILogger<ValidateUserStatusMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, IUserService userService)
        {
            if (context.User.Identity.IsAuthenticated)
            {
                var userIdClaim = context.User.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub);
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
                {
                    var user = await userService.GetUserByIdAsync(userId);
                    if (user == null || !user.Status)
                    {
                        _logger.LogWarning("Access denied. UserID {UserId} is banned.", userId);
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        await context.Response.WriteAsync("Your account has been banned.");
                        return; // Short-circuit the pipeline
                    }
                }
            }

            await _next(context);
        }
    }
}
