using System.ComponentModel.DataAnnotations;

namespace Bloggit.Models.Post;

/// <summary>
/// Request model for updating a post
/// </summary>
public class UpdatePostRequest
{
    [MaxLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
    public string? Title { get; set; }

    public string? Content { get; set; }

    // Note: Null values mean "don't update this field"
}
