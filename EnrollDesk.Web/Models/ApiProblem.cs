namespace EnrollDesk.Web.Models;

/// <summary>Mirrors the shape of an RFC 7807 problem response returned by the API.</summary>
public class ApiProblem
{
    public string? Title { get; set; }
    public string? Detail { get; set; }
    public int? Status { get; set; }
}
