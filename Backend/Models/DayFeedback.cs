namespace LifeHub.Models;

public class DayFeedback
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public DateOnly Date { get; set; }
    public int Score { get; set; }
    public string? Note { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
