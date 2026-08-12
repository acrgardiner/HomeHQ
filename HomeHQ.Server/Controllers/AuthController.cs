using HomeHQ.DTOs;
using HomeHQ.Identity;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace HomeHQ.Server.Controllers;

/// <summary>
/// Custom auth controller for mobile app authentication.
/// Note: You can also use the built-in endpoints at /api/identity/login instead.
/// </summary>
[Route("api/[controller]")]
[ApiController]
public class AuthController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    IOptionsMonitor<BearerTokenOptions> bearerTokenOptions,
    TimeProvider timeProvider) : ControllerBase
{
    public record LoginRequest(string Username, string Password);

    [HttpPost("login")]
    public async Task<Results<Ok<AccessTokenResponse>, UnauthorizedHttpResult, ProblemHttpResult>> Login([FromBody] LoginRequest request)
    {
        // Find user by username
        var user = await userManager.FindByNameAsync(request.Username);
        if (user is null)
        {
            return TypedResults.Unauthorized();
        }

        // Validate password
        var result = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: false);
        if (!result.Succeeded)
        {
            return TypedResults.Unauthorized();
        }

        // Generate bearer token using Identity's built-in mechanism
        var principal = await signInManager.CreateUserPrincipalAsync(user);

        // Update last login timestamp
        user.LastLogin = DateTime.UtcNow;
        await userManager.UpdateAsync(user);

        var response = GenerateAccessTokenResponse(principal);
        return TypedResults.Ok(response);
    }

    [HttpGet("me")]
    [Authorize(Policy = "BearerAndCookies")]
    public async Task<ActionResult<ApiResponse<CurrentUserDto>>> Me()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return Unauthorized();
        }

        var roles = await userManager.GetRolesAsync(user);
        return Ok(ApiResponse<CurrentUserDto>.Ok(
            new CurrentUserDto(user.UserName ?? string.Empty, roles.ToList())));
    }

    private AccessTokenResponse GenerateAccessTokenResponse(System.Security.Claims.ClaimsPrincipal principal)
    {
        var options = bearerTokenOptions.Get(IdentityConstants.BearerScheme);
        var now = timeProvider.GetUtcNow();

        var accessExpiration = now + options.BearerTokenExpiration;
        var refreshExpiration = now + options.RefreshTokenExpiration;

        return new AccessTokenResponse
        {
            AccessToken = GenerateToken(principal, options.BearerTokenProtector, accessExpiration),
            ExpiresIn = (long)options.BearerTokenExpiration.TotalSeconds,
            RefreshToken = GenerateToken(principal, options.RefreshTokenProtector, refreshExpiration)
        };
    }

    private static string GenerateToken(
        System.Security.Claims.ClaimsPrincipal principal,
        Microsoft.AspNetCore.Authentication.ISecureDataFormat<Microsoft.AspNetCore.Authentication.AuthenticationTicket> protector,
        DateTimeOffset expiration)
    {
        var ticket = new Microsoft.AspNetCore.Authentication.AuthenticationTicket(
            principal,
            authenticationScheme: IdentityConstants.BearerScheme);

        ticket.Properties.ExpiresUtc = expiration;

        return protector.Protect(ticket);
    }
}
