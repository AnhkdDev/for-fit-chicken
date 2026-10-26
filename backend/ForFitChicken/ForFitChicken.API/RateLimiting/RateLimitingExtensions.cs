using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace ForFitChicken.API.RateLimiting;

public static class RateLimitingExtensions
{
    public static IServiceCollection AddApiRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<IValidateOptions<ApiRateLimitOptions>, ApiRateLimitOptionsValidator>();
        services.AddOptions<ApiRateLimitOptions>()
            .Bind(configuration.GetSection(ApiRateLimitOptions.SectionName))
            .ValidateOnStart();
        services.AddRateLimiter(_ => { });
        services.AddOptions<RateLimiterOptions>().Configure<IOptions<ApiRateLimitOptions>>((options, configured) =>
        {
            var settings = configured.Value;
            // Global baseline is per network client, even for unannotated future APIs.
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                WindowPartition(IpKey(context), settings.Global));

            AddWindowPolicy(options, RateLimitPolicies.Catalog, settings.Catalog, useIdentity: false);
            AddWindowPolicy(options, RateLimitPolicies.Login, settings.Login, useIdentity: false);
            AddWindowPolicy(options, RateLimitPolicies.Checkout, settings.Checkout, useIdentity: false);
            AddWindowPolicy(options, RateLimitPolicies.Admin, settings.Admin, useIdentity: true);

            options.AddPolicy(RateLimitPolicies.Reports, context =>
                RateLimitPartition.GetConcurrencyLimiter(UserOrIpKey(context), _ => new ConcurrencyLimiterOptions
                {
                    PermitLimit = settings.ReportConcurrencyLimit,
                    QueueLimit = 0,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                }));

            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, cancellationToken) =>
            {
                var response = context.HttpContext.Response;
                response.StatusCode = StatusCodes.Status429TooManyRequests;
                response.Headers.CacheControl = "no-store";

                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    response.Headers.RetryAfter = Math.Max(1, Math.Ceiling(retryAfter.TotalSeconds))
                        .ToString(CultureInfo.InvariantCulture);
                }

                var problem = new ProblemDetails
                {
                    Status = StatusCodes.Status429TooManyRequests,
                    Type = "https://www.rfc-editor.org/rfc/rfc6585#section-4",
                    Title = "Too many requests",
                    Detail = "Bạn thao tác quá nhanh. Vui lòng thử lại sau.",
                    Instance = context.HttpContext.Request.Path
                };
                problem.Extensions["code"] = "RATE_LIMIT_EXCEEDED";

                await response.WriteAsJsonAsync(problem,
                    options: (JsonSerializerOptions?)null,
                    contentType: "application/problem+json",
                    cancellationToken: cancellationToken);
            };
        });

        return services;
    }

    private static void AddWindowPolicy(
        RateLimiterOptions options,
        string name,
        WindowLimitOptions settings,
        bool useIdentity)
    {
        options.AddPolicy(name, context =>
            WindowPartition(useIdentity ? UserOrIpKey(context) : IpKey(context), settings));
    }

    private static RateLimitPartition<string> WindowPartition(string key, WindowLimitOptions settings)
    {
        return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = settings.PermitLimit,
            Window = TimeSpan.FromSeconds(settings.WindowSeconds),
            QueueLimit = 0,
            AutoReplenishment = true,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst
        });
    }

    private static string IpKey(HttpContext context)
    {
        var address = context.Connection.RemoteIpAddress;
        if (address?.IsIPv4MappedToIPv6 == true)
        {
            address = address.MapToIPv4();
        }

        // Never read an untrusted client-supplied X-Forwarded-For header here.
        return $"ip:{address?.ToString() ?? "unknown"}";
    }

    private static string UserOrIpKey(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? context.User.FindFirstValue("sub");

            if (!string.IsNullOrWhiteSpace(userId))
            {
                return $"user:{userId}";
            }
        }

        return IpKey(context);
    }

}
