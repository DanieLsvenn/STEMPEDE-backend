
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Stemkit.Data;
using System.Text;
using Stemkit.Repositories.Implementations;
using Stemkit.Repositories.Interfaces;
using Stemkit.Services.Implementation;
using Stemkit.Services.Interfaces;
using System.Text.Json.Serialization;
using Stemkit.Utils.Interfaces;
using Stemkit.Utils.Implementation;
using Stemkit.Auth.Services.Implementation;
using Stemkit.Auth.Services.Interfaces;
using Stemkit.Auth.Helpers.Implementation;
using Stemkit.Auth.Helpers.Interfaces;

namespace Stemkit
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add CORS policy
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowReactApp",
                    policy => policy.AllowAnyOrigin()
                                    .AllowAnyMethod()
                                    .AllowAnyHeader());
            });

            // Conditional registration of SQL Server based on the environment
            builder.Services.AddDbContext<AppDbContext>(options =>
            {
                options.UseSqlServer(builder.Configuration.GetConnectionString("KitStemHubDb"));
            });

            // Add configuration for user secrets
            builder.Configuration.AddUserSecrets<Program>();

            // Retrieve the JWT secret key from User Secrets
            var jwtSecretKey = builder.Configuration["Authentication:Jwt:Secret"];

            // Add JWT Authentication
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtSecretKey)),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidIssuer = builder.Configuration["Authentication:Jwt:Issuer"],
                    ValidAudience = builder.Configuration["Authentication:Jwt:Audience"]
                };
            });

            // Register services
            // Register Generic Repository
            builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

            // Register Specific Repositories
            builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
            builder.Services.AddScoped<IRefreshTokenService, RefreshTokenService>();
            builder.Services.AddScoped<IDateTimeProvider, DateTimeProvider>();
            builder.Services.AddScoped<IGoogleTokenValidator, GoogleTokenValidator>();
            builder.Services.AddScoped<IExternalAuthService, ExternalAuthService>();
            builder.Services.AddScoped<IUserService, UserService>();

            // Add controllers and other services
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddLogging();
            builder.Services.AddControllers().AddJsonOptions(options => options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);

            // Add Logging Services
            builder.Services.AddLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.AddDebug();
                logging.AddEventSourceLogger();
            });

            var app = builder.Build();

            // Use CORS
            app.UseCors("AllowReactApp");

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                // Enable Developer Exception Page in development
                app.UseDeveloperExceptionPage();

                // Enable Swagger for API documentation
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            else
            {
                // Use default error handler in production
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
