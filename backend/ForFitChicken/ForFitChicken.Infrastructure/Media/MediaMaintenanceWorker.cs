using ForFitChicken.Application.Commerce;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ForFitChicken.Infrastructure.Media;
public sealed class MediaMaintenanceWorker(IServiceScopeFactory scopes, IOptions<MediaOptions> options,
    TimeProvider clock, ILogger<MediaMaintenanceWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (options.Value.Provider != "R2") return;
        var first = true;
        while (!stoppingToken.IsCancellationRequested) {
            try {
                using var scope = scopes.CreateScope();
                var status = await scope.ServiceProvider.GetRequiredService<MediaPolicyService>().StatusAsync(stoppingToken);
                if (first || !status.Initialized || status.LastReconciledAtUtc < clock.GetUtcNow().UtcDateTime.AddHours(-6))
                    status = await scope.ServiceProvider.GetRequiredService<IMediaMaintenance>().ReconcileAsync(stoppingToken);
                first = false;
                if (status.Warnings.Length != 0) logger.LogWarning("Media safety warning codes: {Codes}", string.Join(",", status.Warnings));
            } catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
            catch { logger.LogWarning("Media reconciliation unavailable; quota stays conservative. Check Owner media status."); }
            try { await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
        }
    }
}
