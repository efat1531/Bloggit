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
        public Task<bool> CreatePostAsync(Post newPost);
        public Task<bool> UpdatePostAsync(Post updatedPost);
        public Task<bool> DeletePostAsync(int postId);

        public Task<Post?> GetPostByIdAsync(int postId);


    }
}
