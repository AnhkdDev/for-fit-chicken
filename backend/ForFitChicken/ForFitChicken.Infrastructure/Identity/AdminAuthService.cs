using ForFitChicken.Application.Authentication;
using Microsoft.AspNetCore.Identity;

namespace ForFitChicken.Infrastructure.Identity;

public sealed class AdminAuthService(UserManager<ApplicationUser> users, SignInManager<ApplicationUser> signIn)
    : IAdminAuthService
{
    public async Task<bool> LoginAsync(string userName, string password)
    {
        var user = await users.FindByNameAsync(userName.Trim());
        if (user is null)
        {
            // Perform a password hash verification even for unknown accounts.
            _ = users.PasswordHasher.VerifyHashedPassword(new ApplicationUser(), DummyPasswordHash, password);
            return false;
        }
        if (!user.IsActive) return false;
        var roles = await users.GetRolesAsync(user);
        if (!roles.Contains(AdminRoles.Owner) && !roles.Contains(AdminRoles.Operator)) return false;
        var result = await signIn.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);
        if (!result.Succeeded) return false;
        await signIn.SignInAsync(user, isPersistent: false);
        return true;
    }

    private static readonly string DummyPasswordHash = new PasswordHasher<ApplicationUser>()
        .HashPassword(new ApplicationUser(), Guid.NewGuid().ToString("N"));

    public async Task<AdminProfile?> GetProfileAsync(long userId)
    {
        var user = await users.FindByIdAsync(userId.ToString(System.Globalization.CultureInfo.InvariantCulture));
        if (user is null || !user.IsActive) return null;
        return new AdminProfile(user.Id, user.UserName!, user.DisplayName, (await users.GetRolesAsync(user)).ToArray());
    }

    public async Task<bool> RevokeSessionsAsync(long userId)
    {
        var user = await users.FindByIdAsync(userId.ToString(System.Globalization.CultureInfo.InvariantCulture));
        return user is not null && (await users.UpdateSecurityStampAsync(user)).Succeeded;
    }
}
