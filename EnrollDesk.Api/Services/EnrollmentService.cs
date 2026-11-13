using EnrollDesk.Api.Data;
using EnrollDesk.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace EnrollDesk.Api.Services;

public class EnrollmentService : IEnrollmentService
{
    private const int MaxConcurrencyAttempts = 5;

    private readonly EnrollDeskDbContext _db;
    private readonly IClock _clock;

    public EnrollmentService(EnrollDeskDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<EnrollmentResult> EnrollAsync(int userId, int sectionId, bool overrideCreditCap, CancellationToken ct = default)
    {
        var section = await _db.Sections
            .Include(s => s.Course)
            .Include(s => s.Term)
            .FirstOrDefaultAsync(s => s.Id == sectionId, ct);

        if (section is null)
        {
            return new EnrollmentResult(EnrollmentOutcome.SectionNotFound, "Section not found.");
        }

        var alreadyActive = await _db.Enrollments.AnyAsync(
            e => e.UserId == userId && e.SectionId == sectionId &&
                 (e.Status == EnrollmentStatus.Enrolled || e.Status == EnrollmentStatus.Waitlisted), ct);
        if (alreadyActive)
        {
            return new EnrollmentResult(EnrollmentOutcome.AlreadyEnrolled, "You are already enrolled in or waitlisted for this section.");
        }

        var missingPrerequisites = await GetMissingPrerequisiteCodesAsync(userId, section.CourseId, ct);
        if (missingPrerequisites.Count > 0)
        {
            return new EnrollmentResult(
                EnrollmentOutcome.PrerequisiteNotMet,
                $"Missing prerequisite(s): {string.Join(", ", missingPrerequisites)}.");
        }

        var conflictingSection = await FindScheduleConflictAsync(userId, section, ct);
        if (conflictingSection is not null)
        {
            return new EnrollmentResult(EnrollmentOutcome.ScheduleConflict, $"Schedule conflicts with {conflictingSection}.");
        }

        if (!overrideCreditCap)
        {
            var currentLoad = await GetActiveCreditLoadAsync(userId, section.TermId, ct);
            var user = await _db.Users.FindAsync(new object[] { userId }, ct);
            var cap = user?.CreditLoadOverride ?? section.Term.MaxCreditsPerTerm;
            if (currentLoad + section.Course.Credits > cap)
            {
                return new EnrollmentResult(
                    EnrollmentOutcome.CreditCapExceeded,
                    $"Enrolling in {section.Course.Code} would bring your load to {currentLoad + section.Course.Credits} credits, above the {cap}-credit term limit.");
            }
        }

        for (var attempt = 0; attempt < MaxConcurrencyAttempts; attempt++)
        {
            var hasSeat = section.SeatsTaken < section.Capacity;
            var enrollment = new Enrollment
            {
                UserId = userId,
                SectionId = sectionId,
                CreatedAtUtc = _clock.UtcNow,
                Status = hasSeat ? EnrollmentStatus.Enrolled : EnrollmentStatus.Waitlisted,
                WaitlistPosition = hasSeat ? null : section.WaitlistCount + 1
            };

            if (hasSeat)
            {
                section.SeatsTaken++;
            }
            else
            {
                section.WaitlistCount++;
            }
            section.RowVersion++;

            _db.Enrollments.Add(enrollment);

            try
            {
                await _db.SaveChangesAsync(ct);
                return hasSeat
                    ? new EnrollmentResult(EnrollmentOutcome.Enrolled, $"Enrolled in {section.Course.Code}-{section.SectionNumber}.", enrollment)
                    : new EnrollmentResult(EnrollmentOutcome.Waitlisted, $"Section full. Waitlisted at position {enrollment.WaitlistPosition}.", enrollment);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _db.Entry(enrollment).State = EntityState.Detached;
                foreach (var entry in ex.Entries)
                {
                    await entry.ReloadAsync(ct);
                }
            }
        }

        return new EnrollmentResult(EnrollmentOutcome.ConcurrencyConflict, "Could not reserve a seat due to concurrent updates. Please retry.");
    }

    public async Task<DropResult> DropAsync(int userId, int enrollmentId, bool isRegistrar, CancellationToken ct = default)
    {
        var enrollment = await _db.Enrollments
            .Include(e => e.Section)
            .ThenInclude(s => s.Term)
            .FirstOrDefaultAsync(e => e.Id == enrollmentId, ct);

        if (enrollment is null || (enrollment.UserId != userId && !isRegistrar))
        {
            return new DropResult(DropOutcome.NotFound, "Enrollment not found.");
        }

        if (enrollment.Status == EnrollmentStatus.Waitlisted)
        {
            return await LeaveWaitlistAsync(enrollment, ct);
        }

        if (enrollment.Status != EnrollmentStatus.Enrolled)
        {
            return new DropResult(DropOutcome.NotActive, "This enrollment is not currently active.");
        }

        var term = enrollment.Section.Term;
        var today = DateOnly.FromDateTime(_clock.UtcNow);

        EnrollmentStatus newStatus;
        DropOutcome outcome;
        if (isRegistrar || today <= term.AddDropDeadline)
        {
            newStatus = EnrollmentStatus.Dropped;
            outcome = DropOutcome.Dropped;
        }
        else if (today <= term.WithdrawalDeadline)
        {
            newStatus = EnrollmentStatus.Withdrawn;
            outcome = DropOutcome.Withdrawn;
        }
        else
        {
            return new DropResult(
                DropOutcome.NotAllowedPastDeadline,
                $"The withdrawal deadline ({term.WithdrawalDeadline:yyyy-MM-dd}) for this term has passed.");
        }

        var sectionId = enrollment.SectionId;

        for (var attempt = 0; attempt < MaxConcurrencyAttempts; attempt++)
        {
            var section = enrollment.Section;
            enrollment.Status = newStatus;
            enrollment.EndedAtUtc = _clock.UtcNow;
            section.SeatsTaken--;

            Enrollment? promoted = null;
            if (section.WaitlistCount > 0)
            {
                promoted = await _db.Enrollments
                    .Where(e => e.SectionId == sectionId && e.Status == EnrollmentStatus.Waitlisted)
                    .OrderBy(e => e.WaitlistPosition)
                    .FirstOrDefaultAsync(ct);
            }

            if (promoted is not null)
            {
                promoted.Status = EnrollmentStatus.Enrolled;
                promoted.WaitlistPosition = null;
                section.SeatsTaken++;
                section.WaitlistCount--;
            }

            section.RowVersion++;

            try
            {
                await _db.SaveChangesAsync(ct);
                if (promoted is not null)
                {
                    await RenumberWaitlistAsync(sectionId, ct);
                }
                return new DropResult(outcome, outcome == DropOutcome.Dropped ? "Dropped with no record." : "Withdrawn; a 'W' will show on the transcript.");
            }
            catch (DbUpdateConcurrencyException ex)
            {
                foreach (var entry in ex.Entries)
                {
                    await entry.ReloadAsync(ct);
                }
            }
        }

        return new DropResult(DropOutcome.ConcurrencyConflict, "Could not release the seat due to concurrent updates. Please retry.");
    }

    private async Task<DropResult> LeaveWaitlistAsync(Enrollment enrollment, CancellationToken ct)
    {
        var sectionId = enrollment.SectionId;

        for (var attempt = 0; attempt < MaxConcurrencyAttempts; attempt++)
        {
            var section = enrollment.Section;
            enrollment.Status = EnrollmentStatus.Dropped;
            enrollment.EndedAtUtc = _clock.UtcNow;
            section.WaitlistCount--;
            section.RowVersion++;

            try
            {
                await _db.SaveChangesAsync(ct);
                await RenumberWaitlistAsync(sectionId, ct);
                return new DropResult(DropOutcome.LeftWaitlist, "Removed from the waitlist.");
            }
            catch (DbUpdateConcurrencyException ex)
            {
                foreach (var entry in ex.Entries)
                {
                    await entry.ReloadAsync(ct);
                }
            }
        }

        return new DropResult(DropOutcome.ConcurrencyConflict, "Could not leave the waitlist due to concurrent updates. Please retry.");
    }

    /// <summary>
    /// Closes the position gap left by a promoted or departed waitlisted student. Run as a
    /// follow-up pass (outside the Section's optimistic-concurrency retry loop) since it only
    /// renumbers display order and does not affect seat counts.
    /// </summary>
    private async Task RenumberWaitlistAsync(int sectionId, CancellationToken ct)
    {
        var waitlisted = await _db.Enrollments
            .Where(e => e.SectionId == sectionId && e.Status == EnrollmentStatus.Waitlisted)
            .OrderBy(e => e.WaitlistPosition)
            .ToListAsync(ct);

        for (var i = 0; i < waitlisted.Count; i++)
        {
            waitlisted[i].WaitlistPosition = i + 1;
        }

        if (waitlisted.Count > 0)
        {
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task<GradeResult> RecordGradeAsync(int enrollmentId, decimal finalScore, CancellationToken ct = default)
    {
        var enrollment = await _db.Enrollments
            .Include(e => e.Section)
            .ThenInclude(s => s.Term)
            .FirstOrDefaultAsync(e => e.Id == enrollmentId, ct);

        if (enrollment is null)
        {
            return new GradeResult(GradeOutcome.NotFound, "Enrollment not found.");
        }

        if (enrollment.Status != EnrollmentStatus.Enrolled)
        {
            return new GradeResult(GradeOutcome.NotActive, "Only an active enrollment can be graded.");
        }

        var today = DateOnly.FromDateTime(_clock.UtcNow);
        if (today < enrollment.Section.Term.EndDate)
        {
            return new GradeResult(GradeOutcome.TermNotEnded, $"The term does not end until {enrollment.Section.Term.EndDate:yyyy-MM-dd}.");
        }

        enrollment.Status = EnrollmentStatus.Completed;
        enrollment.FinalScore = finalScore;
        enrollment.EndedAtUtc = _clock.UtcNow;
        await _db.SaveChangesAsync(ct);

        return new GradeResult(GradeOutcome.Recorded, "Grade recorded.");
    }

    private async Task<List<string>> GetMissingPrerequisiteCodesAsync(int userId, int courseId, CancellationToken ct)
    {
        var requiredCourseIds = await _db.CoursePrerequisites
            .Where(cp => cp.CourseId == courseId)
            .Select(cp => cp.RequiredCourseId)
            .ToListAsync(ct);

        if (requiredCourseIds.Count == 0)
        {
            return new List<string>();
        }

        var completedCourseIds = await _db.Enrollments
            .Where(e => e.UserId == userId && e.Status == EnrollmentStatus.Completed && e.FinalScore >= Enrollment.PassingScore)
            .Select(e => e.Section.CourseId)
            .Distinct()
            .ToListAsync(ct);

        var missingIds = requiredCourseIds.Except(completedCourseIds).ToList();
        if (missingIds.Count == 0)
        {
            return new List<string>();
        }

        return await _db.Courses.Where(c => missingIds.Contains(c.Id)).Select(c => c.Code).ToListAsync(ct);
    }

    private async Task<string?> FindScheduleConflictAsync(int userId, Section target, CancellationToken ct)
    {
        var activeEnrollments = await _db.Enrollments
            .Include(e => e.Section)
            .ThenInclude(s => s.Course)
            .Where(e => e.UserId == userId && e.SectionId != target.Id && e.Section.TermId == target.TermId &&
                        (e.Status == EnrollmentStatus.Enrolled || e.Status == EnrollmentStatus.Waitlisted))
            .ToListAsync(ct);

        foreach (var enrollment in activeEnrollments)
        {
            var other = enrollment.Section;
            var sameDay = (other.Days & target.Days) != MeetingDays.None;
            var timeOverlaps = other.StartTime < target.EndTime && target.StartTime < other.EndTime;
            if (sameDay && timeOverlaps)
            {
                return $"{other.Course.Code}-{other.SectionNumber}";
            }
        }

        return null;
    }

    private async Task<int> GetActiveCreditLoadAsync(int userId, int termId, CancellationToken ct)
    {
        return await _db.Enrollments
            .Where(e => e.UserId == userId && e.Section.TermId == termId &&
                        (e.Status == EnrollmentStatus.Enrolled || e.Status == EnrollmentStatus.Waitlisted))
            .Select(e => e.Section.Course.Credits)
            .SumAsync(ct);
    }
}
