using System.ComponentModel.DataAnnotations;

namespace Bloggit.Models.User;

public class RegisterRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(50, MinimumLength = 3)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 7)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [StringLength(100)]
    public string? LastName { get; set; }

    [StringLength(500)]
    public string? Photo { get; set; }

    public DateTime? DateOfBirth { get; set; }
}
