namespace LifeHub.Models;

public enum HabitDayStatus
{
    None = 0,
    Skip = 1,
    Full = 2
}

public class HabitDay
{
    public Guid Id { get; set; }

    public Guid HabitId { get; set; }
    public Habit? Habit { get; set; }

    /// <summary>Local calendar date (no timezone).</summary>
    public DateOnly Date { get; set; }

    public HabitDayStatus Status { get; set; }
}

