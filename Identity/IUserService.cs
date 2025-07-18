namespace projectaardvarkx2.Identity
{
    public interface IUserService
    {
        Task<List<ApplicationUser>> GetUsersAsync();
        Task<int> GetUserCountAsync();
    }
}
