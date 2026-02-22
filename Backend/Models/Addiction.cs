namespace LifeHub.Models;

public class Addiction
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    public required string Title { get; set; }
    public required string Color { get; set; }

    public DateTime CreatedAt { get; set; }

    public Guid? GoalId { get; set; }
    public Goal? Goal { get; set; }

    public List<AddictionReset> Resets { get; } = [];
}
