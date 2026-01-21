namespace LifeHub.Models;

public class User
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }

    public List<LifeFocus> Focuses { get; } = [];
    public List<Goal> Goals { get; } = [];
    public List<TaskItem> Tasks { get; } = [];
}