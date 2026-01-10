using System.ComponentModel.DataAnnotations;

namespace Bloggit.Models.Post;

/// <summary>
/// Request model for creating a new post
/// </summary>
public class CreatePostRequest
{
    [Required(ErrorMessage = "Title is required")]
    [MaxLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Content is required")]
    public string Content { get; set; } = string.Empty;

    // Note: AuthorId is set from JWT claims, not from client
}
