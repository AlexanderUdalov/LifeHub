namespace LifeHub.Models;

public class Goal
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    public required string Title { get; set; }
    public string? Description { get; set; }
    public DateTime DueDate { get; set; }

    public Guid? LifeFocusId { get; set; }
    public LifeFocus? LifeFocus { get; set; }
    public List<TaskItem> Tasks { get; } = [];
}