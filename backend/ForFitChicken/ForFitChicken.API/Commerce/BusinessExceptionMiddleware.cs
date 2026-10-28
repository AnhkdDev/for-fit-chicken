using ForFitChicken.Application.Commerce;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ForFitChicken.API.Commerce;

public sealed class BusinessExceptionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try { await next(context); }
        catch (BusinessException error) { await Problem(context, error.Status, error.Code, error.Message); }
        catch (DbUpdateConcurrencyException) { await Problem(context, 409, "VERSION_CONFLICT", "Dữ liệu đã thay đổi. Tải lại rồi thử với yêu cầu mới."); }
        catch (DbUpdateException error) when (error.InnerException is SqlException { Number: 2601 or 2627 })
        { await Problem(context, 409, "DUPLICATE_DATA", "Mã hoặc khóa yêu cầu đã tồn tại. Tải lại dữ liệu trước khi thử."); }
        catch (SqlException error) when (error.Number is 1205 or 51001)
        { await Problem(context, 409, "BUSINESS_BUSY", "Dữ liệu đang được xử lý. Thử lại bằng yêu cầu mới, giữ cùng Idempotency-Key nếu có."); }
        catch (OverflowException) { await Problem(context, 400, "QUANTITY_TOO_LARGE", "Số lượng vượt giới hạn xử lý."); }
    }
    private static Task Problem(HttpContext context, int status, string code, string message)
    {
        context.Response.StatusCode = status; context.Response.Headers.CacheControl = "no-store";
        return context.Response.WriteAsJsonAsync(new ProblemDetails { Status = status, Title = message, Extensions = { ["code"] = code } },
            options: (System.Text.Json.JsonSerializerOptions?)null, contentType: "application/problem+json", cancellationToken: context.RequestAborted);
    }
}
