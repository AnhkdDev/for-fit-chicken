namespace ForFitChicken.API.RateLimiting;

public sealed class ApiRateLimitOptions
{
    public const string SectionName = "RateLimiting";

    public WindowLimitOptions Global { get; set; } = new() { PermitLimit = 120 };
    public WindowLimitOptions Catalog { get; set; } = new() { PermitLimit = 120 };
    public WindowLimitOptions Login { get; set; } = new() { PermitLimit = 5 };
    public WindowLimitOptions Checkout { get; set; } = new() { PermitLimit = 10 };
    public WindowLimitOptions Admin { get; set; } = new() { PermitLimit = 120 };
    public WindowLimitOptions Upload { get; set; } = new() { PermitLimit = 10 };
    public int ReportConcurrencyLimit { get; set; } = 2;
}

public sealed class WindowLimitOptions
{
    public int PermitLimit { get; set; } = 120;
    public int WindowSeconds { get; set; } = 60;
}
