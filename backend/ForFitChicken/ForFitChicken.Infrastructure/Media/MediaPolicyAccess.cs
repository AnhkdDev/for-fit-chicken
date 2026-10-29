using System.Security.Cryptography;
using System.Text;
using ForFitChicken.Application.Commerce;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ForFitChicken.Infrastructure.Media;
public sealed class ConfiguredMediaPolicySettings(IOptions<MediaOptions> options) : IMediaPolicySettings
{
    public MediaPolicySettings Current
    {
        get {
            var o = options.Value; var s = o.Security;
            var hash = o.R2.IsValid() ? Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(o.R2.Endpoint.TrimEnd('/').ToLowerInvariant() + "|" + o.R2.BucketName))) : "";
            return new(o.Provider == "R2", hash, s.MaxStoredBytes, s.DailyUploads, s.DailyInputBytes, s.PerUserDailyUploads,
                s.ActiveUploads, s.MonthlyClassA, s.MonthlyClassB, s.ReconcileMaxAgeHours, s.OrphanGraceHours);
        }
    }
}
// Each policy action owns a fresh UoW: a failed caller transaction must not prevent quota cleanup.
public sealed class MediaPolicyAccess(IServiceScopeFactory scopes) : IMediaPolicyAccess
{
    public async Task<T> RunAsync<T>(Func<MediaPolicyService, Task<T>> action)
    { using var scope = scopes.CreateScope(); return await action(scope.ServiceProvider.GetRequiredService<MediaPolicyService>()); }
}
