using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bloggit.Data.IServices
{
    public interface IPostService
    {
        // Define methods for managing blog posts here
        public Task<IEnumerable<Post>> GetPostsAsync();
        public Task<bool> CreatePost(Post newPost);
        public Task<bool> UpdatePost(Post updatedPost);
        public Task<bool> DeletePost(int postId);

        public Task<Post?> GetPostById(int postId);


    }
}
