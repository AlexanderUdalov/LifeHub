namespace LifeHub.Models;

public class User
{
    public Guid Id { get; set; }
    public required string Name { get; set; }

    public List<LifeFocus> Focuses { get; } = [];
    public List<Goal> Goals { get; } = [];
    public List<TaskItem> Tasks { get; } = [];
}