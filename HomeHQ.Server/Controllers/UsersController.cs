using HomeHQ.DTOs;
using HomeHQ.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HomeHQ.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "AdminOnly")]
public class UsersController(
    IUserService userService,
    UserManager<ApplicationUser> userManager) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<UserDto>>>> GetAll()
    {
        var users = await userService.GetUsersAsync();
        var dtos = new List<UserDto>(users.Count);

        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);
            dtos.Add(new UserDto(
                user.Id,
                user.UserName ?? string.Empty,
                user.LastLogin,
                roles.ToList()));
        }

        return Ok(ApiResponse<IEnumerable<UserDto>>.Ok(dtos.OrderBy(u => u.UserName)));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<UserDto>>> Create([FromBody] CreateUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.UserName))
        {
            return Ok(ApiResponse<UserDto>.Fail("Username is required."));
        }

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
        {
            return Ok(ApiResponse<UserDto>.Fail("Password must be at least 6 characters."));
        }

        var existing = await userManager.FindByNameAsync(request.UserName.Trim());
        if (existing is not null)
        {
            return Ok(ApiResponse<UserDto>.Fail("A user with that username already exists."));
        }

        var roles = new List<Roles> { Roles.Basic };
        if (request.IsAdmin)
        {
            roles.Add(Roles.Admin);
        }

        try
        {
            var user = await userService.AddUser(request.UserName.Trim(), request.Password, roles);
            if (string.IsNullOrEmpty(user.Id))
            {
                return Ok(ApiResponse<UserDto>.Fail("Failed to create user."));
            }

            var userRoles = await userManager.GetRolesAsync(user);
            var dto = new UserDto(user.Id, user.UserName ?? request.UserName, user.LastLogin, userRoles.ToList());
            return Ok(ApiResponse<UserDto>.Ok(dto, "User created successfully"));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse<UserDto>.Fail($"Failed to create user: {ex.Message}"));
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<UserDto>>> Update(string id, [FromBody] UpdateUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.UserName))
        {
            return Ok(ApiResponse<UserDto>.Fail("Username is required."));
        }

        var success = await userService.UpdateUserName(id, request.UserName.Trim());
        if (!success)
        {
            return Ok(ApiResponse<UserDto>.Fail("User update failed."));
        }

        var user = await userManager.FindByIdAsync(id);
        if (user is null)
        {
            return Ok(ApiResponse<UserDto>.Fail("User not found."));
        }

        var roles = await userManager.GetRolesAsync(user);
        return Ok(ApiResponse<UserDto>.Ok(
            new UserDto(user.Id, user.UserName ?? string.Empty, user.LastLogin, roles.ToList()),
            "User updated successfully"));
    }

    [HttpPost("{id}/reset-password")]
    public async Task<ActionResult<ApiResponse>> ResetPassword(string id, [FromBody] ResetPasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 6)
        {
            return Ok(ApiResponse.Fail("Password must be at least 6 characters."));
        }

        var success = await userService.ResetPasswordAsync(id, request.NewPassword);
        return Ok(success
            ? ApiResponse.Ok("Password reset successfully")
            : ApiResponse.Fail("Failed to reset password."));
    }
}
