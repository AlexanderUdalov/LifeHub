namespace LifeHub.Models;

public enum HabitState { Active, Paused, Archived }

public class Habit
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    public required string Title { get; set; }
    public required string Color { get; set; }

    public DateOnly StartDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public HabitState State { get; set; } = HabitState.Active;

    /// <summary>iCalendar RRULE (RFC 5545) value (without "RRULE:" prefix). Use "FREQ=DAILY" for every day.</summary>
    public required string RecurrenceRule { get; set; }

    /// <summary>When set, habit goal is "N times per week" (any days); streak = consecutive weeks with at least N completions. When null, use RecurrenceRule BYDAY (specific weekdays).</summary>
    public int? TimesPerWeekGoal { get; set; }

    public Guid? GoalId { get; set; }
    public Goal? Goal { get; set; }
    public Guid? LifeAreaId { get; set; }
    public LifeArea? LifeArea { get; set; }

    public List<HabitDay> Days { get; } = [];
    public List<HabitSchedule> Schedules { get; } = [];
    public List<HabitInactivePeriod> InactivePeriods { get; } = [];
}

public class HabitSchedule
{
    public Guid Id { get; set; }
    public Guid HabitId { get; set; }
    public Habit? Habit { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public required string RecurrenceRule { get; set; }
    public int? TimesPerWeekGoal { get; set; }
}

public class HabitInactivePeriod
{
    public Guid Id { get; set; }
    public Guid HabitId { get; set; }
    public Habit? Habit { get; set; }
    public DateOnly StartDate { get; set; }
    /// <summary>Exclusive. Null while paused or archived.</summary>
    public DateOnly? EndDate { get; set; }
}

