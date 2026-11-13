using EnrollDesk.Api.Data;
using EnrollDesk.Api.Domain;
using EnrollDesk.Api.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnrollDesk.Api.Controllers;

[ApiController]
[Route("api/sections")]
public class SectionsController : ControllerBase
{
    private readonly EnrollDeskDbContext _db;

    public SectionsController(EnrollDeskDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<SectionResponse>>> GetAll(
        [FromQuery] int? termId, [FromQuery] int? courseId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _db.Sections
            .Include(s => s.Course)
            .Include(s => s.Term)
            .Where(s => termId == null || s.TermId == termId)
            .Where(s => courseId == null || s.CourseId == courseId)
            .OrderBy(s => s.Course.Code).ThenBy(s => s.SectionNumber);

        var totalCount = await query.CountAsync();
        var sections = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return Ok(new PagedResult<SectionResponse>(sections.Select(ToResponse).ToList(), page, pageSize, totalCount));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<SectionResponse>> GetById(int id)
    {
        var section = await _db.Sections.Include(s => s.Course).Include(s => s.Term).FirstOrDefaultAsync(s => s.Id == id);
        return section is null
            ? Problem(statusCode: StatusCodes.Status404NotFound, title: "Section not found.")
            : Ok(ToResponse(section));
    }

    [HttpPost]
    [Authorize(Roles = "Registrar")]
    public async Task<ActionResult<SectionResponse>> Create(CreateSectionRequest request)
    {
        var course = await _db.Courses.FindAsync(request.CourseId);
        if (course is null)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: $"Course {request.CourseId} does not exist.");
        }

        var term = await _db.Terms.FindAsync(request.TermId);
        if (term is null)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: $"Term {request.TermId} does not exist.");
        }

        MeetingDays days;
        try
        {
            days = MeetingDaysMapper.Parse(request.Days);
        }
        catch (ArgumentException ex)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: ex.Message);
        }

        var duplicate = await _db.Sections.AnyAsync(s =>
            s.CourseId == request.CourseId && s.TermId == request.TermId && s.SectionNumber == request.SectionNumber);
        if (duplicate)
        {
            return Problem(statusCode: StatusCodes.Status409Conflict, title: "A section with this number already exists for that course and term.");
        }

        var section = new Section
        {
            CourseId = request.CourseId,
            TermId = request.TermId,
            SectionNumber = request.SectionNumber,
            InstructorName = request.InstructorName,
            Location = request.Location,
            Days = days,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            Capacity = request.Capacity,
            SeatsTaken = 0,
            WaitlistCount = 0,
            RowVersion = 0
        };

        _db.Sections.Add(section);
        await _db.SaveChangesAsync();

        section.Course = course;
        section.Term = term;
        return CreatedAtAction(nameof(GetById), new { id = section.Id }, ToResponse(section));
    }

    private static SectionResponse ToResponse(Section section) => new(
        section.Id,
        section.CourseId,
        section.Course.Code,
        section.Course.Title,
        section.TermId,
        section.Term.Name,
        section.SectionNumber,
        section.InstructorName,
        section.Location,
        MeetingDaysMapper.ToNames(section.Days),
        section.StartTime,
        section.EndTime,
        section.Capacity,
        section.SeatsTaken,
        section.SeatsAvailable,
        section.WaitlistCount);
}
