using ForFitChicken.Application.Abstractions.Persistence;
using ForFitChicken.Application.Authentication;
using ForFitChicken.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using ForFitChicken.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ForFitChicken.Application.Commerce;
using ForFitChicken.Infrastructure.Media;
using ForFitChicken.Infrastructure.Notifications;

namespace ForFitChicken.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'DefaultConnection' is required. Configure it in Development settings or external configuration.");
        }

        services.AddDbContext<ForFitChickenDbContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<ICommerceStore, CommerceStore>();
        services.AddSingleton<IMediaStorage, LocalMediaStorage>();
        services.AddScoped<CatalogService>();
        services.AddScoped<InventoryService>();
        services.AddScoped<OrderService>();
        services.AddScoped<GymService>();
        services.AddScoped<ContentReportService>();
        services.AddOptions<TelegramOptions>().Bind(configuration.GetSection("Telegram"))
            .Validate(x => !x.Enabled || (!string.IsNullOrWhiteSpace(x.BotToken) && x.BotToken.Length <= 200 &&
                System.Text.RegularExpressions.Regex.IsMatch(x.BotToken, "^[0-9]+:[A-Za-z0-9_-]+$") && x.TargetChatId is not null and not 0),
                "Enabled Telegram requires a valid BotToken and nonzero TargetChatId.").ValidateOnStart();
        services.AddSingleton<INotificationSettings, ConfiguredNotificationSettings>();
        services.AddSingleton<ITelegramSender>(provider => new TelegramSender(
            new HttpClient(new SocketsHttpHandler { AllowAutoRedirect = false }) { Timeout = TimeSpan.FromSeconds(15) },
            provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<TelegramOptions>>()));
        services.AddScoped<NotificationDispatcher>();
        services.AddHostedService<TelegramWorker>();
        services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.Password.RequiredLength = 12;
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Lockout.AllowedForNewUsers = true;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        }).AddRoles<ApplicationRole>()
          .AddEntityFrameworkStores<ForFitChickenDbContext>()
          .AddSignInManager();
        services.AddScoped<IAdminAuthService, AdminAuthService>();
        services.AddScoped<IOwnerBootstrapper, OwnerBootstrapper>();

        return services;
    }
}
