namespace EnrollDesk.Api.Services;

/// <summary>Indirection over the system clock so deadline rules can be tested deterministically.</summary>
public interface IClock
{
    DateTime UtcNow { get; }
}
