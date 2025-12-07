using Bloggit.Business.IRepository;
using Bloggit.Data.IServices;
using Bloggit.Data.Models;

namespace Bloggit.Business.Repository
{
    public class PostRepository(IPostService postService) : IPostRepository
    {
        private readonly IPostService _postService = postService;

        public async Task<IEnumerable<Post>> GetPostsAsync()
        {
            return await _postService.GetPostsAsync();
        }

        public async Task<bool> CreatePostAsync(Post newPost)
        {
            return await _postService.CreatePostAsync(newPost);
        }

        public async Task<bool> UpdatePostAsync(Post updatedPost)
        {
            return await _postService.UpdatePostAsync(updatedPost);
        }

        public async Task<bool> DeletePostAsync(int postId)
        {
            return await _postService.DeletePostAsync(postId);
        }

        public async Task<Post?> GetPostByIdAsync(int postId)
        {
            return await _postService.GetPostByIdAsync(postId);
        }
    }
}
