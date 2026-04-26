namespace LifeHub.Models;

public enum AddictionTriggerOutcome
{
    Overcame = 0,
    Relapsed = 1
}

public class AddictionTriggerEvent
{
    public Guid Id { get; set; }

    public Guid AddictionId { get; set; }
    public Addiction? Addiction { get; set; }

    public DateTime EventAt { get; set; }
    public AddictionTriggerOutcome Outcome { get; set; }

    public string? Note { get; set; }

    public Guid? JournalEntryId { get; set; }
    public JournalEntry? JournalEntry { get; set; }
}
