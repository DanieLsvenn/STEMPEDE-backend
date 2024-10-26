namespace Stemkit.Middleware
{
    public static class MiddlewareExtensions
    {
        public static IApplicationBuilder UseValidateUserStatus(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ValidateUserStatusMiddleware>();
        }
    }
}
