namespace EnrollDesk.Api.Domain;

/// <summary>A single scheduled offering of a <see cref="Course"/> within a <see cref="Term"/>.</summary>
public class Section
{
    public int Id { get; set; }

    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;

    public int TermId { get; set; }
    public Term Term { get; set; } = null!;

    /// <summary>e.g. "001" to distinguish parallel sections of the same course.</summary>
    public string SectionNumber { get; set; } = string.Empty;

    public string InstructorName { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;

    public MeetingDays Days { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }

    public int Capacity { get; set; }

    /// <summary>Seats currently occupied by students with <see cref="EnrollmentStatus.Enrolled"/> status.</summary>
    public int SeatsTaken { get; set; }

    /// <summary>Students currently on the FIFO waitlist for this section.</summary>
    public int WaitlistCount { get; set; }

    /// <summary>
    /// Application-managed optimistic concurrency token, incremented on every write.
    /// Uses a plain integer (not a database-generated rowversion) so the same entity
    /// and configuration work unchanged against both SQL Server and SQLite.
    /// </summary>
    public int RowVersion { get; set; }

    public List<Enrollment> Enrollments { get; set; } = new();

    public int SeatsAvailable => Capacity - SeatsTaken;
}
