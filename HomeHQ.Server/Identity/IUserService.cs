namespace HomeHQ.Identity;

public interface IUserService
{
    Task<List<ApplicationUser>> GetUsersAsync();
    Task<int> GetUserCountAsync();
    Task<ApplicationUser> AddUser(string username, string password, List<Roles> roles);
    Task<IList<string>> GetUserRolesAsync(string userId);
    Task<bool> UpdateUserName(string userId, string newUsername);
    Task<bool> UpdateUserAsync(string userId, string newUsername, IEnumerable<Roles> roles);
    Task<bool> ResetPasswordAsync(string userId, string newPassword);
}
