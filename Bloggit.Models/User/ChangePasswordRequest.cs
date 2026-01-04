using System.ComponentModel.DataAnnotations;

namespace Bloggit.Models.User;

public class ChangePasswordRequest
{
    [Required]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 7)]
    public string NewPassword { get; set; } = string.Empty;
}
