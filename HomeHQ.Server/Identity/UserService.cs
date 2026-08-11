using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using HomeHQ.Data;

namespace HomeHQ.Identity;

public class UserService : IUserService
{
    private readonly ApplicationDbContext _applicationContext;
    private readonly UserManager<ApplicationUser> _userManager;

    public UserService(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _applicationContext = context;
        _userManager = userManager;
    }

    public async Task<List<ApplicationUser>> GetUsersAsync()
    {
        return await _applicationContext.Users.AsNoTracking().ToListAsync();
    }

    public async Task<int> GetUserCountAsync()
    {
        return await _applicationContext.Users.CountAsync();
    }

    public async Task<ApplicationUser> AddUser(string username, string password, List<Roles> roles)
    {
        var newUser = new ApplicationUser
        {
            UserName = username
        };

        var createResult = await _userManager.CreateAsync(newUser, password);
        if (!createResult.Succeeded)
        {
            var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));
            throw new InvalidOperationException(errors);
        }

        var roleNames = NormalizeRoles(roles).Select(r => r.ToString()).ToList();
        await _userManager.AddToRolesAsync(newUser, roleNames);

        return newUser;
    }

    public async Task<IList<string>> GetUserRolesAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return Array.Empty<string>();
        }

        return await _userManager.GetRolesAsync(user);
    }

    public async Task<bool> UpdateUserName(string userId, string newUsername)
    {
        if (string.IsNullOrWhiteSpace(newUsername))
        {
            return false;
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return false;
        }

        user.UserName = newUsername;
        user.NormalizedUserName = newUsername.ToUpper();
        var result = await _userManager.UpdateAsync(user);

        return result.Succeeded;
    }

    public async Task<bool> UpdateUserAsync(string userId, string newUsername, IEnumerable<Roles> roles)
    {
        if (string.IsNullOrWhiteSpace(newUsername))
        {
            return false;
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return false;
        }

        if (!string.Equals(user.UserName, newUsername, StringComparison.Ordinal))
        {
            user.UserName = newUsername;
            user.NormalizedUserName = newUsername.ToUpperInvariant();
            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                return false;
            }
        }

        var desiredRoles = NormalizeRoles(roles).Select(r => r.ToString()).ToHashSet(StringComparer.Ordinal);
        var currentRoles = await _userManager.GetRolesAsync(user);

        var toRemove = currentRoles.Where(r => !desiredRoles.Contains(r)).ToList();
        var toAdd = desiredRoles.Where(r => !currentRoles.Contains(r, StringComparer.Ordinal)).ToList();

        if (toRemove.Count > 0)
        {
            var removeResult = await _userManager.RemoveFromRolesAsync(user, toRemove);
            if (!removeResult.Succeeded)
            {
                return false;
            }
        }

        if (toAdd.Count > 0)
        {
            var addResult = await _userManager.AddToRolesAsync(user, toAdd);
            if (!addResult.Succeeded)
            {
                return false;
            }
        }

        return true;
    }

    public async Task<bool> ResetPasswordAsync(string userId, string newPassword)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return false;
            }

            // Remove current password and set new one
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

            return result.Succeeded;
        }
        catch
        {
            return false;
        }
    }

    private static List<Roles> NormalizeRoles(IEnumerable<Roles> roles)
    {
        var normalized = roles.Distinct().ToList();
        if (!normalized.Contains(Roles.Basic))
        {
            normalized.Insert(0, Roles.Basic);
        }

        return normalized;
    }
}
