namespace EnrollDesk.Api.Services;

public interface IEnrollmentService
{
    Task<EnrollmentResult> EnrollAsync(int userId, int sectionId, bool overrideCreditCap, CancellationToken ct = default);

    Task<DropResult> DropAsync(int userId, int enrollmentId, bool isRegistrar, CancellationToken ct = default);
}
