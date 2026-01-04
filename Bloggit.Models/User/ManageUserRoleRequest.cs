using System.ComponentModel.DataAnnotations;

namespace Bloggit.Models.User;

public class ManageUserRoleRequest
{
    [Required(ErrorMessage = "User ID is required")]
    public string UserId { get; set; } = string.Empty;
}
