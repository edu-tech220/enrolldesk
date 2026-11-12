namespace EnrollDesk.Api.Domain;

public class User
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string PasswordSalt { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Student;

    /// <summary>Per-student override of the term credit cap set by a registrar. Null means the global default applies.</summary>
    public int? CreditLoadOverride { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public List<Enrollment> Enrollments { get; set; } = new();
}
