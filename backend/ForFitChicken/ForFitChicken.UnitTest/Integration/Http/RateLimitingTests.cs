using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace ForFitChicken.UnitTest.Integration.Http;

[Trait("Category", "HttpIntegration")]
public sealed class RateLimitingTests
{
    [Fact]
    public async Task UnannotatedEndpoint_HasBaseline_AndReturnsProblemAndRetryAfter()
    {
        using var factory = new RateLimitApiFactory(new()
        {
            ["RateLimiting:Global:PermitLimit"] = "2"
        });
        using var client = factory.Client();

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/rate-limit-probe/global")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/rate-limit-probe/global")).StatusCode);
        using var rejected = await client.GetAsync("/api/rate-limit-probe/global");

        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
        Assert.Equal("application/problem+json", rejected.Content.Headers.ContentType?.MediaType);
        Assert.True(rejected.Headers.RetryAfter?.Delta > TimeSpan.Zero);
        using var body = JsonDocument.Parse(await rejected.Content.ReadAsStringAsync());
        Assert.Equal(429, body.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("RATE_LIMIT_EXCEEDED", body.RootElement.GetProperty("code").GetString());

        // A client cannot evade the quota by spoofing a forwarded header.
        client.DefaultRequestHeaders.Add("X-Forwarded-For", "198.51.100.77");
        Assert.Equal(HttpStatusCode.TooManyRequests,
            (await client.GetAsync("/api/rate-limit-probe/global")).StatusCode);

        // Explicit liveness exemption remains usable after the business quota is exhausted.
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health")).StatusCode);
    }

    [Fact]
    public async Task LoginPolicy_PartitionsByIp_NotOneQuotaForTheWholeSite()
    {
        using var factory = new RateLimitApiFactory(new()
        {
            ["RateLimiting:Login:PermitLimit"] = "1"
        });
        using var first = factory.Client("192.0.2.1");
        using var second = factory.Client("192.0.2.2");

        Assert.Equal(HttpStatusCode.OK, (await first.PostAsync("/api/rate-limit-probe/login", null)).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests,
            (await first.PostAsync("/api/rate-limit-probe/login", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await second.PostAsync("/api/rate-limit-probe/login", null)).StatusCode);
    }

    [Fact]
    public async Task CheckoutPolicy_IsIndependentFromLoginPolicy()
    {
        using var factory = new RateLimitApiFactory(new()
        {
            ["RateLimiting:Login:PermitLimit"] = "1",
            ["RateLimiting:Checkout:PermitLimit"] = "1"
        });
        using var client = factory.Client();

        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync("/api/rate-limit-probe/login", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync("/api/rate-limit-probe/checkout", null)).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests,
            (await client.PostAsync("/api/rate-limit-probe/checkout", null)).StatusCode);
    }

    [Fact]
    public async Task AdminPolicy_PartitionsByAuthenticatedUser_OnTheSameIp()
    {
        using var factory = new RateLimitApiFactory(new()
        {
            ["RateLimiting:Admin:PermitLimit"] = "1"
        });
        using var alice = factory.Client(user: "alice");
        using var bob = factory.Client(user: "bob");

        Assert.Equal(HttpStatusCode.OK, (await alice.GetAsync("/api/rate-limit-probe/admin")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await bob.GetAsync("/api/rate-limit-probe/admin")).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests,
            (await alice.GetAsync("/api/rate-limit-probe/admin")).StatusCode);
    }

    [Fact]
    public async Task Window_ReplenishesAfterRejection()
    {
        using var factory = new RateLimitApiFactory(new()
        {
            ["RateLimiting:Login:PermitLimit"] = "1",
            ["RateLimiting:Login:WindowSeconds"] = "1"
        });
        using var client = factory.Client();

        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync("/api/rate-limit-probe/login", null)).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests,
            (await client.PostAsync("/api/rate-limit-probe/login", null)).StatusCode);
        await Task.Delay(TimeSpan.FromMilliseconds(1200));
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync("/api/rate-limit-probe/login", null)).StatusCode);
    }

    [Fact]
    public async Task Reports_RejectConcurrentWork_AndReleasePermitWhenFinished()
    {
        using var factory = new RateLimitApiFactory(new()
        {
            ["RateLimiting:ReportConcurrencyLimit"] = "1"
        });
        using var client = factory.Client(user: "alice");
        var gate = factory.Services.GetRequiredService<ReportGate>();
        var firstRequest = client.GetAsync("/api/rate-limit-probe/reports?hold=true");

        try
        {
            await gate.Started.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.Equal(HttpStatusCode.TooManyRequests,
                (await client.GetAsync("/api/rate-limit-probe/reports")).StatusCode);
        }
        finally
        {
            gate.Release.TrySetResult(true);
            using var first = await firstRequest;
            Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        }

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/rate-limit-probe/reports")).StatusCode);
    }
}
