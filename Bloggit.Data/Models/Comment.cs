using System.ComponentModel.DataAnnotations;

namespace Bloggit.Data.Models;

public class Comment
{
    public int Id { get; set; }
    [Required]
    public string? Content { get; set; }
    public DateTime CreatedAt { get; set; }

    // Foreign key to Post
    public int PostId { get; set; }

    // Foreign key to ApplicationUser
    public string? CommenterId { get; set; }

    // Navigation properties
    public Post? Post { get; set; }
    public ApplicationUser? Commenter { get; set; }
}