using System.Security.Claims;

namespace HomeHQ.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<CurrentUserService> _logger;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor, ILogger<CurrentUserService> logger)
        {
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public string? UserId =>
            _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        public string? UserName =>
            _httpContextAccessor.HttpContext?.User?.Identity?.Name;

        public bool IsMobile
        {
            get
            {
                var userAgent = _httpContextAccessor.HttpContext?.Request?.Headers["User-Agent"].ToString();
                if (string.IsNullOrEmpty(userAgent))
                    return false;

                // Simple mobile detection
                string[] mobileKeywords = new[]
                {
                    "Android", "iPhone", "iPad", "iPod", "Opera Mini", "IEMobile", "Mobile", "BlackBerry", "webOS"
                };

                return mobileKeywords.Any(keyword => userAgent.Contains(keyword, StringComparison.OrdinalIgnoreCase));
            }
        }
    }
}
