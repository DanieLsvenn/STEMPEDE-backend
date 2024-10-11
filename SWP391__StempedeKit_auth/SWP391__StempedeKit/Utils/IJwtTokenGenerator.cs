namespace SWP391__StempedeKit.Utils
{
    public interface IJwtTokenGenerator
    {
        string GenerateJwtToken(int userId, List<string> roles);
    }
}
