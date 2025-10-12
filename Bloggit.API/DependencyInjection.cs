using Bloggit.Business.IRepository;
using Bloggit.Business.Repository;
using Bloggit.Data;
using Bloggit.Data.IServices;
using Bloggit.Data.Services;
using Microsoft.EntityFrameworkCore;
using Asp.Versioning;
using Bloggit.API.Mappings;

namespace Bloggit.API
{
    public static class DependencyInjection
    {
        /// <summary>
        /// Extension method to configure all services including DbContext and dependency injection
        /// </summary>
        public static IServiceCollection AddAllServices(this IServiceCollection services, string connectionString)
        {
            // Add DbContext with SQL Server
            services.AddDbContext<ApplicationDbContext>(options => 
                options.UseSqlServer(connectionString));

            // Register Repositories
            services.AddScoped<IPostRepository, PostRepository>();

            // Register Services
            services.AddScoped<IPostService , PostService>();

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

            // Add OpenAPI
            services.AddOpenApi();

            return services;
        }
    }
}