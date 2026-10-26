using ForFitChicken.API.RateLimiting;
using ForFitChicken.Infrastructure;

namespace ForFitChicken.API;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers();
        builder.Services.AddProblemDetails();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddHealthChecks();
        builder.Services.AddAuthentication();
        builder.Services.AddApiRateLimiting(builder.Configuration);
        builder.Services.AddInfrastructure(builder.Configuration);

        var app = builder.Build();

        app.UseExceptionHandler();
        app.UseStatusCodePages();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.UseRouting();
        app.UseAuthentication();
        app.UseRateLimiter();
        app.UseAuthorization();

        // Liveness only: database readiness is added with persistence features.
        // Explicit operational exemption: liveness must not consume business quotas.
        app.MapHealthChecks("/health").DisableRateLimiting();
        app.MapControllers();

        app.Run();
    }
}
