using ForFitChicken.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ForFitChicken.Infrastructure.Notifications;

public sealed class NotificationDispatcher(ForFitChickenDbContext db, ITelegramSender sender, TimeProvider clock)
{
    public async Task<bool> ProcessOneAsync(CancellationToken token)
    {
        var now = clock.GetUtcNow().UtcDateTime; var lease = Guid.NewGuid(); var until = now.AddMinutes(2);
        // A crashed final attempt must not leave an expired Processing row forever.
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE [NotificationJobs] SET [Status]='Failed', [LeaseToken]=NULL, [LeaseUntilUtc]=NULL,
                [LastError]='ATTEMPTS_EXHAUSTED_AMBIGUOUS'
            WHERE [Status]='Processing' AND [LeaseUntilUtc] <= {now} AND [AttemptCount] >= 6;
            """, token);
        var jobs = await db.NotificationJobs.FromSqlInterpolated($"""
            ;WITH candidate AS (
                SELECT TOP (1) * FROM [NotificationJobs] WITH (UPDLOCK, READPAST, READCOMMITTEDLOCK)
                WHERE ([Status]='Pending' AND [NextAttemptAtUtc] <= {now}
                    OR [Status]='Processing' AND [LeaseUntilUtc] <= {now}) AND [AttemptCount] < 6
                ORDER BY [NextAttemptAtUtc], [Id]
            )
            UPDATE candidate SET [Status]='Processing', [LeaseToken]={lease}, [LeaseUntilUtc]={until},
                [AttemptCount]=[AttemptCount]+1 OUTPUT inserted.*;
            """).AsNoTracking().ToArrayAsync(token);
        var job = jobs.SingleOrDefault(); if (job is null) return false;
        // External HTTP runs after the atomic claim, without a database transaction held open.
        var result = await sender.SendAsync(job.TargetChatId, job.PayloadJson, token);
        var finish = clock.GetUtcNow().UtcDateTime;
        if (result.Success)
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE [NotificationJobs] SET [Status]='Sent', [SentAtUtc]={finish}, [TelegramMessageId]={result.MessageId},
                    [LeaseToken]=NULL, [LeaseUntilUtc]=NULL, [LastError]=NULL
                WHERE [Id]={job.Id} AND [Status]='Processing' AND [LeaseToken]={lease};
                """, token);
        else
        {
            var state = result.PermanentFailure || job.AttemptCount >= 6 ? "Failed" : "Pending";
            var next = finish.AddSeconds(Math.Max(result.RetryAfterSeconds ?? 0, Math.Min(3600, 15 * (1 << job.AttemptCount))));
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE [NotificationJobs] SET [Status]={state}, [NextAttemptAtUtc]={next}, [LastError]={result.SafeError},
                    [LeaseToken]=NULL, [LeaseUntilUtc]=NULL
                WHERE [Id]={job.Id} AND [Status]='Processing' AND [LeaseToken]={lease};
                """, token);
        }
        return true;
    }
}
