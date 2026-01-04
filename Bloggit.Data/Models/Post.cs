using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Bloggit.Data.Models;

public class Post
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string? Title { get; set; }

    [Required]
    public string? Content { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    // Foreign key to ApplicationUser
    public string? AuthorId { get; set; }

    // Navigation property
    [JsonIgnore]
    public ApplicationUser? Author { get; set; }
}