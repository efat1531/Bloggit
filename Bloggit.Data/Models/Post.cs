using System.ComponentModel.DataAnnotations;

public class Post
{
    [Key]
    public int Id { get; set; }
    [Required]
    public string? Title { get; set; }
    [Required]
    public string? Content { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int? AuthorId { get; set; }
}