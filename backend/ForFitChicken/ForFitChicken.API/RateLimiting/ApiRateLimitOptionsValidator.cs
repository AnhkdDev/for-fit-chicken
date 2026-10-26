using Microsoft.Extensions.Options;

namespace ForFitChicken.API.RateLimiting;

public sealed class ApiRateLimitOptionsValidator : IValidateOptions<ApiRateLimitOptions>
{
    public ValidateOptionsResult Validate(string? name, ApiRateLimitOptions settings)
    {
        var failures = new List<string>();
        foreach (var (section, window) in new[]
                 {
                     ("Global", settings.Global), ("Catalog", settings.Catalog),
                     ("Login", settings.Login), ("Checkout", settings.Checkout), ("Admin", settings.Admin)
                 })
        {
            if (window is null || window.PermitLimit <= 0 || window.WindowSeconds <= 0
                || window.WindowSeconds > 86400)
            {
                failures.Add($"RateLimiting:{section} requires PermitLimit > 0 and WindowSeconds between 1 and 86400.");
            }
        }

        if (settings.ReportConcurrencyLimit <= 0)
        {
            failures.Add("RateLimiting:ReportConcurrencyLimit must be greater than zero.");
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}
