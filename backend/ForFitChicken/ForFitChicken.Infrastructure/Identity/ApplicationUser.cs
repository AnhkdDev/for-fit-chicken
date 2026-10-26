using Microsoft.AspNetCore.Identity;

namespace ForFitChicken.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser<long>
{
    public string DisplayName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
