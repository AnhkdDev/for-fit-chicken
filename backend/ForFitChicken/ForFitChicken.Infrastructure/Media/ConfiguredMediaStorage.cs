using ForFitChicken.Application.Commerce;
using Microsoft.Extensions.Options;

namespace ForFitChicken.Infrastructure.Media;

// Existing flat keys remain local. New R2 keys carry an immutable provider prefix.
public sealed class ConfiguredMediaStorage(LocalMediaStorage local, R2MediaStorage r2, IOptions<MediaOptions> options) : IMediaStorage
{
    public Task<StoredMedia> SaveAsync(Stream stream, CancellationToken token) =>
        options.Value.Provider == "R2" ? r2.SaveAsync(stream, token) : local.SaveAsync(stream, token);
    public Task<Stream> OpenAsync(string key, CancellationToken token) =>
        key.StartsWith("r2/", StringComparison.Ordinal) ? r2.OpenAsync(key, token) : local.OpenAsync(key, token);
    public Task DeleteAsync(string key, CancellationToken token) =>
        key.StartsWith("r2/", StringComparison.Ordinal) ? r2.DeleteAsync(key, token) : local.DeleteAsync(key, token);
}
