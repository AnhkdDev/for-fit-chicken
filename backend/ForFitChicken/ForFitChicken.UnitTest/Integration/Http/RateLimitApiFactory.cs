using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using ForFitChicken.API;
using ForFitChicken.API.RateLimiting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ForFitChicken.UnitTest.Integration.Http;

public sealed class RateLimitApiFactory(Dictionary<string, string?>? overrides = null)
    : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(overrides ?? []));
        builder.ConfigureTestServices(services =>
        {
            services.AddControllers().AddApplicationPart(typeof(RateLimitProbeController).Assembly);
            services.AddSingleton<ReportGate>();
            services.AddTransient<IStartupFilter, TestClientAddressFilter>();
            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = "Test";
                    options.DefaultChallengeScheme = "Test";
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>("Test", _ => { });
        });
    }

    public HttpClient Client(string ip = "192.0.2.1", string? user = null)
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        });
        client.DefaultRequestHeaders.Add("X-Test-Client-IP", ip);
        if (user is not null)
        {
            client.DefaultRequestHeaders.Add("X-Test-User", user);
        }
        return client;
    }
}

// These routes/middleware exist in the test assembly only, never in the production API.
[ApiController]
[AllowAnonymous]
[IgnoreAntiforgeryToken]
[Route("api/rate-limit-probe")]
public sealed class RateLimitProbeController(ReportGate gate) : ControllerBase
{
    [HttpGet("global")]
    public IActionResult Global() => Ok();

    [HttpPost("login")]
    [EnableRateLimiting(RateLimitPolicies.Login)]
    public IActionResult Login() => Ok();

    [HttpPost("checkout")]
    [EnableRateLimiting(RateLimitPolicies.Checkout)]
    public IActionResult Checkout() => Ok();

    [HttpGet("admin")]
    [EnableRateLimiting(RateLimitPolicies.Admin)]
    public IActionResult Admin() => Ok();

    [HttpGet("reports")]
    [EnableRateLimiting(RateLimitPolicies.Reports)]
    public async Task<IActionResult> Reports([FromQuery] bool hold, CancellationToken cancellationToken)
    {
        if (hold)
        {
            gate.Started.TrySetResult(true);
            await gate.Release.Task.WaitAsync(cancellationToken);
        }
        return Ok();
    }
}

public sealed class ReportGate
{
    public TaskCompletionSource<bool> Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource<bool> Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
}

public sealed class TestClientAddressFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
    {
        app.Use(async (context, nextMiddleware) =>
        {
            if (IPAddress.TryParse(context.Request.Headers["X-Test-Client-IP"].ToString(), out var address))
            {
                context.Connection.RemoteIpAddress = address;
            }
            await nextMiddleware(context);
        });
        next(app);
    };
}

public sealed class TestAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var userId = Request.Headers["X-Test-User"].ToString();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var identity = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId)], Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(
            new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
    }
}
