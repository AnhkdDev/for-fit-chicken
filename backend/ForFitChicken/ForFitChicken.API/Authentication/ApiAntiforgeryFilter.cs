using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ForFitChicken.API.Authentication;

public sealed class ApiAntiforgeryFilter(IAntiforgery antiforgery) : IAsyncAuthorizationFilter, IOrderedFilter
{
    public int Order => 1000;

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var method = context.HttpContext.Request.Method;
        if (HttpMethods.IsGet(method) || HttpMethods.IsHead(method) || HttpMethods.IsOptions(method) || HttpMethods.IsTrace(method) ||
            context.Filters.OfType<IgnoreAntiforgeryTokenAttribute>().Any()) return;
        try
        {
            await antiforgery.ValidateRequestAsync(context.HttpContext);
        }
        catch (AntiforgeryValidationException)
        {
            context.Result = new ObjectResult(new ProblemDetails
            {
                Status = 400, Title = "CSRF token không hợp lệ. Hãy tải lại token và thử lại.",
                Extensions = { ["code"] = "CSRF_INVALID" }
            }) { StatusCode = 400, ContentTypes = { "application/problem+json" } };
        }
    }
}
