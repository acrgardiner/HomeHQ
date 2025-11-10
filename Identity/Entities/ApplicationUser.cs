using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace HomeHQ.Identity;

public class ApplicationUser : IdentityUser
{
    [NotMapped]
    public override string? Email { get; set; }
    [NotMapped]
    public override string? NormalizedEmail { get; set; }
    [NotMapped]
    public override bool EmailConfirmed { get; set; }
    [NotMapped]
    public override string? PhoneNumber { get; set; }

    [NotMapped]
    public override bool PhoneNumberConfirmed { get; set; }
    [NotMapped]
    public override bool TwoFactorEnabled { get; set; }

    public DateTime? LastLogin { get; set; }
}
