namespace Bloggit.Models.User;

public class UserResponse
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string? LastName { get; set; }
    public string? Photo { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public bool EmailConfirmed { get; set; }
}
