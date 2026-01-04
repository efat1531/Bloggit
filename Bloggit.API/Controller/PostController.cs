using Asp.Versioning;
using AutoMapper;
using Bloggit.Business.IRepository;
using Bloggit.Data.IServices;
using Bloggit.Data.Models;
using Bloggit.Models.Post;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Bloggit.API.Controller
{
    [ApiVersion(1.0)]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    public class PostController(
        IPostRepository postRepository,
        IMapper mapper,
        ILogger<PostController> logger,
        IInputSanitizationService inputSanitizationService) : ControllerBase
    {
        private readonly IPostRepository _postRepository = postRepository;
        private readonly IMapper _mapper = mapper;
        private readonly ILogger<PostController> _logger = logger;
        private readonly IInputSanitizationService _inputSanitizationService = inputSanitizationService;
            
        /// <summary>
        /// Get all posts
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            try
            {
                var posts = await _postRepository.GetPostsAsync();
                var postResponses = _mapper.Map<List<PostResponse>>(posts);
                return Ok(postResponses);
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
        public async Task<IActionResult> Create([FromBody] CreatePostRequest request)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid data for creating post");
                return BadRequest(ModelState);
            }

            try
            {
                // Sanitize input to prevent XSS
                _inputSanitizationService.SanitizeObject(request);

                // Map to domain model
                var post = _mapper.Map<Post>(request);

                // Set AuthorId from authenticated user claims
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                post.AuthorId = userId;

                var success = await _postRepository.CreatePostAsync(post);

                if (!success)
                {
                    _logger.LogError("Failed to create post with title: {Title}", request.Title);
                    return StatusCode(500, "Failed to create post");
                }

                var response = _mapper.Map<PostResponse>(post);
                _logger.LogInformation("Post created by user {UserId} with title: {Title}", userId, request.Title);
                return CreatedAtAction(nameof(GetPostById), new { id = post.Id }, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating post with title: {Title}", request.Title);
                return StatusCode(500, "An error occurred while creating the post");
            }
        }

        /// <summary>
        /// Update an existing post
        /// </summary>
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> Update(int id, [FromBody] UpdatePostRequest request)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for updating post ID: {PostId}", id);
                return BadRequest(ModelState);
            }

            try
            {
                // Sanitize input
                _inputSanitizationService.SanitizeObject(request);

                var existingPost = await _postRepository.GetPostByIdAsync(id);

                if (existingPost == null)
                {
                    _logger.LogWarning("Post with ID: {PostId} not found", id);
                    return NotFound(CreateNotFoundMessage(id));
                }

                // Check if the current user is the author (only author can edit)
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (existingPost.AuthorId != userId)
                {
                    _logger.LogWarning("User {UserId} attempted to update post {PostId} owned by {AuthorId}", userId, id, existingPost.AuthorId);
                    return Forbid();
                }

                // Map updates (null values are ignored)
                _mapper.Map(request, existingPost);
                existingPost.UpdatedAt = System.DateTime.UtcNow;

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

                // Check if the current user is the author OR an Admin
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var isAdmin = User.IsInRole("Admin");
                var isAuthor = existingPost.AuthorId == userId;

                if (!isAdmin && !isAuthor)
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

                var deletedBy = isAdmin ? "Admin" : "Author";
                _logger.LogInformation("Post {PostId} deleted by {Role} user {UserId}", id, deletedBy, userId);
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

                var response = _mapper.Map<PostResponse>(post);
                return Ok(response);
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
