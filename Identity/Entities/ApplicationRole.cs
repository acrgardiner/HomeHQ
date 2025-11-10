using Microsoft.AspNetCore.Identity;

namespace HomeHQ.Identity;

public class ApplicationRole : IdentityRole
{
    public ApplicationRole() : base() { }
    public ApplicationRole(string roleName) : base(roleName) { }

}

public enum Roles
{
    SysAdmin,
    Admin,
    Basic,
}