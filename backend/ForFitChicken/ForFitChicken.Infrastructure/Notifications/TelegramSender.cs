using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace ForFitChicken.Infrastructure.Notifications;

public sealed class TelegramSender(HttpClient http, IOptions<TelegramOptions> options) : ITelegramSender, IDisposable
{
    public async Task<TelegramSendResult> SendAsync(long chatId, string payloadJson, CancellationToken token)
    {
        try
        {
            using var payload = JsonDocument.Parse(payloadJson);
            var root = payload.RootElement;
            var text = $"Đơn COD mới: {root.GetProperty("OrderNumber").GetString()}\nTiền hàng: {root.GetProperty("ItemsSubtotal").GetDecimal():N0} VND\nSố gói: {root.GetProperty("PackageCount").GetInt32()}\nMở trang quản trị để gọi xác nhận. Chưa giữ hàng.";
            // Raw HttpClient intentionally has no logging handler: Telegram token is in its URL.
            using var response = await http.PostAsJsonAsync("https://api.telegram.org/bot" + options.Value.BotToken + "/sendMessage", new { chat_id = chatId, text }, token);
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(token));
            var data = json.RootElement;
            if (response.IsSuccessStatusCode && data.TryGetProperty("ok", out var ok) && ok.GetBoolean())
                return new(true, data.GetProperty("result").GetProperty("message_id").GetInt64(), false, null, null);
            int? retry = data.TryGetProperty("parameters", out var parameters) && parameters.TryGetProperty("retry_after", out var retryValue)
                ? Math.Clamp(retryValue.GetInt32(), 1, 86400) : null;
            return new(false, null, (int)response.StatusCode is 400 or 401 or 403 or 404, retry, "TELEGRAM_HTTP_" + (int)response.StatusCode);
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested) { return new(false, null, false, null, "TELEGRAM_TIMEOUT_AMBIGUOUS"); }
        catch (HttpRequestException) { return new(false, null, false, null, "TELEGRAM_NETWORK_FAILURE"); }
        catch (JsonException) { return new(false, null, false, null, "TELEGRAM_INVALID_RESPONSE"); }
        catch (KeyNotFoundException) { return new(false, null, true, null, "TELEGRAM_INVALID_PAYLOAD"); }
    }
    public void Dispose() => http.Dispose();
}
