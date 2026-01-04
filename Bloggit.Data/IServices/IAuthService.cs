using Bloggit.Data.Models;
using System.Security.Claims;

namespace Bloggit.Data.IServices;

public interface IAuthService
{
    string GenerateJwtToken(ApplicationUser user, IList<string> roles, IList<Claim>? userClaims = null);
    bool IsTokenExpiringSoon(string token);
    DateTime GetTokenExpiration(int expirationHours);
}
