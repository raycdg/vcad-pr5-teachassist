using Microsoft.AspNetCore.Identity;

namespace TeachAssist.Api.Models;

public class AppUser : IdentityUser
{
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
