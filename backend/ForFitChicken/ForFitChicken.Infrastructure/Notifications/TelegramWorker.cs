using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ForFitChicken.Infrastructure.Notifications;

public sealed class TelegramWorker(IServiceScopeFactory scopes, IOptions<TelegramOptions> options, ILogger<TelegramWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled) return;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopes.CreateScope();
                await scope.ServiceProvider.GetRequiredService<NotificationDispatcher>().ProcessOneAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
            catch (Exception)
            {
                // Do not print HTTP URLs, payloads, connection strings or credential-bearing exception text.
                logger.LogWarning("Notification dispatch failed; the worker will retry after its poll interval.");
            }
            try { await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
        }
    }
}
