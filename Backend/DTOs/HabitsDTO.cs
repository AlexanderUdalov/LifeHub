using LifeHub.Models;
using System.Globalization;

namespace LifeHub.DTOs;

public record HabitDTO(
    Guid Id,
    string Title,
    string Color,
    string RecurrenceRule,
    int? TimesPerWeekGoal,
    Guid? GoalId,
    Guid? LifeAreaId
);

public record HabitDayDTO(
    DateOnly Date,
    string Status
);

public record HabitWithHistoryDTO(
    HabitDTO Habit,
    IReadOnlyList<HabitDayDTO> History,
    int CurrentStreak
);

public record HabitUpsertRequest(
    string Title,
    string Color,
    string RecurrenceRule,
    int? TimesPerWeekGoal,
    Guid? GoalId,
    Guid? LifeAreaId
);

public record SetDayStatusRequest(string Status);

public static class HabitMapping
{
    public static HabitDTO ToDTO(this Habit habit) =>
        new(
            habit.Id,
            habit.Title,
            habit.Color,
            habit.RecurrenceRule,
            habit.TimesPerWeekGoal,
            habit.GoalId,
            habit.LifeAreaId
        );

    public static HabitDayDTO ToDTO(this HabitDay day) =>
        new(day.Date, day.Status.ToString().ToLowerInvariant());
}

public static class HabitStreakCalculator
{
    public static int CalculateCurrentStreak(Habit habit, IReadOnlyList<HabitDay> allDays)
    {
        if (allDays.Count == 0)
        {
            return 0;
        }

        var today = DateOnly.FromDateTime(DateTime.Today);

        // Only consider days up to today
        var filteredDays = allDays
            .Where(d => d.Date <= today)
            .OrderBy(d => d.Date)
            .ToList();

        if (filteredDays.Count == 0)
        {
            return 0;
        }

        var statusByDate = filteredDays
            .GroupBy(d => d.Date)
            .ToDictionary(g => g.Key, g => g
                .OrderByDescending(x => x.Status) // any order, we just need one value
                .First().Status);

        var minDate = filteredDays[0].Date;
        var maxDate = today;

        // Build continuous calendar range [minDate, today]
        var days = new List<DateOnly>();
        for (var d = minDate; d <= maxDate; d = d.AddDays(1))
        {
            days.Add(d);
        }

        // Helper to read completion status similar to frontend HabitCompletion
        HabitDayStatus GetStatus(DateOnly date)
        {
            return statusByDate.TryGetValue(date, out var s) ? s : HabitDayStatus.None;
        }

        if (habit.TimesPerWeekGoal is int goal && goal is >= 1 and <= 7)
        {
            // Times-per-week mode: use weekly goal streak logic
            var fullCountByWeek = BuildFullCountByWeek(filteredDays);
            var lastIndex = days.Count - 1;

            // Find the most recent full-completion day; streak is anchored there.
            var anchorIndex = FindLastFullIndex(days, lastIndex, GetStatus);
            if (anchorIndex < 0)
            {
                return 0;
            }

            return CalcTimesPerWeekStreak(days, anchorIndex, goal, GetStatus, fullCountByWeek);
        }
        else
        {
            // Fixed weekdays mode based on RecurrenceRule BYDAY
            var disabledByIndex = BuildDisabledFlags(days, habit.RecurrenceRule);
            var lastIndex = days.Count - 1;

            return CalcFixedDaysStreak(days, lastIndex, GetStatus, disabledByIndex);
        }
    }

    private static IReadOnlyDictionary<string, int> BuildFullCountByWeek(IReadOnlyList<HabitDay> days)
    {
        var dict = new Dictionary<string, int>();
        foreach (var day in days.Where(d => d.Status == HabitDayStatus.Full))
        {
            var key = GetWeekKey(day.Date);
            dict[key] = dict.TryGetValue(key, out var existing) ? existing + 1 : 1;
        }

        return dict;
    }

    private static int FindLastFullIndex(
        IReadOnlyList<DateOnly> days,
        int startIndex,
        Func<DateOnly, HabitDayStatus> getStatus)
    {
        for (var i = startIndex; i >= 0; i--)
        {
            var status = getStatus(days[i]);
            if (status == HabitDayStatus.Full)
            {
                return i;
            }

            if (status == HabitDayStatus.None)
            {
                // Streak cannot pass through a "none" day
                break;
            }
        }

        return -1;
    }

    private static int CalcTimesPerWeekStreak(
        IReadOnlyList<DateOnly> days,
        int index,
        int goal,
        Func<DateOnly, HabitDayStatus> getStatus,
        IReadOnlyDictionary<string, int> fullCountByWeek)
    {
        if (index < 0 || index >= days.Count)
        {
            return 0;
        }

        if (getStatus(days[index]) != HabitDayStatus.Full)
        {
            return 0;
        }

        var count = 1;
        var currentWeekKey = GetWeekKey(days[index]);

        for (var j = index - 1; j >= 0; j--)
        {
            var dayWeekKey = GetWeekKey(days[j]);

            if (dayWeekKey != currentWeekKey)
            {
                var prevWeekFulls = fullCountByWeek.TryGetValue(dayWeekKey, out var v) ? v : 0;
                if (prevWeekFulls < goal)
                {
                    break;
                }

                currentWeekKey = dayWeekKey;
            }

            if (getStatus(days[j]) != HabitDayStatus.Full)
            {
                continue;
            }

            count++;
        }

        return count;
    }

    private static int CalcFixedDaysStreak(
        IReadOnlyList<DateOnly> days,
        int index,
        Func<DateOnly, HabitDayStatus> getStatus,
        IReadOnlyList<bool> disabledByIndex)
    {
        var count = 0;
        for (var i = index; i >= 0; i--)
        {
            if (disabledByIndex[i])
            {
                continue;
            }

            var status = getStatus(days[i]);
            if (status == HabitDayStatus.Full)
            {
                count++;
                continue;
            }

            if (status == HabitDayStatus.Skip)
            {
                continue;
            }

            break;
        }

        return count;
    }

    private static List<bool> BuildDisabledFlags(IReadOnlyList<DateOnly> days, string recurrenceRule)
    {
        var weekdays = ParseByDayWeekdays(recurrenceRule);
        if (weekdays is null)
        {
            // Daily habits: nothing is disabled
            return Enumerable.Repeat(false, days.Count).ToList();
        }

        return days
            .Select(d => !weekdays.Contains(d.DayOfWeek))
            .ToList();
    }

    private static HashSet<DayOfWeek>? ParseByDayWeekdays(string recurrenceRule)
    {
        if (string.IsNullOrWhiteSpace(recurrenceRule))
        {
            return null;
        }

        var rule = recurrenceRule.Trim().ToUpperInvariant();

        // FREQ=DAILY or no BYDAY -> every day is active
        if (rule.Contains("FREQ=DAILY", StringComparison.Ordinal))
        {
            return null;
        }

        var byDayIndex = rule.IndexOf("BYDAY=", StringComparison.Ordinal);
        if (byDayIndex < 0)
        {
            return null;
        }

        var after = rule.Substring(byDayIndex + "BYDAY=".Length);
        var end = after.IndexOf(';');
        var listPart = end >= 0 ? after[..end] : after;

        if (string.IsNullOrWhiteSpace(listPart))
        {
            return null;
        }

        var result = new HashSet<DayOfWeek>();
        foreach (var token in listPart.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = token.Trim();
            if (trimmed.Length < 2)
            {
                continue;
            }

            var code = trimmed[..2];
            if (TryMapByDayCode(code, out var dayOfWeek))
            {
                result.Add(dayOfWeek);
            }
        }

        return result;
    }

    private static bool TryMapByDayCode(string code, out DayOfWeek dayOfWeek)
    {
        dayOfWeek = code.ToUpperInvariant() switch
        {
            "MO" => DayOfWeek.Monday,
            "TU" => DayOfWeek.Tuesday,
            "WE" => DayOfWeek.Wednesday,
            "TH" => DayOfWeek.Thursday,
            "FR" => DayOfWeek.Friday,
            "SA" => DayOfWeek.Saturday,
            "SU" => DayOfWeek.Sunday,
            _ => DayOfWeek.Sunday
        };

        return code is "MO" or "TU" or "WE" or "TH" or "FR" or "SA" or "SU";
    }

    private static string GetWeekKey(DateOnly date)
    {
        // Monday-based week, same as frontend getWeekKey()
        var d = date.ToDateTime(TimeOnly.MinValue);
        var dayOfWeek = d.DayOfWeek;
        var mondayOffset = dayOfWeek == DayOfWeek.Sunday ? -6 : DayOfWeek.Monday - dayOfWeek;
        var monday = date.AddDays(mondayOffset);
        return monday.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }
}

