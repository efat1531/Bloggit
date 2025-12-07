using System.ComponentModel.DataAnnotations;

namespace Bloggit.API.DTOs;

public class UpdateUserProfileDto
{
    [StringLength(100)]
    public string? FirstName { get; set; }

    [StringLength(100)]
    public string? LastName { get; set; }

    [StringLength(500)]
    public string? Photo { get; set; }

    public DateTime? DateOfBirth { get; set; }
}
