using System.Security.Claims;
using ForFitChicken.Application.Authentication;
using ForFitChicken.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ForFitChicken.API.Authentication;

public static class AdminPolicies
{
    public const string Staff = "AdminStaff";
    public const string Owner = "OwnerOnly";
}

public static class AdminAuthenticationExtensions
{
    public static IServiceCollection AddAdminAuthentication(this IServiceCollection services)
    {
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = IdentityConstants.ApplicationScheme;
            options.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
            options.DefaultSignInScheme = IdentityConstants.ApplicationScheme;
        }).AddCookie(IdentityConstants.ApplicationScheme, options =>
        {
            options.Cookie.Name = "__Host-ForFitChicken.Admin";
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.Cookie.Path = "/";
            options.ExpireTimeSpan = TimeSpan.FromHours(8);
            options.SlidingExpiration = false;
            options.Events.OnRedirectToLogin = context => ErrorAsync(context.HttpContext, 401, "AUTH_REQUIRED", "Bạn cần đăng nhập.");
            options.Events.OnRedirectToAccessDenied = context => ErrorAsync(context.HttpContext, 403, "ACCESS_DENIED", "Bạn không có quyền thực hiện thao tác này.");
            options.Events.OnValidatePrincipal = ValidatePrincipalAsync;
        });
        services.AddAuthorization(options =>
        {
            options.AddPolicy(AdminPolicies.Staff, policy => policy.RequireAuthenticatedUser().RequireRole(AdminRoles.Owner, AdminRoles.Operator));
            options.AddPolicy(AdminPolicies.Owner, policy => policy.RequireAuthenticatedUser().RequireRole(AdminRoles.Owner));
            // Business controllers are private unless explicitly marked AllowAnonymous.
            options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser().RequireRole(AdminRoles.Owner, AdminRoles.Operator).Build();
        });
        services.AddAntiforgery(options =>
        {
            options.HeaderName = "X-CSRF-TOKEN";
            options.Cookie.Name = "__Host-ForFitChicken.Csrf";
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.Cookie.Path = "/";
        });
        return services;
    }

    private static async Task ValidatePrincipalAsync(CookieValidatePrincipalContext context)
    {
        var users = context.HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
        var signIn = context.HttpContext.RequestServices.GetRequiredService<SignInManager<ApplicationUser>>();
        var user = await users.GetUserAsync(context.Principal!);
        var stamp = context.Principal!.FindFirstValue("AspNet.Identity.SecurityStamp");
        if (user is null || !user.IsActive || await users.IsLockedOutAsync(user) ||
            !string.Equals(stamp, user.SecurityStamp, StringComparison.Ordinal))
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
            return;
        }
        var fresh = await signIn.CreateUserPrincipalAsync(user);
        if (!fresh.IsInRole(AdminRoles.Owner) && !fresh.IsInRole(AdminRoles.Operator))
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
            return;
        }
        // Authorize against current roles, including changes made after cookie issuance.
        context.ReplacePrincipal(fresh);
    }

    private static Task ErrorAsync(HttpContext context, int status, string code, string title)
    {
        context.Response.StatusCode = status;
        context.Response.Headers.CacheControl = "no-store";
        return context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = status, Title = title, Extensions = { ["code"] = code }
        }, options: (System.Text.Json.JsonSerializerOptions?)null, contentType: "application/problem+json", cancellationToken: context.RequestAborted);
    }
}
