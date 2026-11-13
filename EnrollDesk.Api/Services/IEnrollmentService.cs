namespace EnrollDesk.Api.Services;

public interface IEnrollmentService
{
    Task<EnrollmentResult> EnrollAsync(int userId, int sectionId, bool overrideCreditCap, CancellationToken ct = default);

    Task<DropResult> DropAsync(int userId, int enrollmentId, bool isRegistrar, CancellationToken ct = default);

    /// <summary>Registrar-only: records a final score once the term has ended, closing out the enrollment.</summary>
    Task<GradeResult> RecordGradeAsync(int enrollmentId, decimal finalScore, CancellationToken ct = default);
}
