namespace Uvoluntapp.Backend.Application.DTOs.Programs;

public sealed record PeriodEditRequest(string Label, DateOnly? EndDate);
