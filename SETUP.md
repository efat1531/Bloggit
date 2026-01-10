# Bloggit Development Setup

This guide will help you set up the Bloggit application for local development.

## Prerequisites

- .NET 9.0 SDK
- SQL Server (or SQL Server Express)
- A code editor (Visual Studio, VS Code, or Rider)

## Initial Setup

### 1. Clone the Repository

```bash
git clone https://github.com/efat1531/Bloggit.git
cd Bloggit
```

### 2. Configure User Secrets

For security, sensitive configuration values (connection strings, JWT secrets) are stored in **user secrets** for development, NOT in `appsettings.json`.

#### Set up User Secrets for Bloggit.API:

```bash
cd Bloggit.API

# Initialize user secrets (if not already done)
dotnet user-secrets init

# Set the database connection string
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Data Source=YOUR_SERVER;Initial Catalog=Bloggit;Integrated Security=True;Persist Security Info=False;Pooling=False;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=True"

# Set the JWT secret (minimum 32 characters)
dotnet user-secrets set "JwtSettings:Secret" "YOUR-SUPER-SECRET-KEY-AT-LEAST-32-CHARACTERS-LONG"
```

**Replace the placeholders:**
- `YOUR_SERVER` - Your SQL Server instance name (e.g., `localhost`, `.\SQLEXPRESS`, or `WIN-FI7GT4BT3EB`)
- `YOUR-SUPER-SECRET-KEY-AT-LEAST-32-CHARACTERS-LONG` - Generate a strong secret key

#### Generate a Secure JWT Secret:

You can generate a secure random secret using PowerShell:

```powershell
# Generate a 64-character random string
-join ((65..90) + (97..122) + (48..57) | Get-Random -Count 64 | ForEach-Object {[char]$_})
```

Or use an online generator: https://generate-random.org/api-key-generator

### 3. Verify User Secrets

To verify your secrets are set correctly:

```bash
cd Bloggit.API
dotnet user-secrets list
```

You should see:
```
ConnectionStrings:DefaultConnection = Data Source=...
JwtSettings:Secret = YOUR-SECRET-KEY
```

### 4. Create Database

```bash
cd Bloggit.API

# Create initial migration (if not exists)
dotnet ef migrations add InitialCreate

# Apply migrations to create the database
dotnet ef database update
```

### 5. Build and Run

```bash
# Build the entire solution
dotnet build

# Run the API
cd Bloggit.API
dotnet run
```

The API will be available at:
- HTTPS: `https://localhost:7291`
- HTTP: `http://localhost:5000`
- Scalar API Docs: `https://localhost:7291/scalar/v1`

## Setting Up the First SuperAdmin

SuperAdmin users have elevated privileges to promote/demote admins. To assign the first SuperAdmin:

### Option 1: Database Seeding (Recommended)

Add this code to `Program.cs` after role seeding (around line 80):

```csharp
// Seed first SuperAdmin user
var superAdminEmail = "your-email@example.com"; // Change this
var superAdminUser = await userManager.FindByEmailAsync(superAdminEmail);

if (superAdminUser != null)
{
    var superAdminClaim = (await userManager.GetClaimsAsync(superAdminUser))
        .FirstOrDefault(c => c.Type == "SuperAdmin" && c.Value == "true");

    if (superAdminClaim == null)
    {
        await userManager.AddClaimAsync(superAdminUser, new System.Security.Claims.Claim("SuperAdmin", "true"));

        if (!await userManager.IsInRoleAsync(superAdminUser, "Admin"))
        {
            await userManager.AddToRoleAsync(superAdminUser, "Admin");
        }

        logger.LogInformation("SuperAdmin privileges assigned to {Email}", superAdminEmail);
    }
}
```

### Option 2: Direct SQL

```sql
-- Find your user ID first
SELECT Id, Email, UserName FROM AspNetUsers WHERE Email = 'your-email@example.com';

-- Replace USER_ID with the Id from above query
DECLARE @UserId NVARCHAR(450) = 'USER_ID';

-- Add SuperAdmin claim
INSERT INTO AspNetUserClaims (UserId, ClaimType, ClaimValue)
VALUES (@UserId, 'SuperAdmin', 'true');

-- Ensure user is in Admin role
INSERT INTO AspNetUserRoles (UserId, RoleId)
SELECT @UserId, Id FROM AspNetRoles WHERE Name = 'Admin'
WHERE NOT EXISTS (
    SELECT 1 FROM AspNetUserRoles WHERE UserId = @UserId AND RoleId = (SELECT Id FROM AspNetRoles WHERE Name = 'Admin')
);
```

## Production Deployment

For production, use environment variables or Azure Key Vault instead of user secrets:

### Environment Variables:

```bash
export ConnectionStrings__DefaultConnection="your-production-connection-string"
export JwtSettings__Secret="your-production-jwt-secret"
```

### Azure App Service:

Set these in Configuration → Application Settings:
- `ConnectionStrings:DefaultConnection`
- `JwtSettings:Secret`

## Troubleshooting

### "Connection string is not configured" error

Make sure you've set up user secrets correctly:
```bash
cd Bloggit.API
dotnet user-secrets list
```

### "JWT Secret is not configured" error

Set the JWT secret in user secrets:
```bash
cd Bloggit.API
dotnet user-secrets set "JwtSettings:Secret" "your-secret-key-at-least-32-characters"
```

### Database connection fails

1. Verify SQL Server is running
2. Check your connection string in user secrets
3. Ensure the database exists (run `dotnet ef database update`)

## Additional Resources

- [User Secrets Documentation](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets)
- [EF Core Migrations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/)
- [ASP.NET Core Configuration](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/)
