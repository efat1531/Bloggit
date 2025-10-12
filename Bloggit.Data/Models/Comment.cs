using System.ComponentModel.DataAnnotations;

public class Comment
{
    public int Id { get; set; }
    [Required]
    public string? Content { get; set; }
    public DateTime CreatedAt { get; set; }
    public int PostId { get; set; }
    public int? CommenterId { get; set; }
}