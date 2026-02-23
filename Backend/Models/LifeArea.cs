namespace LifeHub.Models;

public class LifeArea
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    public required string Name { get; set; }
    public required string Color { get; set; }
}
