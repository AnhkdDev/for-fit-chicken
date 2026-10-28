using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ForFitChicken.API;
using ForFitChicken.API.Authentication;
using ForFitChicken.API.RateLimiting;
using ForFitChicken.Application.Authentication;
using ForFitChicken.Infrastructure.Identity;
using ForFitChicken.Infrastructure.Persistence;
using ForFitChicken.UnitTest.Integration.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace ForFitChicken.UnitTest.Integration.Http;

public sealed class AdminAuthApiFactory(MigratedDatabaseFixture database, int loginLimit = 20, Action<IServiceCollection>? configure = null,
    Action<DbContextOptionsBuilder>? contextOptions = null) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<ForFitChickenDbContext>>();
            services.AddDbContext<ForFitChickenDbContext>(options =>
            {
                options.UseSqlServer(database.ConnectionString);
                contextOptions?.Invoke(options);
            });
            services.AddControllers().AddApplicationPart(typeof(AuthPolicyProbeController).Assembly);
            services.PostConfigure<ApiRateLimitOptions>(options => options.Login.PermitLimit = loginLimit);
            configure?.Invoke(services);
        });
    }

    public HttpClient Client(bool cookies = true) => CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false, HandleCookies = cookies
    });
}

[ApiController]
[Route("api/auth-policy-probe")]
public sealed class AuthPolicyProbeController : ControllerBase
{
    [HttpGet("staff")]
    public IActionResult Staff() => Ok();

    [HttpGet("owner")]
    [Authorize(Policy = AdminPolicies.Owner)]
    public IActionResult Owner() => Ok();
}

[Trait("Category", "SqlServerIntegration")]
public sealed class AdminAuthTests(MigratedDatabaseFixture database) : IClassFixture<MigratedDatabaseFixture>
{
    // Test-only credential, never used for the real development account.
    private const string Password = "TestOnly-StrongPassword42!";

    [SqlServerFact]
    public async Task AnonymousRequests_ReceiveJson401_AndHealthStaysPublic()
    {
        await using var factory = new AdminAuthApiFactory(database);
        using var client = factory.Client();
        var me = await client.GetAsync("/api/admin/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);
        Assert.Equal("application/problem+json", me.Content.Headers.ContentType?.MediaType);
        Assert.Null(me.Headers.Location);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth-policy-probe/staff")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health")).StatusCode);
    }

    [SqlServerFact]
    public async Task Login_RequiresCsrf_AndCookieHasSecureFlags()
    {
        await using var factory = new AdminAuthApiFactory(database);
        var user = await CreateUserAsync(factory, AdminRoles.Owner);
        using var client = factory.Client();
        var missing = await client.PostAsJsonAsync("/api/admin/auth/login", new { userName = user, password = Password });
        Assert.Equal(HttpStatusCode.BadRequest, missing.StatusCode);
        Assert.Equal("CSRF_INVALID", (await missing.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
        var anonymousToken = await CsrfAsync(client);
        var login = await LoginAsync(client, user, Password, anonymousToken);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var profile = await login.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(user, profile.GetProperty("userName").GetString());
        Assert.False(profile.TryGetProperty("passwordHash", out _));
        var cookie = login.Headers.GetValues("Set-Cookie").Single(x => x.StartsWith("__Host-ForFitChicken.Admin="));
        Assert.Contains("secure", cookie.ToLowerInvariant());
        Assert.Contains("httponly", cookie.ToLowerInvariant());
        Assert.Contains("samesite=strict", cookie.ToLowerInvariant());
        Assert.DoesNotContain("expires=", cookie.ToLowerInvariant());
        Assert.True(login.Headers.CacheControl?.NoStore);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/admin/auth/me")).StatusCode);
        // CSRF tokens are identity-bound: anonymous tokens cannot mutate an authenticated session.
        Assert.Equal(HttpStatusCode.BadRequest, (await PostAsync(client, "/api/admin/auth/logout", anonymousToken)).StatusCode);
    }

    [SqlServerFact]
    public async Task OperatorCannotUseOwnerPolicy_AndRoleChangesApplyImmediately()
    {
        await using var factory = new AdminAuthApiFactory(database);
        var userName = await CreateUserAsync(factory, AdminRoles.Operator);
        using var client = factory.Client();
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(client, userName)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/auth-policy-probe/staff")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/auth-policy-probe/owner")).StatusCode);
        await MutateUserAsync(factory, userName, async (users, user) => Assert.True((await users.AddToRoleAsync(user, AdminRoles.Owner)).Succeeded));
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/auth-policy-probe/owner")).StatusCode);
        await MutateUserAsync(factory, userName, async (users, user) => Assert.True((await users.RemoveFromRoleAsync(user, AdminRoles.Owner)).Succeeded));
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/auth-policy-probe/owner")).StatusCode);
    }

    [SqlServerFact]
    public async Task Logout_RequiresCsrf_AndRevokesOtherCookies()
    {
        await using var factory = new AdminAuthApiFactory(database);
        var user = await CreateUserAsync(factory, AdminRoles.Owner);
        using var first = factory.Client();
        using var second = factory.Client();
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(first, user)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(second, user)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await first.PostAsync("/api/admin/auth/logout", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await PostAsync(first, "/api/admin/auth/logout", await CsrfAsync(first))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await first.GetAsync("/api/admin/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await second.GetAsync("/api/admin/auth/me")).StatusCode);
    }

    [SqlServerFact]
    public async Task CookieTicket_ExpiresAfterEightHours_AndExpiredOrTamperedCookiesAreRejected()
    {
        await using var factory = new AdminAuthApiFactory(database);
        var user = await CreateUserAsync(factory, AdminRoles.Owner);
        using var client = factory.Client();
        var response = await LoginAsync(client, user);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var cookie = response.Headers.GetValues("Set-Cookie").Single(x => x.StartsWith("__Host-ForFitChicken.Admin=")).Split(';')[0];
        var options = factory.Services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>().Get(IdentityConstants.ApplicationScheme);
        var ticket = options.TicketDataFormat.Unprotect(cookie[(cookie.IndexOf('=') + 1)..]);
        Assert.NotNull(ticket);
        Assert.False(ticket.Properties.IsPersistent);
        Assert.Equal(TimeSpan.FromHours(8), ticket.Properties.ExpiresUtc - ticket.Properties.IssuedUtc);
        ticket.Properties.ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(-1);
        using var expired = factory.Client(cookies: false);
        expired.DefaultRequestHeaders.Add("Cookie", "__Host-ForFitChicken.Admin=" + options.TicketDataFormat.Protect(ticket));
        Assert.Equal(HttpStatusCode.Unauthorized, (await expired.GetAsync("/api/admin/auth/me")).StatusCode);
        using var tampered = factory.Client(cookies: false);
        tampered.DefaultRequestHeaders.Add("Cookie", "__Host-ForFitChicken.Admin=invalid-cookie");
        Assert.Equal(HttpStatusCode.Unauthorized, (await tampered.GetAsync("/api/admin/auth/me")).StatusCode);
    }

    [SqlServerFact]
    public async Task DisabledAccount_RejectsExistingCookieAndNewLogin()
    {
        await using var factory = new AdminAuthApiFactory(database);
        var user = await CreateUserAsync(factory, AdminRoles.Owner);
        using var client = factory.Client();
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(client, user)).StatusCode);
        await MutateUserAsync(factory, user, async (users, account) =>
        {
            account.IsActive = false;
            Assert.True((await users.UpdateAsync(account)).Succeeded);
        });
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/admin/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await LoginAsync(client, user)).StatusCode);
    }

    [SqlServerFact]
    public async Task ChangedSecurityStamp_RejectsExistingCookie()
    {
        await using var factory = new AdminAuthApiFactory(database);
        var user = await CreateUserAsync(factory, AdminRoles.Owner);
        using var client = factory.Client();
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(client, user)).StatusCode);
        await MutateUserAsync(factory, user, async (users, account) => Assert.True((await users.UpdateSecurityStampAsync(account)).Succeeded));
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/admin/auth/me")).StatusCode);
    }

    [SqlServerFact]
    public async Task RemovingAllAdminRoles_RejectsExistingCookieAndNewLogin()
    {
        await using var factory = new AdminAuthApiFactory(database);
        var user = await CreateUserAsync(factory, AdminRoles.Operator);
        using var client = factory.Client();
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(client, user)).StatusCode);
        await MutateUserAsync(factory, user, async (users, account) => Assert.True((await users.RemoveFromRoleAsync(account, AdminRoles.Operator)).Succeeded));
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/admin/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await LoginAsync(client, user)).StatusCode);
    }

    [SqlServerFact]
    public async Task InvalidCredentials_AndInvalidInput_DoNotIssueAuthCookie()
    {
        await using var factory = new AdminAuthApiFactory(database);
        using var client = factory.Client();
        var unknown = await LoginAsync(client, Guid.NewGuid().ToString("N"));
        Assert.Equal(HttpStatusCode.Unauthorized, unknown.StatusCode);
        Assert.False(unknown.Headers.TryGetValues("Set-Cookie", out var cookies) && cookies.Any(x => x.StartsWith("__Host-ForFitChicken.Admin=")));
        var empty = await LoginAsync(client, "", "");
        Assert.Equal(HttpStatusCode.BadRequest, empty.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/admin/auth/me")).StatusCode);
    }

    [SqlServerFact]
    public async Task FiveWrongPasswords_LockAccount_WithoutRevealingReason()
    {
        await using var factory = new AdminAuthApiFactory(database);
        var user = await CreateUserAsync(factory, AdminRoles.Operator);
        using var client = factory.Client();
        for (var i = 0; i < 5; i++)
            Assert.Equal(HttpStatusCode.Unauthorized, (await LoginAsync(client, user, "WrongPassword42!" )).StatusCode);
        var locked = await LoginAsync(client, user);
        Assert.Equal(HttpStatusCode.Unauthorized, locked.StatusCode);
        Assert.Equal("LOGIN_FAILED", (await locked.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
        await MutateUserAsync(factory, user, async (users, account) =>
        {
            Assert.True(await users.IsLockedOutAsync(account));
            Assert.True((await users.SetLockoutEndDateAsync(account, DateTimeOffset.UtcNow.AddSeconds(-1))).Succeeded);
        });
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(client, user)).StatusCode);
    }

    [SqlServerFact]
    public async Task RealLoginEndpoint_UsesLoginRateLimit()
    {
        await using var factory = new AdminAuthApiFactory(database, loginLimit: 2);
        using var client = factory.Client();
        Assert.Equal(HttpStatusCode.Unauthorized, (await LoginAsync(client, "unknown-user")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await LoginAsync(client, "unknown-user")).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, (await LoginAsync(client, "unknown-user")).StatusCode);
    }

    [SqlServerFact]
    public async Task Bootstrap_CreatesOneOwner_AndDoesNotResetExistingAccount()
    {
        // Dedicated DB: other tests intentionally create Owners too.
        var isolated = new MigratedDatabaseFixture();
        try
        {
            await isolated.InitializeAsync();
            await using var factory = new AdminAuthApiFactory(isolated);
            using (var scope = factory.Services.CreateScope())
            {
                var result = await scope.ServiceProvider.GetRequiredService<IOwnerBootstrapper>()
                    .CreateAsync("first-owner", "Chủ nhóm", Password, CancellationToken.None);
                Assert.True(result.Created);
            }
            using (var scope = factory.Services.CreateScope())
            {
                var result = await scope.ServiceProvider.GetRequiredService<IOwnerBootstrapper>()
                    .CreateAsync("another-owner", "Other", Password, CancellationToken.None);
                Assert.False(result.Created);
                Assert.Contains("OwnerAlreadyExists", result.Errors);
                var db = scope.ServiceProvider.GetRequiredService<ForFitChickenDbContext>();
                Assert.Equal(1, await db.Users.CountAsync());
            }
            using var client = factory.Client();
            Assert.Equal(HttpStatusCode.OK, (await LoginAsync(client, "first-owner")).StatusCode);
        }
        finally { await isolated.DisposeAsync(); }
    }

    [SqlServerFact]
    public async Task Bootstrap_RejectsWeakPassword_AndSerializesConcurrentCreation()
    {
        var isolated = new MigratedDatabaseFixture();
        try
        {
            await isolated.InitializeAsync();
            await using var factory = new AdminAuthApiFactory(isolated);
            using (var scope = factory.Services.CreateScope())
            {
                var result = await scope.ServiceProvider.GetRequiredService<IOwnerBootstrapper>()
                    .CreateAsync("weak-owner", "Test", "weak", CancellationToken.None);
                Assert.False(result.Created);
                Assert.Contains("PasswordTooShort", result.Errors);
                Assert.False(await scope.ServiceProvider.GetRequiredService<ForFitChickenDbContext>().Users.AnyAsync());
            }
            async Task<OwnerBootstrapResult> CreateAsync(string name)
            {
                using var scope = factory.Services.CreateScope();
                return await scope.ServiceProvider.GetRequiredService<IOwnerBootstrapper>()
                    .CreateAsync(name, "Test", Password, CancellationToken.None);
            }
            var results = await Task.WhenAll(CreateAsync("concurrent-owner-a"), CreateAsync("concurrent-owner-b"));
            Assert.Single(results.Where(x => x.Created));
            Assert.Single(results.Where(x => x.Errors.Contains("OwnerAlreadyExists")));
            await using var verification = isolated.Context();
            Assert.Equal(1, await verification.Users.CountAsync());
            Assert.Equal(1, await verification.UserRoles.CountAsync(x => x.RoleId == 1));
        }
        finally { await isolated.DisposeAsync(); }
    }

    private static async Task<string> CreateUserAsync(AdminAuthApiFactory factory, string role)
    {
        using var scope = factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser { UserName = Guid.NewGuid().ToString("N"), DisplayName = "Test admin", LockoutEnabled = true };
        Assert.True((await users.CreateAsync(user, Password)).Succeeded);
        Assert.True((await users.AddToRoleAsync(user, role)).Succeeded);
        return user.UserName;
    }

    private static async Task MutateUserAsync(AdminAuthApiFactory factory, string userName, Func<UserManager<ApplicationUser>, ApplicationUser, Task> mutate)
    {
        using var scope = factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        await mutate(users, (await users.FindByNameAsync(userName))!);
    }

    private static async Task<string> CsrfAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/admin/auth/csrf");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("requestToken").GetString()!;
    }

    private static async Task<HttpResponseMessage> LoginAsync(HttpClient client, string user, string password = Password, string? token = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/admin/auth/login")
        { Content = JsonContent.Create(new { userName = user, password }) };
        request.Headers.Add("X-CSRF-TOKEN", token ?? await CsrfAsync(client));
        return await client.SendAsync(request);
    }

    private static Task<HttpResponseMessage> PostAsync(HttpClient client, string path, string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, path);
        request.Headers.Add("X-CSRF-TOKEN", token);
        return client.SendAsync(request);
    }
}
