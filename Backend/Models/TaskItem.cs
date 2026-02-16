namespace LifeHub.Models;

public class TaskItem
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    public required string Title { get; set; }
    public string? Description { get; set; }

    public DateTimeOffset? DueDate { get; set; }
    public DateTimeOffset? CompletionDate { get; set; }

    /// <summary>iCalendar RRULE (RFC 5545), e.g. "FREQ=DAILY" or "FREQ=WEEKLY;BYDAY=MO,TH". Null = no recurrence.</summary>
    public string? RecurrenceRule { get; set; }

    public Guid? GoalId { get; set; }
    public Goal? Goal { get; set; }

    /// <summary>Manual sort order for "Today" and "Inbox" lists. Null = end of list. Reset when due date changes.</summary>
    public int? SortOrder { get; set; }
}