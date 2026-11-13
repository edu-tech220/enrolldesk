using EnrollDesk.Api.Services;

namespace EnrollDesk.Tests.TestSupport;

public class FakeClock : IClock
{
    public DateTime UtcNow { get; set; } = DateTime.UtcNow;
}
