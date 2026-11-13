namespace EnrollDesk.Api.Dtos;

public record TermResponse(
    int Id,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    DateOnly AddDropDeadline,
    DateOnly WithdrawalDeadline,
    int MaxCreditsPerTerm,
    bool IsActive);

public record CreateTermRequest(
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    DateOnly AddDropDeadline,
    DateOnly WithdrawalDeadline,
    int MaxCreditsPerTerm,
    bool IsActive);
