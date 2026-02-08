# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Bloggit is a blog application built with ASP.NET Core 9.0 using a layered architecture pattern. The solution consists of 6 projects:

- **Bloggit.API** - REST API backend (controllers, AutoMapper profiles, middleware, authorization)
- **Bloggit.Business** - Business logic layer (repository interfaces and implementations)
- **Bloggit.Data** - Data access layer (EF Core DbContext, models, service interfaces and implementations)
- **Bloggit.Models** - Shared request/response DTOs used by API and Business layers
- **Bloggit.Web** - MVC frontend (communicates with API via HTTP only, no project reference)
- **Bloggit.API.Tests** - Unit tests (xUnit, Moq, FluentAssertions)

## Build, Run, and Test Commands

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

### Run tests

```bash
dotnet test
```

Or target the test project directly:

```bash
dotnet test Bloggit.API.Tests
```

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

```
Bloggit.Web (MVC Frontend)
    ↓ HTTP calls only (no project reference)
Bloggit.API (REST API)
    ↓ references
Bloggit.Business (Repositories)
    ↓ references
Bloggit.Data (EF Core, Models, Services)
    ↓ EF Core
SQL Server

Bloggit.Models (Shared DTOs)
    ↑ referenced by API, Business, and Data
```

**CRITICAL**: Bloggit.Web does NOT reference the API project - it communicates via HTTP only.

### Repository Pattern Location

Repositories are located in **Bloggit.Business**, NOT Bloggit.Data. This is contrary to typical convention but important:

- `Bloggit.Business/IRepository/` - Repository interfaces (IPostRepository, IUserRepository)
- `Bloggit.Business/Repository/` - Repository implementations (PostRepository, UserRepository)

### Service Pattern Location

Services are located in **Bloggit.Data**:

- `Bloggit.Data/IServices/` - Service interfaces (IPostService, IAuthService, IEmailService, IInputSanitizationService)
- `Bloggit.Data/Services/` - Service implementations

### Adding New Features

When adding a new entity/feature, follow this order:

1. Create model in `Bloggit.Data/Models/`
2. Add DbSet to `Bloggit.Data/ApplicationDbContext.cs`
3. Create service interface in `Bloggit.Data/IServices/` (if needed)
4. Implement service in `Bloggit.Data/Services/` (if needed)
5. Create repository interface in `Bloggit.Business/IRepository/`
6. Implement repository in `Bloggit.Business/Repository/`
7. Register repository and service in `Bloggit.API/DependencyInjection.cs` AddAllServices method
8. Create request/response models in `Bloggit.Models/{Entity}/`:
   - `{Entity}Response.cs` - for GET operations (includes Id, timestamps)
   - `Create{Entity}Request.cs` - for POST operations (no Id, no timestamps, use `[Required]`)
   - `Update{Entity}Request.cs` - for PUT operations (no Id, no CreatedAt, nullable properties for partial updates)
9. Create AutoMapper profile in `Bloggit.API/Mappings/{Entity}MappingProfile.cs`
10. Create API controller in `Bloggit.API/Controller/`
11. Create migration and update database
12. Create unit tests in `Bloggit.API.Tests/`

### Request/Response Model Conventions

Models live in **Bloggit.Models/** organized by entity (e.g., `Bloggit.Models/Post/`, `Bloggit.Models/User/`, `Bloggit.Models/Auth/`).

- **Response models** (`{Entity}Response`): Include all properties including Id and timestamps
- **Create request models** (`Create{Entity}Request`): Exclude Id (auto-generated) and timestamps (set automatically), use `[Required]` validation
- **Update request models** (`Update{Entity}Request`): Exclude Id and CreatedAt, make properties nullable for partial updates

### AutoMapper Profiles

Create in `Bloggit.API/Mappings/` with pattern `{Entity}MappingProfile.cs`. AutoMapper automatically discovers all Profile classes. Example structure:

```csharp
public class PostMappingProfile : Profile
{
    public PostMappingProfile()
    {
        CreateMap<Post, PostResponse>();
        CreateMap<CreatePostRequest, Post>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore());
        CreateMap<UpdatePostRequest, Post>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
    }
}
```

## Dependency Injection

Service registration happens in `Bloggit.API/DependencyInjection.cs`:

- `AddAllServices(connectionString, configuration)` - Register DbContext, Identity, JWT authentication, authorization policies, repositories, and services
- `AddApiServices()` - Register controllers, AutoMapper, API versioning, OpenAPI/Scalar

When adding new repositories or services, register them in `AddAllServices()`.

### Currently Registered Services

**Repositories:** IPostRepository, IUserRepository
**Services:** IPostService, IAuthService, IEmailService, IInputSanitizationService

## Authentication and Authorization

### JWT Authentication

- JWT tokens are stored in HttpOnly cookies (Secure, SameSite=Strict)
- Token settings configured in `appsettings.json` under `JwtSettings`
- Secret key stored in user secrets (`JwtSettings:Secret`)
- Default token lifetime: 24 hours, refresh threshold: 16 hours

### Authorization Policies

Three policies are defined in `DependencyInjection.cs`:

- `"AdminOnly"` - Requires Admin role
- `"SuperAdminOnly"` - Requires SuperAdmin claim = "true"
- `"AuthenticatedUser"` - Requires authenticated user

### Resource-Based Authorization

Post ownership checks use `ResourceOwnershipRequirement` and `PostOwnershipAuthorizationHandler` in `Bloggit.API/Authorization/`:

- Admin users can modify any post
- Non-admin users can only modify posts where AuthorId matches their UserId

### Identity Configuration

- ASP.NET Core Identity with ApplicationUser (extends IdentityUser)
- Password: min 7 chars, requires digit, lowercase, and special character
- Account lockout: 5 failed attempts = 5 minutes lockout
- Unique email required
- Roles seeded on startup: "User", "Admin"

## Security

### Input Sanitization

`InputSanitizationService` in `Bloggit.Data/Services/` uses HtmlSanitizer for XSS prevention:

- Allowed HTML tags: p, br, div, strong, em, u, h1-h6, ul, ol, li, a, blockquote, code, pre
- Allowed attributes: href, title
- Apply sanitization in controllers before saving user input

### Security Headers Middleware

`SecurityHeadersMiddleware` adds headers in non-development environments:

- Content-Security-Policy, X-Content-Type-Options, X-Frame-Options, X-XSS-Protection
- Referrer-Policy, Permissions-Policy, HSTS

## API Endpoints

### Auth (`/api/v1/auth`)

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | `/register` | No | Register new user |
| GET | `/confirm-email` | No | Confirm user email |
| POST | `/login` | No | Login (returns JWT) |
| POST | `/logout` | Yes | Logout |
| GET | `/me` | Yes | Get current user profile |
| PUT | `/profile` | Yes | Update user profile |
| POST | `/change-password` | Yes | Change password |
| POST | `/refresh-token` | Yes | Refresh JWT token |

### Posts (`/api/v1/post`)

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/` | No | Get all posts |
| GET | `/{id}` | No | Get post by ID |
| POST | `/` | Yes | Create post |
| PUT | `/{id}` | Yes | Update post (owner/admin) |
| DELETE | `/{id}` | Yes | Delete post (owner/admin) |

### Users (`/api/v1/user`)

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/` | AdminOnly | Get all users (optional role filter) |
| POST | `/promote` | SuperAdminOnly | Promote user to Admin |
| POST | `/demote` | SuperAdminOnly | Demote admin user |
| POST | `/assign-superadmin` | SuperAdminOnly | Assign SuperAdmin claim |

## API Versioning

The API uses URL segment versioning:

- Controllers use `[ApiVersion(1.0)]` and `[Route("api/v{version:apiVersion}/[controller]")]`
- Current endpoints: `/api/v1/auth`, `/api/v1/post`, `/api/v1/user`

## Data Models

### Post (`Bloggit.Data/Models/Post.cs`)

- Id (int, Key), Title (string, Required, MaxLength 200), Content (string, Required)
- CreatedAt (DateTime), UpdatedAt (DateTime)
- AuthorId (string, FK to ApplicationUser)

### Comment (`Bloggit.Data/Models/Comment.cs`)

- Id (int, Key), Content (string, Required)
- CreatedAt (DateTime)
- PostId (int, FK to Post, cascade delete)
- CommenterId (string, FK to ApplicationUser, set null on delete)

### ApplicationUser (`Bloggit.Data/Models/ApplicationUser.cs`)

Extends IdentityUser with:

- FirstName (string, Required, StringLength 100)
- LastName (string, Optional, StringLength 100)
- Photo (string, Optional, StringLength 500)
- DateOfBirth (DateTime?, Optional)
- Navigation properties: Posts, Comments

## Database

- Single `ApplicationDbContext` in `Bloggit.Data/ApplicationDbContext.cs`
- Extends `IdentityDbContext<ApplicationUser>`
- SQL Server with EF Core 9.0
- Connection string via user secrets (Development) or environment variables (Production)
- DbSets: Posts, Comments (Identity tables inherited)

### Delete Behavior

- User deletion: Posts and Comments get AuthorId/CommenterId set to null (SetNull)
- Post deletion: Comments cascade delete

## Connection Strings

Connection strings are stored in user secrets (Development) or environment variables (Production), NOT in appsettings.json.

```bash
cd Bloggit.API
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "your-connection-string"
dotnet user-secrets set "JwtSettings:Secret" "your-jwt-secret-key"
```

## Configuration

### appsettings.json (Bloggit.API)

- `JwtSettings`: Issuer, Audience, ExpirationHours (24), RefreshThresholdHours (16)
- `EmailSettings`: FromEmail, FromName
- Secret/connection string values are in user secrets, not in appsettings.json

## Middleware

Registered in `Bloggit.API/Program.cs`:

1. **RequestLoggingMiddleware** (`Bloggit.API/Middleware/RequestLoggingMiddleware.cs`) - Logs all requests/responses with method, path, status, duration
2. **SecurityHeadersMiddleware** (`Bloggit.API/Middleware/SecurityHeadersMiddleware.cs`) - Adds security headers (non-development only)

Extension methods for registration in `MiddlewareExtensions.cs`.

## Logging

The project uses ASP.NET Core's built-in ILogger with structured logging. Inject `ILogger<T>` via primary constructor:

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

- Always use structured logging with placeholders, not string interpolation
- Log levels configured in `appsettings.json`

## API Documentation

In Development mode, API documentation is available via Scalar at `/scalar/v1` endpoint.

## Testing

### Test Project: `Bloggit.API.Tests`

**Stack:** xUnit, Moq, FluentAssertions, Microsoft.EntityFrameworkCore.InMemory

**Test Structure:**
- `Controllers/AuthControllerTests.cs` - Auth endpoint tests
- `Controllers/PostControllerTests.cs` - Post CRUD tests
- `Controllers/UserControllerTests.cs` - User management tests
- `Services/InputSanitizationServiceTests.cs` - Sanitization logic tests

**Conventions:**
- Use Moq to mock repository/service dependencies
- Use FluentAssertions for readable assertions (`.Should().BeOfType<>()`, `.Should().Be()`)
- Set up authenticated user context with claims for authorized endpoint tests
- Test both success and failure paths (not found, unauthorized, bad request)

### Running Tests

```bash
dotnet test
```

## Current Features

- **Authentication**: User registration, login/logout, email confirmation, JWT tokens with HttpOnly cookies
- **User Management**: Profile updates, password changes, role management (Admin/SuperAdmin)
- **Post CRUD**: Create, read, update, delete posts with ownership enforcement
- **Comments**: Comment model with post association (controller not yet implemented)
- **Input Sanitization**: HTML sanitization for XSS prevention
- **Security Headers**: CSP, X-Frame-Options, HSTS, etc.
- **Request Logging**: Middleware for request/response logging with timing
- **API Documentation**: Scalar/OpenAPI at `/scalar/v1`
- **API Versioning**: URL segment versioning (v1.0)
- **AutoMapper**: DTO mapping profiles for Post and User entities

## Important Conventions

- Use primary constructors for dependency injection in controllers, repositories, and services
- Controllers return `Task<IActionResult>` with proper HTTP status codes (Ok, Created, NoContent, NotFound, BadRequest, StatusCode)
- All async methods MUST have `Async` suffix (e.g., `GetPostsAsync`, `CreatePostAsync`)
- Always use structured logging with placeholders, not string interpolation:
  - `_logger.LogInformation("Post {PostId} created", id)` (correct)
  - `_logger.LogInformation($"Post {id} created")` (wrong)
- Request/response models must be used for all API operations - never expose entities directly from controllers
- Repository methods return entities, controllers map to/from request/response models using AutoMapper
- If you create a new API method, also create corresponding unit tests in `Bloggit.API.Tests/`
- Apply input sanitization on user-provided content before persisting
