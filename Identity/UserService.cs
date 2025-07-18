using Microsoft.EntityFrameworkCore;
using projectaardvarkx2.Data;
using projectaardvarkx2.Repositories;

namespace projectaardvarkx2.Identity
{
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _applicationContext;

        public UserService(ApplicationDbContext context)
        {
            _applicationContext = context;

        }

        public async Task<List<ApplicationUser>> GetUsersAsync()
        {
            return await _applicationContext.Users.AsNoTracking().ToListAsync();
        }

        public async Task<int> GetUserCountAsync()
        {
            return await _applicationContext.Users.CountAsync();
        }
    }
}
