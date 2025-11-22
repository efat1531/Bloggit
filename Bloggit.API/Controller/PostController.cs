using Asp.Versioning;
using AutoMapper;
using Bloggit.API.DTOs;
using Bloggit.Business.IRepository;
using Bloggit.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Bloggit.API.Controller
{
    [ApiVersion(1.0)]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    public class PostController(IPostRepository postRepository, IMapper mapper, ILogger<PostController> logger) : ControllerBase
    {
        private readonly IPostRepository _postRepository = postRepository;
        private readonly IMapper _mapper = mapper;
        private readonly ILogger<PostController> _logger = logger;
            
        /// <summary>
        /// Get all posts
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            try
            {
                var posts = await _postRepository.GetPostsAsync();
                var postDtos = _mapper.Map<List<PostDto>>(posts);
                return Ok(postDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting all posts");
                return StatusCode(500, "An error occurred while retrieving posts");
            }
        }

        /// <summary>
        /// Create a new post
        /// </summary>
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create([FromBody] CreatePostDto createPostDto)
        {

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid data for creating post");
                return BadRequest(ModelState);
            }

            try
            {
                var post = _mapper.Map<Post>(createPostDto);

                // Set the AuthorId from the authenticated user's claims
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                post.AuthorId = userId;

                var success = await _postRepository.CreatePostAsync(post);

                if (!success)
                {
                    _logger.LogError("Failed to create post with title: {Title}", createPostDto.Title);
                    return StatusCode(500, "Failed to create post");
                }

                var postDto = _mapper.Map<PostDto>(post);
                _logger.LogInformation("Post created by user {UserId} with title: {Title}", userId, createPostDto.Title);
                return CreatedAtAction(nameof(GetPostById), new { id = post.Id }, postDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating post with title: {Title}", createPostDto.Title);
                return StatusCode(500, "An error occurred while creating the post");
            }
        }

        /// <summary>
        /// Update an existing post
        /// </summary>
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> Update(int id, [FromBody] UpdatePostDto updatePostDto)
        {

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for updating post ID: {PostId}", id);
                return BadRequest(ModelState);
            }

            try
            {
                // Get posts to find the one to update
                var existingPost = await _postRepository.GetPostByIdAsync(id);

                if (existingPost == null)
                {
                    _logger.LogWarning("Post with ID: {PostId} not found", id);
                    return NotFound(CreateNotFoundMessage(id));
                }

                // Check if the current user is the author of the post
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (existingPost.AuthorId != userId)
                {
                    _logger.LogWarning("User {UserId} attempted to update post {PostId} owned by {AuthorId}", userId, id, existingPost.AuthorId);
                    return Forbid();
                }

                // Map the update DTO to the existing post
                _mapper.Map(updatePostDto, existingPost);

                var success = await _postRepository.UpdatePostAsync(existingPost);

                if (!success)
                {
                    _logger.LogError("Failed to update post with ID: {PostId}", id);
                    return StatusCode(500, "Failed to update post");
                }

                _logger.LogInformation("Post {PostId} updated by user {UserId}", id, userId);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating post with ID: {PostId}", id);
                return StatusCode(500, "An error occurred while updating the post");
            }
        }

        /// <summary>
        /// Delete a post
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> Delete(int id)
        {

            try
            {
                // Get the post first to check ownership
                var existingPost = await _postRepository.GetPostByIdAsync(id);

                if (existingPost == null)
                {
                    _logger.LogWarning("Post with ID: {PostId} not found for deletion", id);
                    return NotFound(CreateNotFoundMessage(id));
                }

                // Check if the current user is the author of the post
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (existingPost.AuthorId != userId)
                {
                    _logger.LogWarning("User {UserId} attempted to delete post {PostId} owned by {AuthorId}", userId, id, existingPost.AuthorId);
                    return Forbid();
                }

                var success = await _postRepository.DeletePostAsync(id);

                if (!success)
                {
                    _logger.LogError("Failed to delete post with ID: {PostId}", id);
                    return StatusCode(500, "Failed to delete post");
                }

                _logger.LogInformation("Post {PostId} deleted by user {UserId}", id, userId);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting post with ID: {PostId}", id);
                return StatusCode(500, "An error occurred while deleting the post");
            }
        }

        /// <summary>
        /// Get a post by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetPostById(int id)
        {
            try
            {
                var post = await _postRepository.GetPostByIdAsync(id);
                if(post == null)
                {
                    _logger.LogWarning("Post with ID: {PostId} not found", id);
                    return NotFound(CreateNotFoundMessage(id));
                }

                var postDTO = _mapper.Map<PostDto>(post);
                return Ok(postDTO);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting post by ID: {PostId}", id);
                return StatusCode(500, "An error occurred while retrieving the post");
            }
        }
        #region Private Functions
        private static string CreateNotFoundMessage(int? id = null)
        {
            string msg = "The resource you are looking for has been removed or not found in the server.";
            if (id == null)
            {
                return msg;
            }
            msg = $"The post with id = {id} has been removed or not found in the server";
            return msg;
        }
        #endregion

    }
}
