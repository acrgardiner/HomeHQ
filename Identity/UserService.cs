using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MudBlazor;
using projectaardvarkx2.Data;
using projectaardvarkx2.Repositories;

namespace projectaardvarkx2.Identity
{
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

            var a = await _userManager.CreateAsync(newUser, password);

            await _userManager.AddToRolesAsync(newUser, roles.Select(r => r.ToString()));

            return newUser;
        }
    }
}
