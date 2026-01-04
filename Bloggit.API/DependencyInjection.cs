using Bloggit.Business.IRepository;
using Bloggit.Business.Repository;
using Bloggit.Data;
using Bloggit.Data.Configuration;
using Bloggit.Data.IServices;
using Bloggit.Data.Models;
using Bloggit.Data.Services;
using Microsoft.EntityFrameworkCore;
using Asp.Versioning;
using Bloggit.API.Mappings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Bloggit.API
{
    public static class DependencyInjection
    {
        /// <summary>
        /// Extension method to configure all services including DbContext and dependency injection
        /// </summary>
        public static IServiceCollection AddAllServices(this IServiceCollection services, string connectionString, IConfiguration configuration)
        {
            // Add DbContext with SQL Server
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString,
                    b => b.MigrationsAssembly("Bloggit.API")));

            // Configure Identity
            services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                // Password settings
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequiredLength = 7;
                options.Password.RequiredUniqueChars = 1;

                // User settings
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedEmail = false; // Set to true when email service is configured

                // Lockout settings
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.AllowedForNewUsers = true;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

            // Configure JWT Settings
            var jwtSettings = configuration.GetSection("JwtSettings");
            services.Configure<JwtSettings>(jwtSettings);

            var secret = configuration["JwtSettings:Secret"];
            if (string.IsNullOrEmpty(secret))
            {
                throw new InvalidOperationException(
                    "JWT Secret is not configured. Please add 'JwtSettings:Secret' to appsettings.Development.json for development, " +
                    "or use environment variables/Azure Key Vault for production.");
            }

            var key = Encoding.UTF8.GetBytes(secret);

            // Configure JWT Authentication
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.SaveToken = true;
                options.RequireHttpsMetadata = false; // Set to true in production
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidAudience = jwtSettings["Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ClockSkew = TimeSpan.Zero
                };

                // Read JWT from cookie
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var token = context.Request.Cookies["AuthToken"];
                        if (!string.IsNullOrEmpty(token))
                        {
                            context.Token = token;
                        }
                        return Task.CompletedTask;
                    }
                };
            });

            // Add Authorization with Policies
            services.AddAuthorization(options =>
            {
                // Policy for Admin role only
                options.AddPolicy("AdminOnly", policy =>
                    policy.RequireRole("Admin"));

                // Policy for SuperAdmin only (users with SuperAdmin claim)
                options.AddPolicy("SuperAdminOnly", policy =>
                    policy.RequireClaim("SuperAdmin", "true"));

                // Policy for Admin or Post Author
                options.AddPolicy("AdminOrAuthor", policy =>
                    policy.RequireAssertion(context =>
                        context.User.IsInRole("Admin") ||
                        context.User.HasClaim(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)));

                // Policy for authenticated users
                options.AddPolicy("AuthenticatedUser", policy =>
                    policy.RequireAuthenticatedUser());
            });

            // Register Repositories
            services.AddScoped<IPostRepository, PostRepository>();
            services.AddScoped<IUserRepository, UserRepository>();

            // Register Services
            services.AddScoped<IPostService, PostService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<IInputSanitizationService, InputSanitizationService>();

            return services;
        }

        /// <summary>
        ///  Add API-specific services (Controllers, API Versioning, etc.)
        /// </summary>
        public static IServiceCollection AddApiServices(this IServiceCollection services)
        {
            // Add Controllers
            services.AddControllers();

            // Add AutoMapper
            services.AddAutoMapper(typeof(PostMappingProfile).Assembly);

            // Add API Versioning with proper configuration
            services.AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ReportApiVersions = true;
                options.ApiVersionReader = new UrlSegmentApiVersionReader();
            }).AddMvc();

            // Add OpenAPI with proper configuration
            services.AddOpenApi(options =>
            {
                options.AddDocumentTransformer((document, context, cancellationToken) =>
                {
                    document.Info = new()
                    {
                        Title = "Bloggit API",
                        Version = "v1",
                        Description = "A RESTful API for the Bloggit blog application with authentication, authorization, and CRUD operations.",
                        Contact = new()
                        {
                            Name = "Bloggit Support",
                            Email = "support@bloggit.com"
                        }
                    };
                    return Task.CompletedTask;
                });
            });

            return services;
        }
    }
}