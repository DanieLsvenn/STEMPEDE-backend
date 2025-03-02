using Microsoft.Extensions.DependencyInjection;
using Application.Utils.Interfaces;
using Application.Utils.Implementation;
using Application.Services.Implementation;
using Application.Services.Interfaces;
using Application.Authentication.Helpers.Implementation;
using Application.Authentication.Helpers.Interfaces;
using Application.Authentication.Services.Implementation;
using Application.Authentication.Services.Interfaces;

namespace Application
{
    public static class ApplicationDependencyInjection
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddScoped<IAuthenticationService, AuthenticationService>();
            services.AddScoped<IJwtTokenService, JwtTokenService>();
            services.AddScoped<IRefreshTokenService, RefreshTokenService>();
            services.AddScoped<IDateTimeProvider, DateTimeProvider>();
            services.AddScoped<IGoogleTokenValidator, GoogleTokenValidator>();
            services.AddScoped<IExternalAuthService, ExternalAuthService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IProductService, ProductService>();
            services.AddScoped<ILabService, LabService>();
            services.AddScoped<ISubcategoryService, SubcategoryService>();
            services.AddScoped<ICartService, CartService>();
            services.AddScoped<IUserPermissionService, UserPermissionService>();
            services.AddScoped<IAssignMissingPermissions, AssignMissingPermissions>();
            services.AddScoped<IOrderService, OrderService>();

            return services;

        }
    }
}
