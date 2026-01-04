using Asp.Versioning;
using AutoMapper;
using Bloggit.Business.IRepository;
using Bloggit.Data.Configuration;
using Bloggit.Data.IServices;
using Bloggit.Data.Models;
using Bloggit.Models.Auth;
using Bloggit.Models.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace Bloggit.API.Controller;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/[controller]")]
public class AuthController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    IAuthService authService,
    IEmailService emailService,
    IUserRepository userRepository,
    IMapper mapper,
    ILogger<AuthController> logger,
    IOptions<JwtSettings> jwtSettings,
    IInputSanitizationService inputSanitizationService) : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly SignInManager<ApplicationUser> _signInManager = signInManager;
    private readonly IAuthService _authService = authService;
    private readonly IEmailService _emailService = emailService;
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IMapper _mapper = mapper;
    private readonly ILogger<AuthController> _logger = logger;
    private readonly JwtSettings _jwtSettings = jwtSettings.Value;
    private readonly IInputSanitizationService _inputSanitizationService = inputSanitizationService;

    /// <summary>
    /// Register a new user
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> RegisterAsync([FromBody] RegisterRequest registerDto)
    {
        // Sanitize input to prevent XSS attacks
        _inputSanitizationService.SanitizeObject(registerDto);

        _logger.LogInformation("Registration attempt for email: {Email}", registerDto.Email);
        // Check if user already exists
        var existingUser = await _userManager.FindByEmailAsync(registerDto.Email);
        if (existingUser != null)
        {
            _logger.LogWarning("Registration failed: Email {Email} already exists", registerDto.Email);
            return BadRequest(new { message = "Email already registered" });
        }

        var existingUsername = await _userManager.FindByNameAsync(registerDto.Username);
        if (existingUsername != null)
        {
            _logger.LogWarning("Registration failed: Username {Username} already exists", registerDto.Username);
            return BadRequest(new { message = "Username already taken" });
        }

        // Create new user
        var user = _mapper.Map<ApplicationUser>(registerDto);
        var result = await _userManager.CreateAsync(user, registerDto.Password);

        if (!result.Succeeded)
        {
            _logger.LogError("User creation failed for {Email}: {Errors}", registerDto.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
            return BadRequest(new { message = "User creation failed", errors = result.Errors.Select(e => e.Description) });
        }

        // Add user to default "User" role
        await _userManager.AddToRoleAsync(user, "User");

        // TODO: Enable email confirmation when email service is configured
        // For now, auto-confirm the email
        var emailToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        await _userManager.ConfirmEmailAsync(user, emailToken);

        _logger.LogInformation("User {Email} registered successfully (email auto-confirmed).", registerDto.Email);

        return Ok(new { message = "Registration successful. You can now log in." });
    }

    /// <summary>
    /// Confirm user email
    /// </summary>
    [HttpGet("confirm-email")]
    public async Task<IActionResult> ConfirmEmailAsync([FromQuery] string userId, [FromQuery] string token)
    {
        _logger.LogInformation("Email confirmation attempt for userId: {UserId}", userId);

        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
        {
            return BadRequest(new { message = "Invalid confirmation link" });
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("Email confirmation failed: User {UserId} not found", userId);
            return NotFound(new { message = "User not found" });
        }

        var result = await _userManager.ConfirmEmailAsync(user, token);
        if (!result.Succeeded)
        {
            _logger.LogError("Email confirmation failed for {UserId}: {Errors}", userId, string.Join(", ", result.Errors.Select(e => e.Description)));
            return BadRequest(new { message = "Email confirmation failed", errors = result.Errors.Select(e => e.Description) });
        }

        _logger.LogInformation("Email confirmed successfully for user {UserId}", userId);
        return Ok(new { message = "Email confirmed successfully. You can now log in." });
    }

    /// <summary>
    /// Login user
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> LoginAsync([FromBody] LoginRequest loginDto)
    {
        _logger.LogInformation("Login attempt for: {EmailOrUsername}", loginDto.EmailOrUsername);

        // Find user by email or username
        var user = await _userManager.FindByEmailAsync(loginDto.EmailOrUsername)
                   ?? await _userManager.FindByNameAsync(loginDto.EmailOrUsername);

        if (user == null)
        {
            _logger.LogWarning("Login failed: User {EmailOrUsername} not found", loginDto.EmailOrUsername);
            return Unauthorized(new { message = "Invalid email/username or password" });
        }

        // Email confirmation check - disabled until email service is configured
        // if (!await _userManager.IsEmailConfirmedAsync(user))
        // {
        //     _logger.LogWarning("Login failed: Email not confirmed for {Email}", user.Email);
        //     return Unauthorized(new { message = "Please confirm your email before logging in" });
        // }

        // Check password
        var result = await _signInManager.CheckPasswordSignInAsync(user, loginDto.Password, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            if (result.IsLockedOut)
            {
                _logger.LogWarning("Login failed: Account locked out for {Email}", user.Email);
                return Unauthorized(new { message = "Account locked due to multiple failed login attempts" });
            }

            _logger.LogWarning("Login failed: Invalid password for {Email}", user.Email);
            return Unauthorized(new { message = "Invalid email/username or password" });
        }

        // Generate JWT token
        var roles = await _userManager.GetRolesAsync(user);
        var userClaims = await _userManager.GetClaimsAsync(user);
        var token = _authService.GenerateJwtToken(user, roles, userClaims);
        var expiresAt = _authService.GetTokenExpiration(_jwtSettings.ExpirationHours);

        // Set token in HTTP-only cookie
        SetAuthCookie(token, expiresAt);

        var userDto = _mapper.Map<UserResponse>(user);
        var response = new AuthResponse
        {
            Token = token,
            User = userDto,
            ExpiresAt = expiresAt
        };

        _logger.LogInformation("User {Email} logged in successfully", user.Email);
        return Ok(response);
    }

    /// <summary>
    /// Logout user
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public IActionResult Logout()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        _logger.LogInformation("User {UserId} logging out", userId);

        // Clear the auth cookie
        Response.Cookies.Delete("AuthToken");

        _logger.LogInformation("User {UserId} logged out successfully", userId);
        return Ok(new { message = "Logged out successfully" });
    }

    /// <summary>
    /// Get current user profile
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUserAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        _logger.LogInformation("Fetching profile for user {UserId}", userId);

        var user = await _userRepository.GetUserByIdAsync(userId!);
        if (user == null)
        {
            _logger.LogWarning("User {UserId} not found", userId);
            return NotFound(new { message = "User not found" });
        }

        var userDto = _mapper.Map<UserResponse>(user);
        return Ok(userDto);
    }

    /// <summary>
    /// Update user profile
    /// </summary>
    [HttpPut("profile")]
    [Authorize]
    public async Task<IActionResult> UpdateProfileAsync([FromBody] UpdateUserProfileRequest updateDto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        _logger.LogInformation("Updating profile for user {UserId}", userId);

        // Sanitize input to prevent XSS attacks
        _inputSanitizationService.SanitizeObject(updateDto);

        var user = await _userRepository.GetUserByIdAsync(userId!);
        if (user == null)
        {
            _logger.LogWarning("User {UserId} not found", userId);
            return NotFound(new { message = "User not found" });
        }

        // Update user properties
        _mapper.Map(updateDto, user);

        var success = await _userRepository.UpdateUserProfileAsync(user);
        if (!success)
        {
            _logger.LogError("Failed to update profile for user {UserId}", userId);
            return StatusCode(500, new { message = "Failed to update profile" });
        }

        var userDto = _mapper.Map<UserResponse>(user);
        _logger.LogInformation("Profile updated successfully for user {UserId}", userId);
        return Ok(userDto);
    }

    /// <summary>
    /// Change user password
    /// </summary>
    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePasswordAsync([FromBody] ChangePasswordRequest changePasswordDto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        _logger.LogInformation("Password change attempt for user {UserId}", userId);

        var user = await _userManager.FindByIdAsync(userId!);
        if (user == null)
        {
            _logger.LogWarning("User {UserId} not found", userId);
            return NotFound(new { message = "User not found" });
        }

        var result = await _userManager.ChangePasswordAsync(user, changePasswordDto.CurrentPassword, changePasswordDto.NewPassword);
        if (!result.Succeeded)
        {
            _logger.LogError("Password change failed for user {UserId}: {Errors}", userId, string.Join(", ", result.Errors.Select(e => e.Description)));
            return BadRequest(new { message = "Password change failed", errors = result.Errors.Select(e => e.Description) });
        }

        _logger.LogInformation("Password changed successfully for user {UserId}", userId);
        return Ok(new { message = "Password changed successfully" });
    }

    /// <summary>
    /// Refresh JWT token if expiring soon
    /// </summary>
    [HttpPost("refresh-token")]
    [Authorize]
    public async Task<IActionResult> RefreshTokenAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        _logger.LogInformation("Token refresh attempt for user {UserId}", userId);

        var token = Request.Cookies["AuthToken"];
        if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning("Token refresh failed: No token found for user {UserId}", userId);
            return Unauthorized(new { message = "No token found" });
        }

        // Check if token is expiring soon
        if (!_authService.IsTokenExpiringSoon(token))
        {
            _logger.LogInformation("Token refresh not needed for user {UserId}", userId);
            return Ok(new { message = "Token is still valid" });
        }

        // Generate new token
        var user = await _userManager.FindByIdAsync(userId!);
        if (user == null)
        {
            _logger.LogWarning("User {UserId} not found during token refresh", userId);
            return NotFound(new { message = "User not found" });
        }

        var roles = await _userManager.GetRolesAsync(user);
        var userClaims = await _userManager.GetClaimsAsync(user);
        var newToken = _authService.GenerateJwtToken(user, roles, userClaims);
        var expiresAt = _authService.GetTokenExpiration(_jwtSettings.ExpirationHours);

        // Set new token in HTTP-only cookie
        SetAuthCookie(newToken, expiresAt);

        var userDto = _mapper.Map<UserResponse>(user);
        var response = new AuthResponse
        {
            Token = newToken,
            User = userDto,
            ExpiresAt = expiresAt
        };

        _logger.LogInformation("Token refreshed successfully for user {UserId}", userId);
        return Ok(response);
    }

    /// <summary>
    /// Helper method to set auth cookie
    /// </summary>
    private void SetAuthCookie(string token, DateTime expiresAt)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true, // Requires HTTPS
            SameSite = SameSiteMode.Strict,
            Expires = expiresAt
        };

        Response.Cookies.Append("AuthToken", token, cookieOptions);
    }
}
