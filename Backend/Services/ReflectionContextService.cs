using LifeHub.Models;
using Microsoft.EntityFrameworkCore;

namespace LifeHub.Services;

public record ReflectionContext(
    TasksSummary Tasks,
    HabitsSummary Habits,
    AddictionsSummary Addictions,
    List<string> RecentJournalExcerpts,
    List<string> PreviousReflectionSummaries
);

public record TasksSummary(
    int CompletedCount,
    List<string> CompletedTitles,
    int OverdueCount,
    List<string> OverdueTitles,
    int PendingCount
);

public record HabitSummary(
    string Title,
    int DaysCompleted,
    int DaysMissed,
    int DaysSkipped
);

public record HabitsSummary(
    List<HabitSummary> Items,
    int TotalCompleted,
    int TotalMissed
);

public record AddictionSummary(
    string Title,
    int ResetsInPeriod,
    int CurrentStreakDays
);

public record AddictionsSummary(List<AddictionSummary> Items);

public class ReflectionContextService(ApplicationContext context)
{
    public async Task<ReflectionContext> GatherAsync(Guid userId, int periodDays)
    {
        var now = DateTimeOffset.UtcNow;
        var periodStart = now.AddDays(-periodDays);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var dateStart = today.AddDays(-periodDays);

        var tasks = await GatherTasksAsync(userId, periodStart, now);
        var habits = await GatherHabitsAsync(userId, dateStart, today);
        var addictions = await GatherAddictionsAsync(userId, dateStart, today);
        var journal = await GatherJournalAsync(userId, periodStart);
        var previousReflections = await GatherPreviousReflectionsAsync(userId, periodStart);

        return new ReflectionContext(tasks, habits, addictions, journal, previousReflections);
    }

    private async Task<TasksSummary> GatherTasksAsync(Guid userId, DateTimeOffset periodStart, DateTimeOffset now)
    {
        var userTasks = await context.Tasks
            .AsNoTracking()
            .Where(t => t.UserId == userId)
            .ToListAsync();

        var completed = userTasks
            .Where(t => t.CompletionDate != null && t.CompletionDate >= periodStart)
            .ToList();

        var overdue = userTasks
            .Where(t => t.CompletionDate == null && t.DueDate != null && t.DueDate < now)
            .ToList();

        var pending = userTasks
            .Count(t => t.CompletionDate == null && (t.DueDate == null || t.DueDate >= now));

        return new TasksSummary(
            CompletedCount: completed.Count,
            CompletedTitles: completed.Select(t => t.Title).Take(10).ToList(),
            OverdueCount: overdue.Count,
            OverdueTitles: overdue.Select(t => t.Title).Take(10).ToList(),
            PendingCount: pending
        );
    }

    private async Task<HabitsSummary> GatherHabitsAsync(Guid userId, DateOnly dateStart, DateOnly today)
    {
        var habits = await context.Habits
            .AsNoTracking()
            .Include(h => h.Days)
            .Where(h => h.UserId == userId)
            .ToListAsync();

        var items = new List<HabitSummary>();
        int totalCompleted = 0, totalMissed = 0;

        foreach (var habit in habits)
        {
            var daysInPeriod = habit.Days
                .Where(d => d.Date >= dateStart && d.Date <= today)
                .ToList();

            int completed = daysInPeriod.Count(d => d.Status == HabitDayStatus.Full);
            int skipped = daysInPeriod.Count(d => d.Status == HabitDayStatus.Skip);
            int missed = daysInPeriod.Count(d => d.Status == HabitDayStatus.None);

            totalCompleted += completed;
            totalMissed += missed;

            items.Add(new HabitSummary(habit.Title, completed, missed, skipped));
        }

        return new HabitsSummary(items, totalCompleted, totalMissed);
    }

    private async Task<AddictionsSummary> GatherAddictionsAsync(Guid userId, DateOnly dateStart, DateOnly today)
    {
        var addictions = await context.Addictions
            .AsNoTracking()
            .Include(a => a.Resets)
            .Where(a => a.UserId == userId)
            .ToListAsync();

        var items = addictions.Select(a =>
        {
            int resetsInPeriod = a.Resets.Count(r => r.Date >= dateStart && r.Date <= today);

            var lastReset = a.Resets
                .OrderByDescending(r => r.ResetAt)
                .FirstOrDefault();

            int streakDays = lastReset != null
                ? (DateTime.UtcNow - lastReset.ResetAt).Days
                : (DateTime.UtcNow - a.CreatedAt).Days;

            return new AddictionSummary(a.Title, resetsInPeriod, streakDays);
        }).ToList();

        return new AddictionsSummary(items);
    }

    private async Task<List<string>> GatherJournalAsync(Guid userId, DateTimeOffset periodStart)
    {
        // SQLite does not support DateTimeOffset in WHERE/ORDER BY; load and sort/filter in memory
        var entries = await context.JournalEntries
            .AsNoTracking()
            .Where(e => e.UserId == userId)
            .ToListAsync();

        return entries
            .Where(e => e.CreatedAt >= periodStart)
            .OrderByDescending(e => e.CreatedAt)
            .Take(5)
            .Select(e => e.Text.Length > 200 ? e.Text[..200] + "..." : e.Text)
            .ToList();
    }

    private async Task<List<string>> GatherPreviousReflectionsAsync(Guid userId, DateTimeOffset periodStart)
    {
        // SQLite does not support DateTimeOffset in WHERE/ORDER BY; load and sort/filter in memory
        var entries = await context.JournalEntries
            .AsNoTracking()
            .Where(e => e.UserId == userId)
            .ToListAsync();

        return entries
            .Where(e => e.CreatedAt < periodStart)
            .OrderByDescending(e => e.CreatedAt)
            .Take(3)
            .Select(e => e.Text.Length > 220 ? e.Text[..220] + "..." : e.Text)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();
    }

    public string FormatAsText(ReflectionContext ctx)
    {
        var lines = new List<string>();

        lines.Add("=== TASKS ===");
        lines.Add($"Completed: {ctx.Tasks.CompletedCount}");
        if (ctx.Tasks.CompletedTitles.Count > 0)
            lines.Add($"  Titles: {string.Join(", ", ctx.Tasks.CompletedTitles)}");
        lines.Add($"Overdue: {ctx.Tasks.OverdueCount}");
        if (ctx.Tasks.OverdueTitles.Count > 0)
            lines.Add($"  Titles: {string.Join(", ", ctx.Tasks.OverdueTitles)}");
        lines.Add($"Pending: {ctx.Tasks.PendingCount}");

        lines.Add("");
        lines.Add("=== HABITS ===");
        foreach (var h in ctx.Habits.Items)
        {
            lines.Add($"- {h.Title}: completed {h.DaysCompleted}, missed {h.DaysMissed}, skipped {h.DaysSkipped}");
        }
        lines.Add($"Total: {ctx.Habits.TotalCompleted} completed, {ctx.Habits.TotalMissed} missed");

        lines.Add("");
        lines.Add("=== ADDICTIONS ===");
        foreach (var a in ctx.Addictions.Items)
        {
            lines.Add($"- {a.Title}: {a.ResetsInPeriod} resets in period, current streak {a.CurrentStreakDays} days");
        }

        if (ctx.RecentJournalExcerpts.Count > 0)
        {
            lines.Add("");
            lines.Add("=== RECENT JOURNAL ENTRIES ===");
            foreach (var excerpt in ctx.RecentJournalExcerpts)
            {
                lines.Add($"- {excerpt}");
            }
        }

        if (ctx.PreviousReflectionSummaries.Count > 0)
        {
            lines.Add("");
            lines.Add("=== PREVIOUS REFLECTION SUMMARIES ===");
            foreach (var summary in ctx.PreviousReflectionSummaries)
            {
                lines.Add($"- {summary}");
            }
        }

        return string.Join("\n", lines);
    }

    public string FormatContextSummary(ReflectionContext ctx)
    {
        var parts = new List<string>();

        if (ctx.Tasks.CompletedCount > 0)
            parts.Add($"{ctx.Tasks.CompletedCount} tasks completed");
        if (ctx.Tasks.OverdueCount > 0)
            parts.Add($"{ctx.Tasks.OverdueCount} overdue");

        if (ctx.Habits.Items.Count > 0)
            parts.Add($"{ctx.Habits.TotalCompleted} habit days completed, {ctx.Habits.TotalMissed} missed");

        foreach (var a in ctx.Addictions.Items)
        {
            if (a.ResetsInPeriod > 0)
                parts.Add($"{a.Title}: {a.ResetsInPeriod} resets");
            else
                parts.Add($"{a.Title}: {a.CurrentStreakDays} days clean");
        }

        return parts.Count > 0 ? string.Join("; ", parts) : "No activity data found for this period.";
    }
}
