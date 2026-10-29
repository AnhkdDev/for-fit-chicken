using System.Net;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;

namespace ForFitChicken.API;
public static class DeploymentConfiguration
{
    public static IServiceCollection AddDeploymentConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        var protection = services.AddDataProtection().SetApplicationName("ForFitChicken");
        var keysPath = configuration["DataProtection:KeysPath"];
        if (!string.IsNullOrWhiteSpace(keysPath)) protection.PersistKeysToFileSystem(new DirectoryInfo(Path.GetFullPath(keysPath)));
        var proxies = configuration.GetSection("ReverseProxy:KnownProxies").Get<string[]>() ?? [];
        if (proxies.Length != 0) services.Configure<ForwardedHeadersOptions>(options => {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.ForwardLimit = 1; options.KnownNetworks.Clear(); options.KnownProxies.Clear();
            foreach (var proxy in proxies) {
                if (!IPAddress.TryParse(proxy, out var address)) throw new InvalidOperationException("ReverseProxy:KnownProxies requires explicit IP addresses.");
                options.KnownProxies.Add(address);
            }
        });
        return services;
    }
}
