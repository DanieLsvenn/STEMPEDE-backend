namespace Stemkit.Utils.Interfaces
{
    public interface IJwtTokenGenerator
    {
        string GenerateJwtToken(int userId, List<string> roles);
    }
}
