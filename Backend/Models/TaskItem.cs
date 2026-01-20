namespace LifeHub.Models;

public class TaskItem
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    public required string Title { get; set; }
    public string? Description { get; set; }

    public DateTime? DueDate { get; set; }
    public DateTime? CompletionDate { get; set; }

    public Guid? GoalId { get; set; }
    public Goal? Goal { get; set; }
}