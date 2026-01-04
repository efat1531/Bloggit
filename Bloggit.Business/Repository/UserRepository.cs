using Bloggit.Business.IRepository;
using Bloggit.Data;
using Bloggit.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Bloggit.Business.Repository;

public class UserRepository(ApplicationDbContext context) : IUserRepository
{
    private readonly ApplicationDbContext _context = context;

    public async Task<ApplicationUser?> GetUserByIdAsync(string userId)
    {
        return await _context.Users
            .Include(u => u.Posts)
            .Include(u => u.Comments)
            .FirstOrDefaultAsync(u => u.Id == userId);
    }

    public async Task<bool> UpdateUserProfileAsync(ApplicationUser user)
    {
        try
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<IEnumerable<ApplicationUser>> GetAllUsersAsync()
    {
        return await _context.Users
            .OrderBy(u => u.UserName)
            .ToListAsync();
    }
}
