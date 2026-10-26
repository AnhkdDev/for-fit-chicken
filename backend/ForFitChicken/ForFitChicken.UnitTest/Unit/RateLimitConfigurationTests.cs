using ForFitChicken.API.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ForFitChicken.UnitTest.Unit;

[Trait("Category", "Unit")]
public sealed class RateLimitConfigurationTests
{
    [Theory]
    [InlineData("Global:PermitLimit", "0")]
    [InlineData("Login:PermitLimit", "-1")]
    [InlineData("Checkout:WindowSeconds", "0")]
    [InlineData("Admin:WindowSeconds", "86401")]
    [InlineData("ReportConcurrencyLimit", "0")]
    public void InvalidLimits_FailOptionsValidationBeforeServingRequests(string key, string value)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"RateLimiting:{key}"] = value
        }).Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApiRateLimiting(configuration);
        using var provider = services.BuildServiceProvider();
        var exception = Assert.Throws<OptionsValidationException>(() =>
            provider.GetRequiredService<IOptions<ApiRateLimitOptions>>().Value);

        Assert.Contains(exception.Failures, failure => failure.Contains("RateLimiting:", StringComparison.Ordinal));
    }
}
