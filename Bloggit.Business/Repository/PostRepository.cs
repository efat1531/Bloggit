using Bloggit.Business.IRepository;
using Bloggit.Data.IServices;

namespace Bloggit.Business.Repository
{
    public class PostRepository(IPostService postService) : IPostRepository
    {
        private readonly IPostService _postService = postService;

        public async Task<IEnumerable<Post>> GetPostsAsync()
        {
            return await _postService.GetPostsAsync();
        }

        public async Task<bool> CreatePost(Post newPost)
        {
            return await _postService.CreatePost(newPost);
        }

        public async Task<bool> UpdatePost(Post updatedPost)
        {
            return await _postService.UpdatePost(updatedPost);
        }

        public async Task<bool> DeletePost(int postId)
        {
            return await _postService.DeletePost(postId);
        }

        public async Task<Post?> GetPostById(int postId)
        {
            return await _postService.GetPostById(postId);
        }
    }
}
