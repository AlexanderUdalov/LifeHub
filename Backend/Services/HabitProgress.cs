using LifeHub.Models;

namespace LifeHub.Services;

public static class HabitProgress
{
    private static readonly string[] Weekdays = ["MO", "TU", "WE", "TH", "FR", "SA", "SU"];

    public static (string Rule, int? Goal) ScheduleOn(Habit habit, DateOnly date)
    {
        var schedule = habit.Schedules.Where(x => x.EffectiveFrom <= date)
            .MaxBy(x => x.EffectiveFrom);
        return schedule is null ? (habit.RecurrenceRule, habit.TimesPerWeekGoal)
            : (schedule.RecurrenceRule, schedule.TimesPerWeekGoal);
    }

    public static bool IsActiveOn(Habit habit, DateOnly date) => date >= habit.StartDate &&
        !habit.InactivePeriods.Any(p => p.StartDate <= date && (!p.EndDate.HasValue || date < p.EndDate));

    public static bool IsScheduled(Habit habit, DateOnly date)
    {
        if (!IsActiveOn(habit, date)) return false;
        var (rule, goal) = ScheduleOn(habit, date);
        if (goal.HasValue) return true;
        var byDay = rule.ToUpperInvariant().Split(';').FirstOrDefault(x => x.StartsWith("BYDAY="));
        return byDay is null || byDay[6..].Split(',').Contains(Weekdays[((int)date.DayOfWeek + 6) % 7]);
    }

    public static DateOnly Monday(DateOnly date) => date.AddDays(-(((int)date.DayOfWeek + 6) % 7));

    public static bool TryNormalizeSchedule(string? rule, int? goal, out string normalized)
    {
        normalized = "FREQ=DAILY";
        if (goal is < 1 or > 7 || string.IsNullOrWhiteSpace(rule)) return false;
        var fields = rule.Trim().ToUpperInvariant().Replace("RRULE:", "").Split(';');
        var options = new Dictionary<string, string>();
        foreach (var field in fields)
        {
            var parts = field.Split('=');
            if (parts.Length != 2 || !options.TryAdd(parts[0], parts[1])) return false;
        }
        if (!options.TryGetValue("FREQ", out var frequency) || frequency is not ("DAILY" or "WEEKLY")) return false;
        if (options.Keys.Except(["FREQ", "BYDAY", "INTERVAL", "WKST"]).Any()) return false;
        if (options.TryGetValue("INTERVAL", out var interval) && interval != "1") return false;
        if (options.TryGetValue("WKST", out var wkst) && !Weekdays.Contains(wkst)) return false;
        if (options.TryGetValue("BYDAY", out var byDay))
        {
            var days = byDay.Split(',').Distinct().ToArray();
            if (days.Any(d => !Weekdays.Contains(d))) return false;
            if (days.Length < 7) normalized = "FREQ=WEEKLY;BYDAY=" + string.Join(',', Weekdays.Where(days.Contains));
        }
        if (goal.HasValue) normalized = "FREQ=WEEKLY";
        return true;
    }

    public static int Streak(Habit habit, IReadOnlyList<HabitDay> history, DateOnly today)
    {
        if (today < habit.StartDate) return 0;
        var status = history.Where(d => d.Date <= today).GroupBy(d => d.Date)
            .ToDictionary(g => g.Key, g => g.Max(d => d.Status));
        var current = ScheduleOn(habit, today);
        var count = 0;
        if (current.Goal is null)
        {
            for (var day = today; day >= habit.StartDate; day = day.AddDays(-1))
            {
                if (ScheduleOn(habit, day).Goal.HasValue) break;
                if (!IsScheduled(habit, day)) continue;
                var value = status.GetValueOrDefault(day);
                if (value == HabitDayStatus.Full) count++;
                else if (value != HabitDayStatus.Skip && day != today) break;
                if (day == DateOnly.MinValue) break;
            }
            return count;
        }

        for (var monday = Monday(today); monday.AddDays(6) >= habit.StartDate; monday = monday.AddDays(-7))
        {
            var days = Enumerable.Range(0, 7).Select(monday.AddDays).ToArray();
            var eligible = days.Where(d => d >= habit.StartDate && IsScheduled(habit, d)).ToArray();
            var weekly = eligible.Where(d => ScheduleOn(habit, d).Goal.HasValue).ToArray();
            // Changing the tracking mode starts a series in the new unit.
            if (eligible.Any(d => ScheduleOn(habit, d).Goal is null)) break;
            if (weekly.Length == 0) continue; // A whole week on pause is neutral.
            var goal = ScheduleOn(habit, weekly[0]).Goal!.Value;
            var full = weekly.Count(d => d <= today && status.GetValueOrDefault(d) == HabitDayStatus.Full);
            if (full >= goal) count++;
            else if (monday != Monday(today) && weekly.Length == 7) break;
        }
        return count;
    }
}
