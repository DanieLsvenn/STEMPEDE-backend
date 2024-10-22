using Google.Apis.Auth;

namespace Stemkit.Auth.Helpers.Interfaces
{
    public interface IGoogleTokenValidator
    {
        Task<GoogleJsonWebSignature.Payload> ValidateAsync(string idToken);
    }

}
