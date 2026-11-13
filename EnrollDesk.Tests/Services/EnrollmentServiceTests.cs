using EnrollDesk.Api.Data;
using EnrollDesk.Api.Domain;
using EnrollDesk.Api.Services;
using EnrollDesk.Tests.TestSupport;
using Microsoft.Data.Sqlite;
using Xunit;

namespace EnrollDesk.Tests.Services;

public class EnrollmentServiceTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private EnrollDeskDbContext _db = null!;
    private FakeClock _clock = null!;
    private EnrollmentService _sut = null!;

    private Term _term = null!;
    private Course _intro = null!;
    private Course _advanced = null!;
    private Course _math = null!;
    private Section _introSection = null!;
    private Section _overlappingMathSection = null!;
    private Section _advancedSection = null!;
    private User _student = null!;

    public async Task InitializeAsync()
    {
        (_db, _connection) = await SqliteTestDb.CreateAsync();
        _clock = new FakeClock { UtcNow = new DateTime(2099, 1, 10, 0, 0, 0, DateTimeKind.Utc) };
        _sut = new EnrollmentService(_db, _clock);

        _term = new Term
        {
            Name = "2099-1",
            StartDate = new DateOnly(2099, 1, 1),
            EndDate = new DateOnly(2099, 5, 1),
            AddDropDeadline = new DateOnly(2099, 1, 20),
            WithdrawalDeadline = new DateOnly(2099, 3, 1),
            MaxCreditsPerTerm = 8,
            IsActive = true
        };
        _intro = new Course { Code = "CS101", Title = "Intro to Programming", Credits = 4 };
        _advanced = new Course { Code = "CS201", Title = "Data Structures", Credits = 4 };
        _math = new Course { Code = "MATH101", Title = "Calculus I", Credits = 4 };
        _student = new User { FullName = "Jamie Torres", Email = "jamie@test.local", PasswordHash = "x", PasswordSalt = "y", Role = UserRole.Student, CreatedAtUtc = _clock.UtcNow };

        _db.AddRange(_term, _intro, _advanced, _math, _student);
        await _db.SaveChangesAsync();

        _db.CoursePrerequisites.Add(new CoursePrerequisite { CourseId = _advanced.Id, RequiredCourseId = _intro.Id });

        _introSection = new Section
        {
            CourseId = _intro.Id, TermId = _term.Id, SectionNumber = "001",
            Days = MeetingDays.Monday, StartTime = TimeSpan.FromHours(9), EndTime = TimeSpan.FromHours(10),
            Capacity = 1
        };
        _overlappingMathSection = new Section
        {
            CourseId = _math.Id, TermId = _term.Id, SectionNumber = "001",
            Days = MeetingDays.Monday, StartTime = TimeSpan.FromMinutes(9 * 60 + 30), EndTime = TimeSpan.FromHours(11),
            Capacity = 30
        };
        _advancedSection = new Section
        {
            CourseId = _advanced.Id, TermId = _term.Id, SectionNumber = "001",
            Days = MeetingDays.Wednesday, StartTime = TimeSpan.FromHours(9), EndTime = TimeSpan.FromHours(10),
            Capacity = 30
        };

        _db.AddRange(_introSection, _overlappingMathSection, _advancedSection);
        await _db.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        await _connection.DisposeAsync();
    }

    [Fact]
    public async Task Enroll_WhenSeatsAvailable_ReturnsEnrolled()
    {
        var result = await _sut.EnrollAsync(_student.Id, _introSection.Id, overrideCreditCap: false);

        Assert.Equal(EnrollmentOutcome.Enrolled, result.Outcome);
        Assert.Null(result.Enrollment!.WaitlistPosition);
    }

    [Fact]
    public async Task Enroll_WhenSectionFull_ReturnsWaitlisted()
    {
        var other = new User { FullName = "Alex Rivera", Email = "alex@test.local", PasswordHash = "x", PasswordSalt = "y", Role = UserRole.Student, CreatedAtUtc = _clock.UtcNow };
        _db.Users.Add(other);
        await _db.SaveChangesAsync();

        var first = await _sut.EnrollAsync(_student.Id, _introSection.Id, false);
        var second = await _sut.EnrollAsync(other.Id, _introSection.Id, false);

        Assert.Equal(EnrollmentOutcome.Enrolled, first.Outcome);
        Assert.Equal(EnrollmentOutcome.Waitlisted, second.Outcome);
        Assert.Equal(1, second.Enrollment!.WaitlistPosition);
    }

    [Fact]
    public async Task Enroll_WhenAlreadyActive_ReturnsAlreadyEnrolled()
    {
        await _sut.EnrollAsync(_student.Id, _introSection.Id, false);
        var result = await _sut.EnrollAsync(_student.Id, _introSection.Id, false);

        Assert.Equal(EnrollmentOutcome.AlreadyEnrolled, result.Outcome);
    }

    [Fact]
    public async Task Enroll_WhenPrerequisiteMissing_ReturnsPrerequisiteNotMet()
    {
        var result = await _sut.EnrollAsync(_student.Id, _advancedSection.Id, false);

        Assert.Equal(EnrollmentOutcome.PrerequisiteNotMet, result.Outcome);
        Assert.Contains("CS101", result.Message);
    }

    [Fact]
    public async Task Enroll_WhenPrerequisiteCompletedWithPassingScore_Succeeds()
    {
        _db.Enrollments.Add(new Enrollment
        {
            UserId = _student.Id, SectionId = _introSection.Id, Status = EnrollmentStatus.Completed,
            FinalScore = 85m, CreatedAtUtc = _clock.UtcNow, EndedAtUtc = _clock.UtcNow
        });
        await _db.SaveChangesAsync();

        var result = await _sut.EnrollAsync(_student.Id, _advancedSection.Id, false);

        Assert.Equal(EnrollmentOutcome.Enrolled, result.Outcome);
    }

    [Fact]
    public async Task Enroll_WhenScheduleConflicts_ReturnsScheduleConflict()
    {
        await _sut.EnrollAsync(_student.Id, _introSection.Id, false);
        var result = await _sut.EnrollAsync(_student.Id, _overlappingMathSection.Id, false);

        Assert.Equal(EnrollmentOutcome.ScheduleConflict, result.Outcome);
    }

    [Fact]
    public async Task Enroll_WhenCreditCapExceeded_ReturnsCreditCapExceeded()
    {
        _term.MaxCreditsPerTerm = 4;
        _db.Enrollments.Add(new Enrollment
        {
            UserId = _student.Id, SectionId = _introSection.Id, Status = EnrollmentStatus.Completed,
            FinalScore = 90m, CreatedAtUtc = _clock.UtcNow, EndedAtUtc = _clock.UtcNow
        });
        await _db.SaveChangesAsync();

        // A second, unrelated open section fills the 4-credit cap before the advanced course is attempted.
        var fillerSection = new Section
        {
            CourseId = _math.Id, TermId = _term.Id, SectionNumber = "002",
            Days = MeetingDays.Friday, StartTime = TimeSpan.FromHours(9), EndTime = TimeSpan.FromHours(10),
            Capacity = 10
        };
        _db.Sections.Add(fillerSection);
        await _db.SaveChangesAsync();

        await _sut.EnrollAsync(_student.Id, fillerSection.Id, false);
        var result = await _sut.EnrollAsync(_student.Id, _advancedSection.Id, false);

        Assert.Equal(EnrollmentOutcome.CreditCapExceeded, result.Outcome);
    }

    [Fact]
    public async Task Enroll_WhenCreditCapExceededButOverridden_Succeeds()
    {
        _term.MaxCreditsPerTerm = 4;
        await _db.SaveChangesAsync();

        _db.Enrollments.Add(new Enrollment
        {
            UserId = _student.Id, SectionId = _introSection.Id, Status = EnrollmentStatus.Completed,
            FinalScore = 90m, CreatedAtUtc = _clock.UtcNow, EndedAtUtc = _clock.UtcNow
        });
        await _db.SaveChangesAsync();

        var result = await _sut.EnrollAsync(_student.Id, _advancedSection.Id, overrideCreditCap: true);

        Assert.Equal(EnrollmentOutcome.Enrolled, result.Outcome);
    }

    [Fact]
    public async Task Drop_BeforeAddDropDeadline_ReturnsDropped()
    {
        var enrolled = await _sut.EnrollAsync(_student.Id, _introSection.Id, false);

        var result = await _sut.DropAsync(_student.Id, enrolled.Enrollment!.Id, isRegistrar: false);

        Assert.Equal(DropOutcome.Dropped, result.Outcome);
    }

    [Fact]
    public async Task Drop_AfterAddDropBeforeWithdrawalDeadline_ReturnsWithdrawn()
    {
        var enrolled = await _sut.EnrollAsync(_student.Id, _introSection.Id, false);
        _clock.UtcNow = new DateTime(2099, 2, 1, 0, 0, 0, DateTimeKind.Utc);

        var result = await _sut.DropAsync(_student.Id, enrolled.Enrollment!.Id, isRegistrar: false);

        Assert.Equal(DropOutcome.Withdrawn, result.Outcome);
    }

    [Fact]
    public async Task Drop_AfterWithdrawalDeadline_ReturnsNotAllowedPastDeadline()
    {
        var enrolled = await _sut.EnrollAsync(_student.Id, _introSection.Id, false);
        _clock.UtcNow = new DateTime(2099, 3, 15, 0, 0, 0, DateTimeKind.Utc);

        var result = await _sut.DropAsync(_student.Id, enrolled.Enrollment!.Id, isRegistrar: false);

        Assert.Equal(DropOutcome.NotAllowedPastDeadline, result.Outcome);
    }

    [Fact]
    public async Task Drop_AfterWithdrawalDeadlineByRegistrar_StillDrops()
    {
        var enrolled = await _sut.EnrollAsync(_student.Id, _introSection.Id, false);
        _clock.UtcNow = new DateTime(2099, 3, 15, 0, 0, 0, DateTimeKind.Utc);

        var result = await _sut.DropAsync(_student.Id, enrolled.Enrollment!.Id, isRegistrar: true);

        Assert.Equal(DropOutcome.Dropped, result.Outcome);
    }

    [Fact]
    public async Task Drop_PromotesNextWaitlistedStudent()
    {
        var other = new User { FullName = "Alex Rivera", Email = "alex@test.local", PasswordHash = "x", PasswordSalt = "y", Role = UserRole.Student, CreatedAtUtc = _clock.UtcNow };
        _db.Users.Add(other);
        await _db.SaveChangesAsync();

        var first = await _sut.EnrollAsync(_student.Id, _introSection.Id, false);
        var waitlisted = await _sut.EnrollAsync(other.Id, _introSection.Id, false);
        Assert.Equal(EnrollmentOutcome.Waitlisted, waitlisted.Outcome);

        await _sut.DropAsync(_student.Id, first.Enrollment!.Id, isRegistrar: false);

        var promoted = await _db.Enrollments.FindAsync(waitlisted.Enrollment!.Id);
        Assert.Equal(EnrollmentStatus.Enrolled, promoted!.Status);
        Assert.Null(promoted.WaitlistPosition);

        var section = await _db.Sections.FindAsync(_introSection.Id);
        Assert.Equal(1, section!.SeatsTaken);
        Assert.Equal(0, section.WaitlistCount);
    }

    [Fact]
    public async Task RecordGrade_BeforeTermEnds_ReturnsTermNotEnded()
    {
        var enrolled = await _sut.EnrollAsync(_student.Id, _introSection.Id, false);

        var result = await _sut.RecordGradeAsync(enrolled.Enrollment!.Id, 95m);

        Assert.Equal(GradeOutcome.TermNotEnded, result.Outcome);
    }

    [Fact]
    public async Task RecordGrade_AfterTermEnds_MarksCompleted()
    {
        var enrolled = await _sut.EnrollAsync(_student.Id, _introSection.Id, false);
        _clock.UtcNow = new DateTime(2099, 5, 2, 0, 0, 0, DateTimeKind.Utc);

        var result = await _sut.RecordGradeAsync(enrolled.Enrollment!.Id, 95m);

        Assert.Equal(GradeOutcome.Recorded, result.Outcome);
        var graded = await _db.Enrollments.FindAsync(enrolled.Enrollment!.Id);
        Assert.Equal(EnrollmentStatus.Completed, graded!.Status);
        Assert.Equal(95m, graded.FinalScore);
    }

    [Fact]
    public async Task Enroll_ConcurrentRequests_NeverOversellsTheSection()
    {
        _introSection.Capacity = 2;
        await _db.SaveChangesAsync();

        var students = new List<User>();
        for (var i = 0; i < 5; i++)
        {
            var user = new User { FullName = $"Student {i}", Email = $"student{i}@test.local", PasswordHash = "x", PasswordSalt = "y", Role = UserRole.Student, CreatedAtUtc = _clock.UtcNow };
            students.Add(user);
            _db.Users.Add(user);
        }
        await _db.SaveChangesAsync();

        var tasks = students.Select(async student =>
        {
            await using var context = SqliteTestDb.CreateContext(_connection);
            var service = new EnrollmentService(context, _clock);
            return await service.EnrollAsync(student.Id, _introSection.Id, false);
        });

        var results = await Task.WhenAll(tasks);

        Assert.Equal(2, results.Count(r => r.Outcome == EnrollmentOutcome.Enrolled));
        Assert.Equal(3, results.Count(r => r.Outcome == EnrollmentOutcome.Waitlisted));

        await using var verification = SqliteTestDb.CreateContext(_connection);
        var section = await verification.Sections.FindAsync(_introSection.Id);
        Assert.Equal(2, section!.SeatsTaken);
        Assert.Equal(3, section.WaitlistCount);

        var waitlistPositions = verification.Enrollments
            .Where(e => e.SectionId == _introSection.Id && e.Status == EnrollmentStatus.Waitlisted)
            .Select(e => e.WaitlistPosition!.Value)
            .OrderBy(p => p)
            .ToList();
        Assert.Equal(new List<int> { 1, 2, 3 }, waitlistPositions);
    }
}
