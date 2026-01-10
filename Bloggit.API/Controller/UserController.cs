using Asp.Versioning;
using AutoMapper;
using Bloggit.Business.IRepository;
using Bloggit.Data.Models;
using Bloggit.Models.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Bloggit.API.Controller;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/[controller]")]
public class UserController(
    UserManager<ApplicationUser> userManager,
    IUserRepository userRepository,
    IMapper mapper,
    ILogger<UserController> logger) : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IMapper _mapper = mapper;
    private readonly ILogger<UserController> _logger = logger;

    /// <summary>
    /// Get all users with their roles (Admin only)
    /// </summary>
    /// <param name="role">Optional role filter: 'Admin', 'User', or empty for all users</param>
    [HttpGet]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetUsersAsync([FromQuery] string? role = null)
    {
        _logger.LogInformation("Fetching users with role filter: {Role}", role ?? "All");

        var usersWithRoles = new List<UserWithRolesResponse>();

        // Optimized path when a role filter is specified: avoid per-user GetRolesAsync
        if (!string.IsNullOrWhiteSpace(role))
        {
            var usersInRole = await _userManager.GetUsersInRoleAsync(role);

            foreach (var user in usersInRole)
            {
                var userDto = _mapper.Map<UserWithRolesResponse>(user);
                // We already know the user is in the requested role; avoid extra role lookups.
                userDto.Roles = new List<string> { role };
                usersWithRoles.Add(userDto);
            }
        }
        else
        {
            var users = await _userRepository.GetAllUsersAsync();

            foreach (var user in users)
            {
                var userRoles = await _userManager.GetRolesAsync(user);

                var userDto = _mapper.Map<UserWithRolesResponse>(user);
                userDto.Roles = userRoles;
                usersWithRoles.Add(userDto);
            }
        }

        _logger.LogInformation("Found {Count} users matching filter: {Role}", usersWithRoles.Count, role ?? "All");
        return Ok(usersWithRoles);
    }

    /// <summary>
    /// Promote a user to Admin role (SuperAdmin only)
    /// </summary>
    [HttpPost("promote")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> PromoteUserAsync([FromBody] ManageUserRoleRequest request)
    {
        _logger.LogInformation("Attempting to promote user {UserId} to Admin", request.UserId);

        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user == null)
        {
            _logger.LogWarning("User {UserId} not found", request.UserId);
            return NotFound(new { message = "User not found" });
        }

        // Check if user is already an Admin
        var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
        if (isAdmin)
        {
            _logger.LogWarning("User {UserId} is already an Admin", request.UserId);
            return BadRequest(new { message = "User is already an Admin" });
        }

        // Add user to Admin role
        var result = await _userManager.AddToRoleAsync(user, "Admin");
        if (!result.Succeeded)
        {
            _logger.LogError("Failed to promote user {UserId} to Admin: {Errors}", request.UserId, string.Join(", ", result.Errors.Select(e => e.Description)));
            return StatusCode(500, new { message = "Failed to promote user", errors = result.Errors.Select(e => e.Description) });
        }

        _logger.LogInformation("User {UserId} promoted to Admin successfully", request.UserId);
        return Ok(new { message = $"User {user.UserName} promoted to Admin successfully" });
    }

    /// <summary>
    /// Demote an Admin user to regular User role (SuperAdmin only)
    /// </summary>
    [HttpPost("demote")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> DemoteUserAsync([FromBody] ManageUserRoleRequest request)
    {
        _logger.LogInformation("Attempting to demote user {UserId} from Admin", request.UserId);

        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user == null)
        {
            _logger.LogWarning("User {UserId} not found", request.UserId);
            return NotFound(new { message = "User not found" });
        }

        // Check if user is an Admin
        var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
        if (!isAdmin)
        {
            _logger.LogWarning("User {UserId} is not an Admin", request.UserId);
            return BadRequest(new { message = "User is not an Admin" });
        }

        // Check if user is a SuperAdmin - SuperAdmins cannot be demoted
        var isSuperAdmin = (await _userManager.GetClaimsAsync(user))
            .Any(c => c.Type == "SuperAdmin" && c.Value == "true");
        if (isSuperAdmin)
        {
            _logger.LogWarning("Cannot demote SuperAdmin user {UserId}", request.UserId);
            return BadRequest(new { message = "SuperAdmin users cannot be demoted" });
        }

        // Remove user from Admin role
        var result = await _userManager.RemoveFromRoleAsync(user, "Admin");
        if (!result.Succeeded)
        {
            _logger.LogError("Failed to demote user {UserId} from Admin: {Errors}", request.UserId, string.Join(", ", result.Errors.Select(e => e.Description)));
            return StatusCode(500, new { message = "Failed to demote user", errors = result.Errors.Select(e => e.Description) });
        }

        _logger.LogInformation("User {UserId} demoted from Admin successfully", request.UserId);
        return Ok(new { message = $"User {user.UserName} demoted from Admin successfully" });
    }

    /// <summary>
    /// Assign SuperAdmin claim to a user (SuperAdmin only)
    /// This endpoint can only be called by existing SuperAdmins
    /// </summary>
    [HttpPost("assign-superadmin")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> AssignSuperAdminAsync([FromBody] ManageUserRoleRequest request)
    {
        _logger.LogInformation("Attempting to assign SuperAdmin claim to user {UserId}", request.UserId);

        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user == null)
        {
            _logger.LogWarning("User {UserId} not found", request.UserId);
            return NotFound(new { message = "User not found" });
        }

        // Check if user already has SuperAdmin claim
        var existingClaims = await _userManager.GetClaimsAsync(user);
        var hasSuperAdminClaim = existingClaims.Any(c => c.Type == "SuperAdmin" && c.Value == "true");

        if (hasSuperAdminClaim)
        {
            _logger.LogWarning("User {UserId} already has SuperAdmin claim", request.UserId);
            return BadRequest(new { message = "User already has SuperAdmin privileges" });
        }

        // Add SuperAdmin claim
        var result = await _userManager.AddClaimAsync(user, new Claim("SuperAdmin", "true"));
        if (!result.Succeeded)
        {
            _logger.LogError("Failed to assign SuperAdmin claim to user {UserId}: {Errors}", request.UserId, string.Join(", ", result.Errors.Select(e => e.Description)));
            return StatusCode(500, new { message = "Failed to assign SuperAdmin privileges", errors = result.Errors.Select(e => e.Description) });
        }

        // Ensure user is also an Admin
        var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
        if (!isAdmin)
        {
            var addToRoleResult = await _userManager.AddToRoleAsync(user, "Admin");
            if (!addToRoleResult.Succeeded)
            {
                _logger.LogError("Failed to add user {UserId} to Admin role after assigning SuperAdmin claim: {Errors}", request.UserId, string.Join(", ", addToRoleResult.Errors.Select(e => e.Description)));

                // Attempt to roll back the SuperAdmin claim to avoid inconsistent authorization state
                var rollbackResult = await _userManager.RemoveClaimAsync(user, new Claim("SuperAdmin", "true"));
                if (!rollbackResult.Succeeded)
                {
                    _logger.LogError("Failed to roll back SuperAdmin claim for user {UserId}: {Errors}", request.UserId, string.Join(", ", rollbackResult.Errors.Select(e => e.Description)));
                }

                return StatusCode(500, new
                {
                    message = "Failed to ensure Admin role while assigning SuperAdmin privileges",
                    errors = addToRoleResult.Errors.Select(e => e.Description)
                });
            }
        }

        _logger.LogInformation("SuperAdmin claim assigned to user {UserId} successfully", request.UserId);
        return Ok(new { message = $"SuperAdmin privileges assigned to {user.UserName} successfully" });
    }
}
