using EnrollDesk.Api.Data;
using EnrollDesk.Api.Domain;
using EnrollDesk.Api.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnrollDesk.Api.Controllers;

[ApiController]
[Route("api/courses")]
public class CoursesController : ControllerBase
{
    private readonly EnrollDeskDbContext _db;

    public CoursesController(EnrollDeskDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<CourseResponse>>> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _db.Courses
            .Include(c => c.Prerequisites)
            .ThenInclude(p => p.RequiredCourse)
            .OrderBy(c => c.Code);

        var totalCount = await query.CountAsync();
        var courses = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new PagedResult<CourseResponse>(courses.Select(ToResponse).ToList(), page, pageSize, totalCount));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CourseResponse>> GetById(int id)
    {
        var course = await _db.Courses
            .Include(c => c.Prerequisites)
            .ThenInclude(p => p.RequiredCourse)
            .FirstOrDefaultAsync(c => c.Id == id);

        return course is null
            ? Problem(statusCode: StatusCodes.Status404NotFound, title: "Course not found.")
            : Ok(ToResponse(course));
    }

    [HttpPost]
    [Authorize(Roles = "Registrar")]
    public async Task<ActionResult<CourseResponse>> Create(CreateCourseRequest request)
    {
        if (await _db.Courses.AnyAsync(c => c.Code == request.Code))
        {
            return Problem(statusCode: StatusCodes.Status409Conflict, title: $"Course code '{request.Code}' already exists.");
        }

        var course = new Course
        {
            Code = request.Code,
            Title = request.Title,
            Description = request.Description,
            Credits = request.Credits
        };
        _db.Courses.Add(course);
        await _db.SaveChangesAsync();

        if (request.PrerequisiteCodes is { Count: > 0 })
        {
            var required = await _db.Courses.Where(c => request.PrerequisiteCodes.Contains(c.Code)).ToListAsync();
            var missing = request.PrerequisiteCodes.Except(required.Select(c => c.Code)).ToList();
            if (missing.Count > 0)
            {
                return Problem(statusCode: StatusCodes.Status400BadRequest, title: $"Unknown prerequisite course(s): {string.Join(", ", missing)}.");
            }

            foreach (var requiredCourse in required)
            {
                _db.CoursePrerequisites.Add(new CoursePrerequisite { CourseId = course.Id, RequiredCourseId = requiredCourse.Id });
            }
            await _db.SaveChangesAsync();
        }

        await _db.Entry(course).Collection(c => c.Prerequisites).Query().Include(p => p.RequiredCourse).LoadAsync();
        return CreatedAtAction(nameof(GetById), new { id = course.Id }, ToResponse(course));
    }

    private static CourseResponse ToResponse(Course course) => new(
        course.Id,
        course.Code,
        course.Title,
        course.Description,
        course.Credits,
        course.Prerequisites.Select(p => p.RequiredCourse.Code).ToList());
}
