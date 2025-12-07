using Bloggit.Data.Models;

namespace Bloggit.Data.IServices;

public interface IAuthService
{
    string GenerateJwtToken(ApplicationUser user, IList<string> roles);
    bool IsTokenExpiringSoon(string token);
    DateTime GetTokenExpiration(int expirationHours);
}
