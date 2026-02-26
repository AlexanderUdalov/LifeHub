namespace LifeHub.Models;

public class Habit
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    public required string Title { get; set; }
    public required string Color { get; set; }

    /// <summary>iCalendar RRULE (RFC 5545) value (without "RRULE:" prefix). Use "FREQ=DAILY" for every day.</summary>
    public required string RecurrenceRule { get; set; }

    /// <summary>When set, habit goal is "N times per week" (any days); streak = consecutive weeks with at least N completions. When null, use RecurrenceRule BYDAY (specific weekdays).</summary>
    public int? TimesPerWeekGoal { get; set; }

    public Guid? GoalId { get; set; }
    public Goal? Goal { get; set; }
    public Guid? LifeAreaId { get; set; }
    public LifeArea? LifeArea { get; set; }

    public List<HabitDay> Days { get; } = [];
}

