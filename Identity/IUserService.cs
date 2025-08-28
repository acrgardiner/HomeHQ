namespace projectaardvarkx2.Identity
{
    public interface IUserService
    {
        Task<List<ApplicationUser>> GetUsersAsync();
        Task<int> GetUserCountAsync();
        Task<ApplicationUser> AddUser(string username, string password, List<Roles> roles);
        Task<bool> UpdateUserName(string userId, string newUsername);
        Task<bool> ResetPasswordAsync(string userId, string newPassword);
    }
}
