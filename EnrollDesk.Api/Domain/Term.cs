namespace EnrollDesk.Api.Domain;

public class Term
{
    public int Id { get; set; }

    /// <summary>Human-readable term label, e.g. "2026-1".</summary>
    public string Name { get; set; } = string.Empty;

    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }

    /// <summary>Last day a drop is free (no record, seat released, no grade).</summary>
    public DateOnly AddDropDeadline { get; set; }

    /// <summary>Last day a student may withdraw (seat released, grade recorded as "W").</summary>
    public DateOnly WithdrawalDeadline { get; set; }

    /// <summary>Maximum credits a student may carry in this term unless a registrar overrides it.</summary>
    public int MaxCreditsPerTerm { get; set; } = 18;

    public bool IsActive { get; set; }

    public List<Section> Sections { get; set; } = new();
}
