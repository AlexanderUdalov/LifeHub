namespace LifeHub.Models;

public class LifeFocus
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    public required string Title { get; set; }
    public string? Description { get; set; }

    public float Progress { get; set; }

    public List<Goal> Goals { get; } = [];
}