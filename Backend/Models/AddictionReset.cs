namespace LifeHub.Models;

/// <summary>When abstinence was reset (relapse). Date = calendar day; ResetAt = exact moment (used for "time since" when reset was today).</summary>
public class AddictionReset
{
    public Guid Id { get; set; }

    public Guid AddictionId { get; set; }
    public Addiction? Addiction { get; set; }

    public DateOnly Date { get; set; }

    /// <summary>When the reset was recorded (UTC). Used for "time since last reset" when that day is today.</summary>
    public DateTime ResetAt { get; set; }
}
