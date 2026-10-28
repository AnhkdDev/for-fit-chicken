using ForFitChicken.Application.Commerce;
using Microsoft.Extensions.Options;

namespace ForFitChicken.Infrastructure.Notifications;

public sealed class TelegramOptions
{
    public bool Enabled { get; set; }
    public string BotToken { get; set; } = string.Empty;
    public long? TargetChatId { get; set; }
}
public sealed class ConfiguredNotificationSettings(IOptions<TelegramOptions> options) : INotificationSettings
{
    public NotificationSettings Current => new(options.Value.Enabled, options.Value.TargetChatId);
}
public sealed record TelegramSendResult(bool Success, long? MessageId, bool PermanentFailure, int? RetryAfterSeconds, string? SafeError);
public interface ITelegramSender
{
    Task<TelegramSendResult> SendAsync(long chatId, string payloadJson, CancellationToken token);
}
