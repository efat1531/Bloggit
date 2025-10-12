using Asp.Versioning;
using AutoMapper;
using Bloggit.API.DTOs;
using Bloggit.Business.IRepository;
using Microsoft.AspNetCore.Mvc;

namespace Bloggit.API.Controller
{
    [ApiVersion(1.0)]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    public class PostController(IPostRepository postRepository, IMapper mapper) : ControllerBase
    {
        private readonly IPostRepository _postRepository = postRepository;
        private readonly IMapper _mapper = mapper;
            
        /// <summary>
        /// Get all posts
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var posts = await _postRepository.GetPostsAsync();
            var postDtos = _mapper.Map<List<PostDto>>(posts);
            return Ok(postDtos);
        }

        /// <summary>
        /// Create a new post
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreatePostDto createPostDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var post = _mapper.Map<Post>(createPostDto);
            var success = await _postRepository.CreatePost(post);
            
            if (!success)
                return StatusCode(500, "Failed to create post");

            var postDto = _mapper.Map<PostDto>(post);
            return CreatedAtAction(nameof(Get), new { id = post.Id }, postDto);
        }

        /// <summary>
        /// Update an existing post
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdatePostDto updatePostDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Get posts to find the one to update
            var existingPost = await _postRepository.GetPostById(id);

            if (existingPost == null)
                return NotFound();

            // Map the update DTO to the existing post
            _mapper.Map(updatePostDto, existingPost);
            
            var success = await _postRepository.UpdatePost(existingPost);
            
            if (!success)
                return StatusCode(500, "Failed to update post");

            return NoContent();
        }

        /// <summary>
        /// Delete a post
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _postRepository.DeletePost(id);
            
            if (!success)
                return NotFound();

            return NoContent();
        }

        /// <summary>
        /// Delete a post
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetPostById(int id)
        {
            var post = await _postRepository.GetPostById(id);
            if(post == null)
            {
                return NotFound();
            }
            var postDTO = _mapper.Map<PostDto>(post);
            return Ok(postDTO);
        }

    }
}
