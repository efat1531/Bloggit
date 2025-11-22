namespace Bloggit.API.DTOs
{
    /// <summary>
    /// Data Transfer Object for Post entity
    /// </summary>
    public class PostDto
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public string? Content { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? AuthorId { get; set; }
    }
}
