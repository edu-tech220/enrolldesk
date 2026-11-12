namespace EnrollDesk.Api.Domain;

/// <summary>Links a <see cref="User"/> (student) to a <see cref="Section"/> for one term.</summary>
public class Enrollment
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int SectionId { get; set; }
    public Section Section { get; set; } = null!;

    public EnrollmentStatus Status { get; set; }

    /// <summary>1-based FIFO position while <see cref="Status"/> is <see cref="EnrollmentStatus.Waitlisted"/>; null otherwise.</summary>
    public int? WaitlistPosition { get; set; }

    /// <summary>Final score on a 0-100 scale, set once the course is graded. 60 or above counts as passing.</summary>
    public decimal? FinalScore { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public DateTime? EndedAtUtc { get; set; }

    public const decimal PassingScore = 60m;
}
