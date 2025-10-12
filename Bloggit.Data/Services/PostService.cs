using Bloggit.Data.IServices;

namespace Bloggit.Data.Services
{
    public class PostService(ApplicationDbContext applicationDbContext) : IPostService
    {
        private readonly ApplicationDbContext _context = applicationDbContext;

        // Implement methods defined in IPostService here
        // <summary>
        // This method to get all the post for the api.
        // </summary>
        public async Task<IEnumerable<Post>> GetPostsAsync()
        {
            try
            {
                return await Task.FromResult(_context.Posts.ToList());
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error retrieving posts");
                Console.WriteLine("Error Message: " + ex.Message);
                return Enumerable.Empty<Post>();
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
                Console.WriteLine("Error Creating post");
                Console.WriteLine("Error Message: " + ex.Message);
                return false;
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
                Console.WriteLine("Error Updating post");
                Console.WriteLine("Error Message: " + ex.Message);
                return false;
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
                Console.WriteLine("Error Deleting post");
                Console.WriteLine("Error Message: " + ex.Message);
                return false;
            }
        }

        // <summary>
        // This method to delete a post.
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
                Console.WriteLine("Error Retrieving post by Id");
                Console.WriteLine("Error Message: " + ex.Message);
                return null;
            }
        }
    }
}