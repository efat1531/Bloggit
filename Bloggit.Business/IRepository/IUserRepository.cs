using Bloggit.Data.Models;

namespace Bloggit.Business.IRepository;

public interface IUserRepository
{
    Task<ApplicationUser?> GetUserByIdAsync(string userId);
    Task<bool> UpdateUserProfileAsync(ApplicationUser user);
}
