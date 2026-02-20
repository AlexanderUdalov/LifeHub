namespace LifeHub.Models;

public class Habit
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    public required string Title { get; set; }
    public required string Color { get; set; }

    /// <summary>iCalendar RRULE (RFC 5545) value (without "RRULE:" prefix). Use "FREQ=DAILY" for every day.</summary>
    public required string RecurrenceRule { get; set; }

    public Guid? GoalId { get; set; }
    public Goal? Goal { get; set; }

    public List<HabitDay> Days { get; } = [];
}

