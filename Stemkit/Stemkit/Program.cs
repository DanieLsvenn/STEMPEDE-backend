
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
using Stemkit.Configurations;
using Microsoft.OpenApi.Models;
using System.Reflection;
using Stemkit.Middleware;

namespace Stemkit
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // ========================================
            // 1. Configuration and Services Setup
            // ========================================

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

            // Bind DatabaseSettings
            builder.Services.Configure<DatabaseSettings>(builder.Configuration.GetSection("DatabaseSettings"));

            // Add configuration for user secrets
            builder.Configuration.AddUserSecrets<Program>();

            // Retrieve the JWT secret key from User Secrets
            var jwtSecretKey = builder.Configuration["Authentication:Jwt:Secret"];
            if (string.IsNullOrEmpty(jwtSecretKey))
            {
                throw new InvalidOperationException("JWT Secret Key is not configured.");
            }

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
                    ValidAudience = builder.Configuration["Authentication:Jwt:Audience"],
                    RequireExpirationTime = true,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero // Eliminate clock skew
                };
            });

            
            // Register Generic Repository
            builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

            // Register Specific Repositories
            builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

            // Register Unit of Work
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

            // Register services
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
            builder.Services.AddScoped<IRefreshTokenService, RefreshTokenService>();
            builder.Services.AddScoped<IDateTimeProvider, DateTimeProvider>();
            builder.Services.AddScoped<IGoogleTokenValidator, GoogleTokenValidator>();
            builder.Services.AddScoped<IExternalAuthService, ExternalAuthService>();
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<IProductService, ProductService>();
            builder.Services.AddScoped<ILabService, LabService>();
            builder.Services.AddScoped<ISubcategoryService, SubcategoryService>();

            // Register AutoMapper
            builder.Services.AddAutoMapper(typeof(AutoMapperProfile));

            // Add controllers and other services
            builder.Services.AddControllers().AddJsonOptions(options => options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);

            // Configure Swagger with JWT support
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "STEMKITshop API", Version = "v1" });

                // Define the security scheme
                var securityScheme = new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Description = "Enter JWT Bearer token **_only_**",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer", 
                    BearerFormat = "JWT",
                    Reference = new OpenApiReference
                    {
                        Id = JwtBearerDefaults.AuthenticationScheme,
                        Type = ReferenceType.SecurityScheme
                    }
                };

                c.AddSecurityDefinition(securityScheme.Reference.Id, securityScheme);

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    { securityScheme, new string[] { } }
                });

                // Set the comments path for the Swagger JSON and UI.
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                if (File.Exists(xmlPath))
                {
                    c.IncludeXmlComments(xmlPath);
                }
            });

            // Add Logging Services
            builder.Services.AddLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.AddDebug();
                logging.AddEventSourceLogger();
            });

            // ========================================
            // 2. Build the Application
            // ========================================

            var app = builder.Build();

            // ========================================
            // 3. Configure the HTTP Request Pipeline
            // ========================================

            // Use CORS
            app.UseCors("AllowReactApp");

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                // Enable Developer Exception Page in development
                app.UseDeveloperExceptionPage();

                // Enable Swagger for API documentation
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "STEMKITshop API V1");
                    c.RoutePrefix = "swagger"; // Set Swagger UI at the app's root
                });
            }
            else
            {
                // Use default error handler in production
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            // Enforce HTTPS
            app.UseHttpsRedirection();

            // Enable Authentication & Authorization
            app.UseAuthentication();

            // Add Custom Middleware to Validate User Status
            app.UseValidateUserStatus();

            app.UseAuthorization();

            // Map Controllers
            app.MapControllers();

            // Run the application
            app.Run();
        }
    }
}
