using Stemkit.Models;

namespace Stemkit.Auth.Services.Interfaces
{
    public interface IJwtTokenService
    {
        string GenerateJwtToken(int userId, List<string> roles, bool isActive);
    }
}
