using Google.Apis.Auth;

namespace Application.Authentication.Helpers.Interfaces
{
    public interface IGoogleTokenValidator
    {
        Task<GoogleJsonWebSignature.Payload> ValidateAsync(string idToken);
    }

}
