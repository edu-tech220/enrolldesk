using EnrollDesk.Api.Data;
using EnrollDesk.Api.Domain;
using EnrollDesk.Api.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnrollDesk.Api.Controllers;

[ApiController]
[Route("api/terms")]
public class TermsController : ControllerBase
{
    private readonly EnrollDeskDbContext _db;

    public TermsController(EnrollDeskDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TermResponse>>> GetAll()
    {
        var terms = await _db.Terms.OrderByDescending(t => t.StartDate).ToListAsync();
        return Ok(terms.Select(ToResponse).ToList());
    }

    [HttpGet("active")]
    public async Task<ActionResult<TermResponse>> GetActive()
    {
        var term = await _db.Terms.FirstOrDefaultAsync(t => t.IsActive);
        return term is null
            ? Problem(statusCode: StatusCodes.Status404NotFound, title: "No active term is configured.")
            : Ok(ToResponse(term));
    }

    [HttpPost]
    [Authorize(Roles = "Registrar")]
    public async Task<ActionResult<TermResponse>> Create(CreateTermRequest request)
    {
        if (await _db.Terms.AnyAsync(t => t.Name == request.Name))
        {
            return Problem(statusCode: StatusCodes.Status409Conflict, title: $"Term '{request.Name}' already exists.");
        }

        var term = new Term
        {
            Name = request.Name,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            AddDropDeadline = request.AddDropDeadline,
            WithdrawalDeadline = request.WithdrawalDeadline,
            MaxCreditsPerTerm = request.MaxCreditsPerTerm,
            IsActive = request.IsActive
        };

        _db.Terms.Add(term);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAll), new { }, ToResponse(term));
    }

    private static TermResponse ToResponse(Term term) => new(
        term.Id, term.Name, term.StartDate, term.EndDate, term.AddDropDeadline, term.WithdrawalDeadline, term.MaxCreditsPerTerm, term.IsActive);
}
