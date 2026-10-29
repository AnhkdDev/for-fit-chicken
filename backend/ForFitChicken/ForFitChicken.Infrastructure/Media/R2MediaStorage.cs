using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using ForFitChicken.Application.Commerce;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text.RegularExpressions;

namespace ForFitChicken.Infrastructure.Media;

public sealed class R2MediaStorage : IMediaStorage, IDisposable
{
    private readonly Lazy<IAmazonS3> _client;
    private readonly R2Options _settings;
    public R2MediaStorage(IOptions<MediaOptions> options)
    {
        _settings = options.Value.R2;
        _client = new(() => new AmazonS3Client(new BasicAWSCredentials(_settings.AccessKeyId, _settings.SecretAccessKey),
            new AmazonS3Config { ServiceURL = _settings.Endpoint, AuthenticationRegion = "auto", ForcePathStyle = true,
                Timeout = TimeSpan.FromSeconds(30), MaxErrorRetry = 0 }));
    }
    // Injectable S3 adapter for contract tests; production uses the options-only constructor.
    public R2MediaStorage(IAmazonS3 client, R2Options settings) { _client = new(() => client); _settings = settings; }
    private IAmazonS3 Client => _settings.IsValid() ? _client.Value : throw Unavailable();
    public async Task<StoredMedia> SaveAsync(Stream stream, CancellationToken token)
    {
        var image = await ValidatedImage.ReadAsync(stream, token);
        var key = "r2/" + Guid.NewGuid().ToString("N") + "." + image.Extension;
        await PutAsync(key, image.Bytes, image.ContentType, token);
        return new(key, image.ContentType, image.Bytes.Length);
    }
    public async Task PutAsync(string key, byte[] bytes, string contentType, CancellationToken token)
    {
        ValidateKey(key);
        using var body = new MemoryStream(bytes, writable: false);
        try
        {
            await Client.PutObjectAsync(new PutObjectRequest { BucketName = _settings.BucketName, Key = key,
                InputStream = body, ContentType = contentType, UseChunkEncoding = false,
                DisablePayloadSigning = true, DisableDefaultChecksumValidation = true }, token);
        }
        catch (AmazonServiceException) { throw Unavailable(); }
        catch (AmazonClientException) { throw Unavailable(); }
        catch (HttpRequestException) { throw Unavailable(); }
        catch (OperationCanceledException) when (!token.IsCancellationRequested) { throw Unavailable(); }
    }
    public async Task<(CloudObject[] Objects, string? Next)> ListPageAsync(string? continuation, CancellationToken token)
    {
        try {
            var response = await Client.ListObjectsV2Async(new ListObjectsV2Request { BucketName = _settings.BucketName, ContinuationToken = continuation, MaxKeys = 1000 }, token);
            return (response.S3Objects.Select(x => new CloudObject(x.Key, x.Size)).ToArray(), response.IsTruncated ? response.NextContinuationToken : null);
        } catch (AmazonServiceException) { throw Unavailable(); }
        catch (AmazonClientException) { throw Unavailable(); }
        catch (HttpRequestException) { throw Unavailable(); }
        catch (OperationCanceledException) when (!token.IsCancellationRequested) { throw Unavailable(); }
    }
    public async Task<Stream> OpenAsync(string key, CancellationToken token)
    {
        ValidateKey(key);
        try
        {
            using var response = await Client.GetObjectAsync(new GetObjectRequest { BucketName = _settings.BucketName, Key = key }, token);
            var image = await ValidatedImage.ReadAsync(response.ResponseStream, token);
            return new MemoryStream(image.Bytes, writable: false);
        }
        catch (AmazonS3Exception e) when (e.StatusCode == HttpStatusCode.NotFound) { throw new BusinessException(404, "MEDIA_MISSING", "Không tìm thấy ảnh."); }
        catch (AmazonServiceException) { throw Unavailable(); }
        catch (AmazonClientException) { throw Unavailable(); }
        catch (HttpRequestException) { throw Unavailable(); }
        catch (OperationCanceledException) when (!token.IsCancellationRequested) { throw Unavailable(); }
    }
    public async Task DeleteAsync(string key, CancellationToken token)
    {
        ValidateKey(key);
        try { await Client.DeleteObjectAsync(new DeleteObjectRequest { BucketName = _settings.BucketName, Key = key }, token); }
        catch (AmazonServiceException) { throw Unavailable(); }
        catch (AmazonClientException) { throw Unavailable(); }
        catch (HttpRequestException) { throw Unavailable(); }
        catch (OperationCanceledException) when (!token.IsCancellationRequested) { throw Unavailable(); }
    }
    private static void ValidateKey(string key)
    {
        if (!Regex.IsMatch(key, @"^r2/[a-f0-9]{32}\.(png|jpg|webp)$")) throw new InvalidOperationException("Invalid media key.");
    }
    private static BusinessException Unavailable() => new(503, "MEDIA_STORAGE_UNAVAILABLE", "Kho ảnh chưa sẵn sàng. Vui lòng thử lại sau.");
    public void Dispose() { if (_client.IsValueCreated) _client.Value.Dispose(); }
}
