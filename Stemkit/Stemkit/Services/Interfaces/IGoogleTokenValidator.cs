using Google.Apis.Auth;

namespace Stemkit.Services.Interfaces
{
    public interface IGoogleTokenValidator
    {
        Task<GoogleJsonWebSignature.Payload> ValidateAsync(string idToken);
    }

}
