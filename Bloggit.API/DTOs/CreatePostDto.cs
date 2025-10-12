using System.ComponentModel.DataAnnotations;

namespace Bloggit.API.DTOs
{
    /// <summary>
    /// Data Transfer Object for creating a new Post
    /// </summary>
    public class CreatePostDto
    {
        [Required(ErrorMessage = "Title is required")]
        [MaxLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
        public string? Title { get; set; }

        [Required(ErrorMessage = "Content is required")]
        public string? Content { get; set; }
        public int? AuthorId { get; set; }
    }
}
