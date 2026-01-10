using System.ComponentModel.DataAnnotations;

namespace Bloggit.Models.Post;

/// <summary>
/// Unified model for Post operations - used for both Create and Update
/// </summary>
public class PostRequest
{
    [Required(ErrorMessage = "Title is required")]
    [MaxLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
    public string? Title { get; set; }

    [Required(ErrorMessage = "Content is required")]
    public string? Content { get; set; }

    // Note: Id, AuthorId, timestamps are never sent by client
    // They're set server-side for security
}
