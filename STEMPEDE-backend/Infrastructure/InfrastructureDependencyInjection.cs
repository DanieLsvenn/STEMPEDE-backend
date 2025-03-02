using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Infrastructure.Data;
using Domain;
using Microsoft.EntityFrameworkCore;
using Domain.IRepositories;
using Infrastructure.Repositories;
using Infrastructure.PaymentProviders.VnPay;

namespace Infrastructure
{
    public static class InfrastructureDependencyInjection
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration config)
        {
            services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

            services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

            services.AddDbContext<AppDbContext>(opt => opt.UseSqlServer(config.GetConnectionString("KitStemHubDb")));

            services.AddScoped<IVnPayService, VnPayService>();

            return services;

        }
    }
}
