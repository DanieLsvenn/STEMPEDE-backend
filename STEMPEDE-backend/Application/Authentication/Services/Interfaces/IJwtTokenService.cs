namespace Application.Authentication.Services.Interfaces
{
    public interface IJwtTokenService
    {
        string GenerateJwtToken(int userId, string userName, List<string> roles, bool isActive);
    }
}
