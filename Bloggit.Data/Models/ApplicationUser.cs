using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Bloggit.Data.Models;

public class ApplicationUser : IdentityUser
{
    [Required]
    [StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [StringLength(100)]
    public string? LastName { get; set; }

    [StringLength(500)]
    public string? Photo { get; set; }

    public DateTime? DateOfBirth { get; set; }

    // Navigation properties
    public ICollection<Post> Posts { get; set; } = new List<Post>();
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
}
