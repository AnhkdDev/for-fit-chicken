using System.Text.RegularExpressions;

namespace ForFitChicken.Infrastructure.Media;

public sealed class MediaOptions
{
    public string Provider { get; set; } = "Local";
    public R2Options R2 { get; set; } = new();
    public MediaSecurityOptions Security { get; set; } = new();
    public bool IsValid() => (Provider is "Local" or "R2") && (Provider != "R2" || R2.IsValid()) && Security.IsValid();
}

public sealed class MediaSecurityOptions
{
    public long MaxStoredBytes { get; set; } = 9_000_000_000;
    public int DailyUploads { get; set; } = 100;
    public long DailyInputBytes { get; set; } = 200_000_000;
    public int PerUserDailyUploads { get; set; } = 50;
    public int ActiveUploads { get; set; } = 2;
    public int MonthlyClassA { get; set; } = 100_000;
    public int MonthlyClassB { get; set; } = 1_000_000;
    public int ReconcileMaxAgeHours { get; set; } = 24;
    public int OrphanGraceHours { get; set; } = 24;
    public bool IsValid() => MaxStoredBytes is > 0 and <= 9_000_000_000 && DailyUploads > 0 && DailyInputBytes > 0
        && PerUserDailyUploads > 0 && ActiveUploads is > 0 and <= 4 && MonthlyClassA is > 0 and <= 500_000
        && MonthlyClassB is > 0 and <= 5_000_000 && ReconcileMaxAgeHours is > 0 and <= 24 && OrphanGraceHours is >= 24 and <= 720;
}

public sealed class R2Options
{
    public string Endpoint { get; set; } = "";
    public string BucketName { get; set; } = "";
    public string AccessKeyId { get; set; } = "";
    public string SecretAccessKey { get; set; } = "";
    public bool IsValid() => Regex.IsMatch(Endpoint, @"^https://[a-fA-F0-9]{32}(\.(eu|us|fedramp))?\.r2\.cloudflarestorage\.com/?$")
        && Regex.IsMatch(BucketName, @"^[a-z0-9][a-z0-9-]{1,61}[a-z0-9]$")
        && !string.IsNullOrWhiteSpace(AccessKeyId) && !string.IsNullOrWhiteSpace(SecretAccessKey);
}
