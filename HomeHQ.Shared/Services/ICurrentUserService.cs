using System;
using System.Collections.Generic;
using System.Text;

namespace HomeHQ.Services
{
    public interface ICurrentUserService
    {
        string? UserId { get; }
        string? UserName { get; }
        bool IsMobile { get; }
    }
}
