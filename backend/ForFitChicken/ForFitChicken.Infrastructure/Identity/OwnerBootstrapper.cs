using ForFitChicken.Application.Abstractions.Persistence;
using ForFitChicken.Application.Authentication;
using ForFitChicken.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ForFitChicken.Infrastructure.Identity;

public sealed class OwnerBootstrapper(ForFitChickenDbContext db, IUnitOfWork unit, UserManager<ApplicationUser> users)
    : IOwnerBootstrapper
{
    public Task<OwnerBootstrapResult> CreateAsync(string userName, string displayName, string password, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userName) || userName.Length > 100 ||
            string.IsNullOrWhiteSpace(displayName) || displayName.Length > 150 || string.IsNullOrEmpty(password) || password.Length > 128)
            return Task.FromResult(new OwnerBootstrapResult(false, ["InvalidBootstrapInput"]));

        return unit.ExecuteInTransactionAsync(async token =>
        {
            // Serialize initial-owner creation across concurrent CLI processes.
            var role = await db.Roles.FromSqlRaw(
                "SELECT * FROM [AspNetRoles] WITH (UPDLOCK, HOLDLOCK) WHERE [NormalizedName] = N'OWNER'").SingleAsync(token);
            if (await db.UserRoles.AnyAsync(x => x.RoleId == role.Id, token))
                return new OwnerBootstrapResult(false, ["OwnerAlreadyExists"]);

            var user = new ApplicationUser { UserName = userName.Trim(), DisplayName = displayName.Trim(), LockoutEnabled = true };
            var created = await users.CreateAsync(user, password);
            if (!created.Succeeded) return new OwnerBootstrapResult(false, created.Errors.Select(x => x.Code).ToArray());
            var assigned = await users.AddToRoleAsync(user, AdminRoles.Owner);
            if (!assigned.Succeeded) throw new InvalidOperationException("Could not assign initial Owner role; transaction rolled back.");
            return new OwnerBootstrapResult(true, []);
        }, cancellationToken);
    }
}
