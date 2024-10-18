using Stemkit.Models;

namespace Stemkit.Utils.Interfaces
{
    public interface IJwtTokenGenerator
    {
        string GenerateJwtToken(int userId, List<string> roles);
        RefreshToken GenerateRefreshToken(int userId, string createdByIp);
    }
}
