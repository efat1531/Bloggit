# 🚀 Bloggit Project - Layered Architecture

## Project Structure

```
Bloggit.sln
├── Bloggit.API          - ASP.NET Core Web API (Backend REST API)
├── Bloggit.Business     - Business Logic Layer
├── Bloggit.Data         - Data Access Layer (Repositories, EF Core, Models)
└── Bloggit.Web          - ASP.NET Core MVC (Frontend Web Application)
```

## Architecture Overview

### **4-Tier Layered Architecture:**

```
┌─────────────────────────────────────────────────────────┐
│                     Bloggit.Web                         │
│          (ASP.NET Core MVC - Frontend)                  │
│  • Views (.cshtml)                                      │
│  • Controllers (UI logic)                               │
│  • HttpClient calls to API                              │
└─────────────────────────────────────────────────────────┘
                          ↓ HTTP Requests
┌─────────────────────────────────────────────────────────┐
│                     Bloggit.API                         │
│          (ASP.NET Core Web API - Backend)               │
│  • API Controllers (PostsController, etc.)              │
│  • Authentication & Authorization                       │
│  • Dependency Injection Configuration                   │
│  • Calls Business Layer                                 │
└─────────────────────────────────────────────────────────┘
                          ↓ References
┌─────────────────────────────────────────────────────────┐
│                  Bloggit.Business                       │
│              (Business Logic Layer)                     │
│  • Services (PostService, CommentService)               │
│  • Business Rules & Validation                          │
│  • Authorization Logic                                  │
│  • Calls Data Layer                                     │
└─────────────────────────────────────────────────────────┘
                          ↓ References
┌─────────────────────────────────────────────────────────┐
│                    Bloggit.Data                         │
│              (Data Access Layer)                        │
│  • Models (Post, Comment, ApplicationUser)              │
│  • ApplicationDbContext (EF Core)                       │
│  • Repositories (IPostRepository, etc.)                 │
│  • Database Access                                      │
└─────────────────────────────────────────────────────────┘
                          ↓ EF Core
                  ┌─────────────┐
                  │  SQL Server │
                  └─────────────┘
```

## Project References

- **Bloggit.Web** → (no reference to API, uses HttpClient)
- **Bloggit.API** → references → **Bloggit.Business** + **Bloggit.Data**
- **Bloggit.Business** → references → **Bloggit.Data**
- **Bloggit.Data** → (no dependencies on other projects)

## Layer Responsibilities

### 🎨 **Bloggit.Web (Frontend - MVC)**

- **Purpose:** User interface and presentation logic
- **Technology:** ASP.NET Core MVC with Razor Views
- **Responsibilities:**
  - Render HTML pages using Razor syntax
  - Handle user input via forms
  - Make HTTP requests to Bloggit.API
  - Client-side routing and navigation
  - Display data from API responses
- **Does NOT:** Directly access database or business logic

### 🌐 **Bloggit.API (Backend - REST API)**

- **Purpose:** Expose HTTP endpoints for data operations
- **Technology:** ASP.NET Core Web API
- **Responsibilities:**
  - Define RESTful API endpoints (GET /api/posts, POST /api/posts, etc.)
  - Handle authentication (ASP.NET Core Identity, JWT tokens)
  - Authorize requests ([Authorize] attributes)
  - Call business layer services
  - Return JSON responses
  - Configure dependency injection
  - Run database migrations
- **Example Endpoints:**
  - `GET /api/posts` - Get all posts
  - `GET /api/posts/{id}` - Get post by ID
  - `POST /api/posts` - Create new post
  - `PUT /api/posts/{id}` - Update post
  - `DELETE /api/posts/{id}` - Delete post
  - `POST /api/auth/register` - User registration
  - `POST /api/auth/login` - User login

### 💼 **Bloggit.Business (Business Logic Layer)**

- **Purpose:** Implement business rules and validation
- **Technology:** .NET Class Library
- **Responsibilities:**
  - Validate input data (e.g., post title cannot be empty)
  - Enforce business rules (e.g., users can only edit their own posts)
  - Coordinate between API and Data layers
  - Implement service interfaces (IPostService, ICommentService)
  - Handle exceptions and error logic
- **Example Services:**
  - `PostService`: Create, update, delete posts with validation
  - `CommentService`: Manage comments with business rules

### 💾 **Bloggit.Data (Data Access Layer)**

- **Purpose:** Handle all database operations
- **Technology:** .NET Class Library with Entity Framework Core
- **Responsibilities:**
  - Define data models (Post, Comment, ApplicationUser)
  - Configure Entity Framework DbContext
  - Implement repository pattern (IPostRepository, ICommentRepository)
  - Execute database queries using EF Core
  - Manage entity relationships
  - No business logic - just data access
- **Contains:**
  - Models: Entity classes
  - ApplicationDbContext: EF Core context
  - Repositories: Data access implementations

## Technology Stack

- **Framework:** ASP.NET Core 9.0
- **Language:** C# 12
- **Database:** SQL Server (LocalDB for development)
- **ORM:** Entity Framework Core 9.0
- **Authentication:** ASP.NET Core Identity
- **Frontend:** Razor Views (.cshtml) with C#
- **API:** REST API returning JSON

## Getting Started

### Prerequisites

- .NET 9.0 SDK
- Visual Studio 2022 or VS Code
- SQL Server Express LocalDB

### Setup Steps

1. **Clone/Open the solution**

   ```bash
   cd "h:\Dot Net Project"
   dotnet restore
   ```

2. **Configure connection string** (in Bloggit.API/appsettings.json)

   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=BloggitDb;Trusted_Connection=true;"
   }
   ```

3. **Run migrations** (from Bloggit.API directory)

   ```bash
   dotnet ef migrations add InitialCreate
   dotnet ef database update
   ```

4. **Run the API** (Backend)

   ```bash
   cd Bloggit.API
   dotnet run
   ```

5. **Run the Web** (Frontend)
   ```bash
   cd Bloggit.Web
   dotnet run
   ```

## Development Workflow

1. **Add new feature:**

   - Define model in `Bloggit.Data/Models`
   - Create repository in `Bloggit.Data/Repositories`
   - Implement service in `Bloggit.Business/Services`
   - Create API controller in `Bloggit.API/Controllers`
   - Create views in `Bloggit.Web/Views`

2. **Update database:**
   ```bash
   cd Bloggit.API
   dotnet ef migrations add <MigrationName>
   dotnet ef database update
   ```

## Benefits of This Architecture

✅ **Separation of Concerns** - Each layer has a specific responsibility
✅ **Testability** - Easy to unit test each layer independently  
✅ **Maintainability** - Changes in one layer don't affect others
✅ **Scalability** - API can be deployed separately from Web frontend
✅ **Reusability** - API can be consumed by multiple frontends (Web, Mobile, etc.)
✅ **Security** - Business logic is protected in the backend, not exposed to client

## Next Steps

- [ ] Create models in Data layer
- [ ] Implement repositories
- [ ] Create business services
- [ ] Build API controllers
- [ ] Set up authentication
- [ ] Create database migrations
- [ ] Build Web frontend views
- [ ] Test end-to-end functionality

---

**Last Updated:** October 12, 2025
