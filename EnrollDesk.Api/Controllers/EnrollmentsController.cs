using EnrollDesk.Api.Auth;
using EnrollDesk.Api.Data;
using EnrollDesk.Api.Domain;
using EnrollDesk.Api.Dtos;
using EnrollDesk.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnrollDesk.Api.Controllers;

[ApiController]
[Route("api/enrollments")]
[Authorize]
public class EnrollmentsController : ControllerBase
{
    private readonly EnrollDeskDbContext _db;
    private readonly IEnrollmentService _enrollmentService;

    public EnrollmentsController(EnrollDeskDbContext db, IEnrollmentService enrollmentService)
    {
        _db = db;
        _enrollmentService = enrollmentService;
    }

    [HttpPost]
    public async Task<ActionResult<EnrollmentResponse>> Enroll(CreateEnrollmentRequest request)
    {
        var userId = User.GetUserId();
        var effectiveOverride = request.OverrideCreditCap && User.IsInRole("Registrar");

        var result = await _enrollmentService.EnrollAsync(userId, request.SectionId, effectiveOverride);
        if (!result.IsSuccess)
        {
            return ServiceResultMapper.ToProblem(result.Outcome, result.Message);
        }

        var enrollment = await LoadForResponseAsync(result.Enrollment!.Id);
        return CreatedAtAction(nameof(GetMine), new { }, ToResponse(enrollment!));
    }

    [HttpGet("mine")]
    public async Task<ActionResult<IReadOnlyList<EnrollmentResponse>>> GetMine()
    {
        var userId = User.GetUserId();
        var enrollments = await _db.Enrollments
            .Include(e => e.User)
            .Include(e => e.Section).ThenInclude(s => s.Course)
            .Include(e => e.Section).ThenInclude(s => s.Term)
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.CreatedAtUtc)
            .ToListAsync();

        return Ok(enrollments.Select(ToResponse).ToList());
    }

    [HttpGet]
    [Authorize(Roles = "Registrar")]
    public async Task<ActionResult<PagedResult<EnrollmentResponse>>> GetAll(
        [FromQuery] int? sectionId, [FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        EnrollmentStatus? statusFilter = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<EnrollmentStatus>(status, true, out var parsed))
            {
                return Problem(statusCode: StatusCodes.Status400BadRequest, title: $"Unknown status '{status}'.");
            }
            statusFilter = parsed;
        }

        var query = _db.Enrollments
            .Include(e => e.User)
            .Include(e => e.Section).ThenInclude(s => s.Course)
            .Include(e => e.Section).ThenInclude(s => s.Term)
            .Where(e => sectionId == null || e.SectionId == sectionId)
            .Where(e => statusFilter == null || e.Status == statusFilter)
            .OrderByDescending(e => e.CreatedAtUtc);

        var totalCount = await query.CountAsync();
        var enrollments = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return Ok(new PagedResult<EnrollmentResponse>(enrollments.Select(ToResponse).ToList(), page, pageSize, totalCount));
    }

    [HttpPost("{id:int}/drop")]
    public async Task<IActionResult> Drop(int id)
    {
        var userId = User.GetUserId();
        var isRegistrar = User.IsInRole("Registrar");

        var result = await _enrollmentService.DropAsync(userId, id, isRegistrar);
        return result.IsSuccess ? Ok(new { result.Outcome, result.Message }) : ServiceResultMapper.ToProblem(result.Outcome, result.Message);
    }

    [HttpPost("{id:int}/grade")]
    [Authorize(Roles = "Registrar")]
    public async Task<IActionResult> Grade(int id, GradeRequest request)
    {
        var result = await _enrollmentService.RecordGradeAsync(id, request.FinalScore);
        return result.IsSuccess ? Ok(new { result.Outcome, result.Message }) : ServiceResultMapper.ToProblem(result.Outcome, result.Message);
    }

    private Task<Enrollment?> LoadForResponseAsync(int enrollmentId) => _db.Enrollments
        .Include(e => e.User)
        .Include(e => e.Section).ThenInclude(s => s.Course)
        .Include(e => e.Section).ThenInclude(s => s.Term)
        .FirstOrDefaultAsync(e => e.Id == enrollmentId);

    private static EnrollmentResponse ToResponse(Enrollment e) => new(
        e.Id,
        e.UserId,
        e.User.FullName,
        e.SectionId,
        e.Section.Course.Code,
        e.Section.SectionNumber,
        e.Section.Term.Name,
        e.Status.ToString(),
        e.WaitlistPosition,
        e.FinalScore,
        e.CreatedAtUtc,
        e.EndedAtUtc);
}
