namespace ForFitChicken.Application.Authentication;

public sealed record AdminProfile(long Id, string UserName, string DisplayName, IReadOnlyList<string> Roles);

public interface IAdminAuthService
{
    Task<bool> LoginAsync(string userName, string password);
    Task<AdminProfile?> GetProfileAsync(long userId);
    Task<bool> RevokeSessionsAsync(long userId);
}

public interface IOwnerBootstrapper
{
    Task<OwnerBootstrapResult> CreateAsync(string userName, string displayName, string password, CancellationToken cancellationToken);
}

public sealed record OwnerBootstrapResult(bool Created, IReadOnlyList<string> Errors);

public static class AdminRoles
{
    public const string Owner = "Owner";
    public const string Operator = "Operator";
}
