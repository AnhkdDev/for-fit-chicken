using System.ComponentModel.DataAnnotations;
using System.Reflection;
using ForFitChicken.Application.Abstractions.Persistence;
using ForFitChicken.Domain.Entities;

namespace ForFitChicken.Application.Commerce;

public abstract class BusinessService(ICommerceStore store, IUnitOfWork unit, TimeProvider clock)
{
    protected ICommerceStore Store { get; } = store;
    protected IUnitOfWork Unit { get; } = unit;
    protected DateTime Now => clock.GetUtcNow().UtcDateTime;
    protected Task<T> Write<T>(Func<Task<T>> action, CancellationToken token) => Unit.ExecuteInTransactionAsync(async ct =>
    {
        await Store.AcquireWriteLockAsync(ct);
        return await action();
    }, token);
    protected async Task<T> Need<T>(long id, CancellationToken token) where T : class =>
        await Store.FindAsync<T>(id, token) ?? throw new BusinessException(404, "NOT_FOUND", "Không tìm thấy dữ liệu.");
    protected static void Check(bool condition, string code, string message, int status = 409)
    { if (!condition) throw new BusinessException(status, code, message); }
    protected static void Validate(object input)
    {
        var errors = new List<ValidationResult>();
        Validator.TryValidateObject(input, new ValidationContext(input), errors, true);
        // Positional-record constraints live on constructor parameters for MVC compatibility.
        // Validate the same metadata when use cases are invoked outside HTTP (workers/tests/CLI).
        var constructor = input.GetType().GetConstructors().OrderByDescending(x => x.GetParameters().Length).FirstOrDefault();
        foreach (var parameter in constructor?.GetParameters() ?? [])
        {
            var property = input.GetType().GetProperty(parameter.Name!, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
            if (property is null) continue;
            foreach (var rule in parameter.GetCustomAttributes<ValidationAttribute>())
            {
                var result = rule.GetValidationResult(property.GetValue(input), new ValidationContext(input) { MemberName = property.Name });
                if (result is not null) errors.Add(result);
            }
        }
        if (errors.Count != 0)
            throw new BusinessException(400, "INVALID_INPUT", string.Join("; ", errors.Select(x => x.ErrorMessage)));
    }
    protected static void Version(byte[] actual, string? expected) => Check(
        !string.IsNullOrWhiteSpace(expected) && Convert.ToBase64String(actual) == expected,
        "VERSION_CONFLICT", "Dữ liệu đã thay đổi. Hãy tải lại trước khi thử.");
    protected static string Revision(byte[] value) => Convert.ToBase64String(value);
    protected static int Size(int size) => Math.Clamp(size, 1, 100);
    protected static int Number(int page) => Math.Clamp(page, 1, 100000);
    protected void Audit(long actor, string action, string entity, long id) => Store.Add(new AuditLog
    { ActorUserId = actor, Action = action, EntityType = entity, EntityId = id.ToString(System.Globalization.CultureInfo.InvariantCulture), CreatedAtUtc = Now });
}
