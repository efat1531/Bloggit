using System.ComponentModel.DataAnnotations;

namespace Bloggit.API.DTOs
{
    /// <summary>
    /// Data Transfer Object for updating an existing Post
    /// </summary>
    public class UpdatePostDto
    {
        [MaxLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
        public string? Title { get; set; }

        public string? Content { get; set; }
    }
}
