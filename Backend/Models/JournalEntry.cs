namespace LifeHub.Models;

public class JournalEntry
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    public required string Text { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    public bool IsPinned { get; set; }
    public DateTimeOffset? PinnedAt { get; set; }

    public Guid? TaskItemId { get; set; }
    public TaskItem? TaskItem { get; set; }

    public Guid? HabitId { get; set; }
    public Habit? Habit { get; set; }

    public Guid? AddictionId { get; set; }
    public Addiction? Addiction { get; set; }

    public Guid? GoalId { get; set; }
    public Goal? Goal { get; set; }

    public Guid? LifeAreaId { get; set; }
    public LifeArea? LifeArea { get; set; }

    /// <summary>
    /// True when the journal text was saved from the AI-assisted reflection summary.
    /// </summary>
    public bool AiGenerated { get; set; }
}

