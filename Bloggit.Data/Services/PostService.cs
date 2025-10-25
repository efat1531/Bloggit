using Bloggit.Data.IServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Bloggit.Data.Services
{
    public class PostService(ApplicationDbContext applicationDbContext, ILogger<PostService> logger) : IPostService
    {
        private readonly ApplicationDbContext _context = applicationDbContext;
        private readonly ILogger<PostService> _logger = logger;

        // <summary>
        // This method to get all the post for the api.
        // </summary>
        public async Task<IEnumerable<Post>> GetPostsAsync()
        {
            try
            {
                var posts = await _context.Posts.ToListAsync();
                return posts;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error retrieving posts: {Message}", ex.Message);
                throw; 
            }
        }

        // <summary>
        // This method to create a new post.
        // </summary>
        public async Task<bool> CreatePost(Post newPost)
        {
            try
            {
                await _context.Posts.AddAsync(newPost);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error creating post: {Message}", ex.Message);
                throw; 
            }
        }

        // <summary>
        // This method to update an existing post.
        // </summary>
        public async Task<bool> UpdatePost(Post updatedPost)
        {
            try
            {
                _context.Posts.Update(updatedPost);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error updating post with ID {PostId}: {Message}", updatedPost.Id, ex.Message);
                throw; 
            }
        }

        // <summary>
        // This method to delete a post.
        // </summary>
        public async Task<bool> DeletePost(int postId)
        {
            try
            {
                var post = await _context.Posts.FindAsync(postId);
                if (post != null)
                {
                    _context.Posts.Remove(post);
                    await _context.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error deleting post with ID {PostId}: {Message}", postId, ex.Message);
                throw; 
            }
        }

        // <summary>
        // This method retrives a post by ID.
        // </summary>
        public async Task<Post?> GetPostById(int postId)
        {
            try
            {
                var post = await _context.Posts.FindAsync(postId);
                return post;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error retrieving post with ID {PostId}: {Message}", postId, ex.Message);
                throw; 
            }
        }
    }
}