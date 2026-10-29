using ForFitChicken.API.RateLimiting;
using ForFitChicken.API.Authentication;
using ForFitChicken.Application.Authentication;
using Microsoft.AspNetCore.Authorization;
using ForFitChicken.Infrastructure;
using ForFitChicken.API.Commerce;

namespace ForFitChicken.API;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddDeploymentConfiguration(builder.Configuration);

        builder.Services.AddControllers(options => options.Filters.Add<ApiAntiforgeryFilter>())
            .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter(allowIntegerValues: false)));
        builder.Services.AddProblemDetails();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            options.SchemaFilter<RecordValidationSchemaFilter>();
            options.OperationFilter<CsrfOperationFilter>();
        });
        builder.Services.AddHealthChecks();
        builder.Services.AddAdminAuthentication();
        builder.Services.AddApiRateLimiting(builder.Configuration);
        builder.Services.AddInfrastructure(builder.Configuration);

        await using var app = builder.Build();

        if (args.Contains("--reconcile-media", StringComparer.Ordinal))
        {
            using var scope = app.Services.CreateScope();
            try {
                var status = await scope.ServiceProvider.GetRequiredService<ForFitChicken.Application.Commerce.IMediaMaintenance>().ReconcileAsync(CancellationToken.None);
                Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(status));
            } catch { Console.Error.WriteLine("Media reconciliation failed. Check configuration/database/network; secrets not printed."); Environment.ExitCode = 1; }
            return;
        }

        if (args.Contains("--bootstrap-owner", StringComparer.Ordinal))
        {
            await BootstrapOwnerAsync(app);
            return;
        }

        app.UseExceptionHandler();
        app.UseStatusCodePages();
        app.UseMiddleware<BusinessExceptionMiddleware>();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        if (builder.Configuration.GetSection("ReverseProxy:KnownProxies").Get<string[]>()?.Length > 0)
            app.UseForwardedHeaders();
        app.UseHttpsRedirection();
        app.UseRouting();
        app.UseAuthentication();
        app.UseRateLimiter();
        app.UseAuthorization();

        // Liveness only: database readiness is added with persistence features.
        // Explicit operational exemption: liveness must not consume business quotas.
        app.MapHealthChecks("/health").DisableRateLimiting().AllowAnonymous();
        app.MapControllers();

        app.Run();
    }

    private static async Task BootstrapOwnerAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var config = app.Configuration.GetSection("BootstrapOwner");
        var result = await scope.ServiceProvider.GetRequiredService<IOwnerBootstrapper>().CreateAsync(
            config["UserName"] ?? "", config["DisplayName"] ?? "", config["Password"] ?? "", CancellationToken.None);
        Console.WriteLine(result.Created ? "Owner created successfully." : "Owner not created: " + string.Join(", ", result.Errors));
        Environment.ExitCode = result.Created ? 0 : 1;
    }
}
