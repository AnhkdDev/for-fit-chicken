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
using Microsoft.Extensions.Configuration;

namespace ForFitChicken.UnitTest.Integration.Http;

public sealed class AdminAuthApiFactory(MigratedDatabaseFixture database, int loginLimit = 20, Action<IServiceCollection>? configure = null,
    Action<DbContextOptionsBuilder>? contextOptions = null) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
            new Dictionary<string, string?> { ["Media:Provider"] = "Local", ["Telegram:Enabled"] = "false" }));
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
    [Authorize(Policy = AdminPolicies.Staff)]
    public IActionResult Staff() => Ok();

    [HttpGet("fallback")]
    public IActionResult Fallback() => Ok();

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
    public async Task OperatorScope_ExpandedOperations_KeepOwnerAdministrationRestricted()
    {
        await using var factory=new AdminAuthApiFactory(database);using var staff=factory.Client();using var owner=factory.Client();
        var staffName=await CreateUserAsync(factory,AdminRoles.Operator);var ownerName=await CreateUserAsync(factory,AdminRoles.Owner);
        Assert.Equal(HttpStatusCode.OK,(await LoginAsync(staff,staffName)).StatusCode);Assert.Equal(HttpStatusCode.OK,(await LoginAsync(owner,ownerName)).StatusCode);
        foreach(var path in new[]{"/api/auth-policy-probe/fallback","/api/admin/content","/api/admin/employees","/api/admin/media/assets","/api/admin/media/storage","/api/admin/inventory/batches/1/cost","/api/admin/gyms/count-lines/1/resolutions"})Assert.Equal(HttpStatusCode.Forbidden,(await staff.GetAsync(path)).StatusCode);
        foreach(var path in new[]{"/api/admin/orders","/api/admin/reports/sales?fromUtc=2026-01-01T00:00:00Z&toUtc=2026-01-02T00:00:00Z","/api/admin/notifications","/api/admin/products","/api/admin/inventory/batches","/api/admin/inventory/balances","/api/admin/inventory/locations","/api/admin/inventory/documents","/api/admin/gyms","/api/admin/auth/me"})Assert.Equal(HttpStatusCode.OK,(await staff.GetAsync(path)).StatusCode);
        foreach(var path in new[]{"/api/admin/orders","/api/admin/content","/api/admin/notifications","/api/admin/employees"})Assert.Equal(HttpStatusCode.OK,(await owner.GetAsync(path)).StatusCode);
        async Task<HttpResponseMessage> Send(string path,object input){using var request=new HttpRequestMessage(HttpMethod.Post,path){Content=JsonContent.Create(input)};request.Headers.Add("X-CSRF-TOKEN",await CsrfAsync(staff));return await staff.SendAsync(request);}
        foreach(var path in new[]{"/api/admin/content/HomeHero/publish","/api/admin/gyms/count-lines/1/resolutions","/api/admin/media/reconcile"})Assert.Equal(HttpStatusCode.Forbidden,(await Send(path,new{})).StatusCode);
        // Authenticated mutations reach validation/business rules; no Owner-only gate remains.
        Assert.Equal(HttpStatusCode.BadRequest,(await Send("/api/admin/orders",new{})).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,(await Send("/api/admin/notifications/9223372036854775807/retry",new{})).StatusCode);
        Assert.Equal(HttpStatusCode.OK,(await staff.GetAsync("/api/admin/orders/9223372036854775807/receipts")).StatusCode);
        var slug="scope-"+Guid.NewGuid().ToString("N");
        var draft=new ForFitChicken.Application.Commerce.ProductInput("Scope product",slug,null,null,null,null,null,null,true,false,false,0,null);
        var created=await Send("/api/admin/products",draft);Assert.Equal(HttpStatusCode.OK,created.StatusCode);
        var product=(await created.Content.ReadFromJsonAsync<ForFitChicken.Application.Commerce.ProductView>())!;
        using(var request=new HttpRequestMessage(HttpMethod.Put,$"/api/admin/products/{product.Id}"){Content=JsonContent.Create(draft with {IsPublished=true,Version=product.Version})}){request.Headers.Add("X-CSRF-TOKEN",await CsrfAsync(staff));Assert.Equal(HttpStatusCode.Forbidden,(await staff.SendAsync(request)).StatusCode);}
        var adjustment=new {operationKey=Guid.NewGuid(),documentType="Adjustment",reason="Permission test",lines=new[]{new{batchId=999999,quantity=1,toLocationId=999999,toCondition="Good"}}};
        Assert.Equal(HttpStatusCode.Forbidden,(await Send("/api/admin/inventory/documents",adjustment)).StatusCode);
    }

    [SqlServerFact]
    public async Task Employees_OwnerOnly_ValidatesAndRevokesSessions()
    {
        await using var factory=new AdminAuthApiFactory(database);
        using var owner=factory.Client();using var staff=factory.Client();using var anon=factory.Client();
        Assert.Equal(HttpStatusCode.Unauthorized,(await anon.GetAsync("/api/admin/employees")).StatusCode);
        var ownerName=await CreateUserAsync(factory,AdminRoles.Owner);
        Assert.Equal(HttpStatusCode.OK,(await LoginAsync(owner,ownerName)).StatusCode);
        async Task<HttpResponseMessage> Write(HttpClient c,HttpMethod method,string path,object input){var req=new HttpRequestMessage(method,path){Content=JsonContent.Create(input)};req.Headers.Add("X-CSRF-TOKEN",await CsrfAsync(c));return await c.SendAsync(req);}
        var name="employee-"+Guid.NewGuid().ToString("N");
        Assert.Equal(HttpStatusCode.BadRequest,(await owner.PostAsJsonAsync("/api/admin/employees",new EmployeeCreate(name,"Test employee",Password))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,(await Write(owner,HttpMethod.Post,"/api/admin/employees",new EmployeeCreate(name,"Test employee",new string('a',16)))).StatusCode);
        var created=await Write(owner,HttpMethod.Post,"/api/admin/employees",new EmployeeCreate(name,"Test employee",Password));Assert.Equal(HttpStatusCode.Created,created.StatusCode);
        var employee=(await created.Content.ReadFromJsonAsync<EmployeeView>())!;
        Assert.Equal(HttpStatusCode.Conflict,(await Write(owner,HttpMethod.Post,"/api/admin/employees",new EmployeeCreate(name,"Duplicate",Password))).StatusCode);
        Assert.Equal(HttpStatusCode.OK,(await LoginAsync(staff,name)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,(await staff.GetAsync("/api/admin/employees")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,(await Write(staff,HttpMethod.Post,"/api/admin/employees",new EmployeeCreate("denied-"+Guid.NewGuid().ToString("N"),"Denied",Password))).StatusCode);
        using(var scope=factory.Services.CreateScope()){var users=scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();var u=(await users.FindByNameAsync(name))!;Assert.Equal(new[]{AdminRoles.Operator},await users.GetRolesAsync(u));var own=(await users.FindByNameAsync(ownerName))!;Assert.Equal(HttpStatusCode.NotFound,(await owner.GetAsync($"/api/admin/employees/{own.Id}")).StatusCode);}
        var list=await owner.GetStringAsync($"/api/admin/employees?search={name}&active=true&size=1");Assert.Contains(name,list);Assert.DoesNotContain("passwordHash",list,StringComparison.OrdinalIgnoreCase);Assert.DoesNotContain("securityStamp",list,StringComparison.OrdinalIgnoreCase);
        Assert.Equal(HttpStatusCode.Conflict,(await Write(owner,HttpMethod.Put,$"/api/admin/employees/{employee.Id}",new EmployeeEdit("stale","Stale"))).StatusCode);
        var edited=await Write(owner,HttpMethod.Put,$"/api/admin/employees/{employee.Id}",new EmployeeEdit(employee.Version,"Updated employee"));Assert.Equal(HttpStatusCode.OK,edited.StatusCode);employee=(await edited.Content.ReadFromJsonAsync<EmployeeView>())!;Assert.Equal("Updated employee",employee.DisplayName);
        var disabled=await Write(owner,HttpMethod.Put,$"/api/admin/employees/{employee.Id}/status",new EmployeeStatus(employee.Version,false));Assert.Equal(HttpStatusCode.OK,disabled.StatusCode);employee=(await disabled.Content.ReadFromJsonAsync<EmployeeView>())!;
        Assert.Equal(HttpStatusCode.Unauthorized,(await staff.GetAsync("/api/admin/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,(await LoginAsync(staff,name)).StatusCode);
        var enabled=await Write(owner,HttpMethod.Put,$"/api/admin/employees/{employee.Id}/status",new EmployeeStatus(employee.Version,true));employee=(await enabled.Content.ReadFromJsonAsync<EmployeeView>())!;
        Assert.Equal(HttpStatusCode.OK,(await LoginAsync(staff,name)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,(await Write(owner,HttpMethod.Post,$"/api/admin/employees/{employee.Id}/password",new EmployeePassword(employee.Version,"weak"))).StatusCode);
        Assert.Equal(HttpStatusCode.OK,(await Write(owner,HttpMethod.Post,$"/api/admin/employees/{employee.Id}/password",new EmployeePassword(employee.Version,Password+"New1!"))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,(await staff.GetAsync("/api/admin/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,(await LoginAsync(staff,name)).StatusCode);
        Assert.Equal(HttpStatusCode.OK,(await LoginAsync(staff,name,Password+"New1!")).StatusCode);
    }

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
            Assert.Single(results, x => x.Created);
            Assert.Single(results, x => x.Errors.Contains("OwnerAlreadyExists"));
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


