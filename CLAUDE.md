# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Bloggit is a blog application built with ASP.NET Core 9.0 using a 4-tier layered architecture pattern. The solution consists of:

- **Bloggit.API** - REST API backend (controllers, DTOs, AutoMapper profiles)
- **Bloggit.Business** - Business logic layer (repositories)
- **Bloggit.Data** - Data access layer (EF Core, models, services)
- **Bloggit.Web** - MVC frontend (communicates via HttpClient, not direct reference)

## Build and Run Commands

### Build the solution

```bash
dotnet build
```

### Run the API (backend)

```bash
cd Bloggit.API
dotnet run
```

### Run the Web (frontend)

```bash
cd Bloggit.Web
dotnet run
```

### Run both projects

Open two terminal windows and run the API and Web projects separately.

## Database Commands

All database commands should be run from the `Bloggit.API` directory:

### Create a new migration

```bash
cd Bloggit.API
dotnet ef migrations add <MigrationName>
```

### Apply migrations to database

```bash
cd Bloggit.API
dotnet ef database update
```

### List all migrations

```bash
cd Bloggit.API
dotnet ef migrations list
```

### Remove last migration (if not applied)

```bash
cd Bloggit.API
dotnet ef migrations remove
```

### Drop database

```bash
cd Bloggit.API
dotnet ef database drop
```

## Architecture Patterns

### Layer Responsibilities and Dependencies

```Bash
Bloggit.Web (MVC Frontend)
    ↓ HTTP calls only
Bloggit.API (REST API)
    ↓ references
Bloggit.Business (Repositories)
    ↓ references
Bloggit.Data (EF Core, Models, Services)
    ↓ EF Core
SQL Server
```

**CRITICAL**: Bloggit.Web does NOT reference the API project - it communicates via HTTP only.

### Repository Pattern Location

Repositories are located in **Bloggit.Business**, NOT Bloggit.Data. This is contrary to typical convention but important:

- `Bloggit.Business/IRepository/` - Repository interfaces
- `Bloggit.Business/Repository/` - Repository implementations

### Service Pattern Location

Services are located in **Bloggit.Data**:

- `Bloggit.Data/IServices/` - Service interfaces
- `Bloggit.Data/Services/` - Service implementations

### Adding New Features

When adding a new entity/feature, follow this order:

1. Create model in `Bloggit.Data/Models/`
2. Add DbSet to `ApplicationDbContext.cs`
3. Create repository interface in `Bloggit.Business/IRepository/`
4. Implement repository in `Bloggit.Business/Repository/`
5. Register repository in `Bloggit.API/DependencyInjection.cs` AddAllServices method
6. Create DTOs in `Bloggit.API/DTOs/`:
   - `{Entity}Dto.cs` - for GET operations (includes Id, timestamps)
   - `Create{Entity}Dto.cs` - for POST operations (no Id, no timestamps, use [Required])
   - `Update{Entity}Dto.cs` - for PUT operations (no Id, no CreatedAt, nullable properties)
7. Create AutoMapper profile in `Bloggit.API/Mappings/{Entity}MappingProfile.cs`
8. Create API controller in `Bloggit.API/Controller/`
9. Create migration and update database

### DTO Conventions

- **Read DTOs**: Include all properties including Id and timestamps
- **Create DTOs**: Exclude Id (auto-generated) and timestamps (set automatically), use `[Required]` validation
- **Update DTOs**: Exclude Id and CreatedAt, make properties nullable for partial updates

### AutoMapper Profiles

Create in `Bloggit.API/Mappings/` with pattern `{Entity}MappingProfile.cs`. AutoMapper automatically discovers all Profile classes. Example structure:

```csharp
public class PostMappingProfile : Profile
{
    public PostMappingProfile()
    {
        CreateMap<Post, PostDto>();
        CreateMap<CreatePostDto, Post>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow));
        CreateMap<UpdatePostDto, Post>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
    }
}
```

## Dependency Injection

Service registration happens in `Bloggit.API/DependencyInjection.cs`:

- `AddAllServices()` - Register repositories, services, DbContext
- `AddApiServices()` - Register controllers, AutoMapper, API versioning, OpenAPI

When adding new repositories or services, register them in the appropriate method.

## Logging

The project uses ASP.NET Core's built-in ILogger with structured logging. Inject `ILogger<T>` via constructor:

```csharp
public class PostController(ILogger<PostController> logger) : ControllerBase
{
    private readonly ILogger<PostController> _logger = logger;

    // Use structured logging with placeholders
    _logger.LogInformation("Creating post with title: {Title}", title);
    _logger.LogWarning("Post with ID: {PostId} not found", id);
    _logger.LogError(ex, "Error occurred while processing {Operation}", operation);
}
```

Log levels configured in `appsettings.json`. Development logging is more verbose than production.

## API Versioning

The API uses URL segment versioning:

- Controllers use `[ApiVersion(1.0)]` and `[Route("api/v{version:apiVersion}/[controller]")]`
- Endpoints: `/api/v1/post`, `/api/v1/comment`, etc.

## Connection Strings

Connection strings are stored in user secrets (Development) or environment variables (Production), NOT in appsettings.json. The appsettings.json file does not contain connection strings.

To set up user secrets:

```bash
cd Bloggit.API
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "your-connection-string"
```

## Database Context

The project uses a single `ApplicationDbContext` in `Bloggit.Data/ApplicationDbContext.cs` with SQL Server and EF Core 9.0.

## API Documentation

In Development mode, API documentation is available via Scalar at `/scalar/v1` endpoint.

## Current Features

- Post CRUD operations (PostController, PostRepository, PostService)
- Request logging middleware (RequestLoggingMiddleware)
- Structured logging throughout the application
- AutoMapper for DTO mappings
- API versioning
- OpenAPI/Scalar documentation

## Important Conventions

- Use primary constructors for dependency injection in controllers, repositories, and services
- Controllers return `Task<IActionResult>` with proper HTTP status codes (Ok, Created, NoContent, NotFound, BadRequest, StatusCode)
- All async methods MUST have `Async` suffix (e.g., `GetPostsAsync`, `CreatePostAsync`, `UpdatePostAsync`, `DeletePostAsync`, `GetPostByIdAsync`)
- Always use structured logging with placeholders, not string interpolation:
  - ✅ `_logger.LogInformation("Post {PostId} created", id)`
  - ❌ `_logger.LogInformation($"Post {id} created")`
- DTOs must be used for all API operations - never expose entities directly from controllers
- Repository methods return entities (`Post`, `IEnumerable<Post>`), controllers map to/from DTOs using AutoMapper
- If create a new api method, also create a testcase.
