using System.Security.Claims;

namespace projectaardvarkx2.Services
{
    public interface ICurrentUserService
    {
        string? UserId { get; }
        string? UserName { get; }
    }

    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string? UserId =>
            _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        public string? UserName =>
            _httpContextAccessor.HttpContext?.User?.Identity?.Name;
    }
}
