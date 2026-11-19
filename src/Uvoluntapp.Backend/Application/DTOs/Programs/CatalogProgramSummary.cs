namespace Uvoluntapp.Backend.Application.DTOs.Programs;

/// <summary>Programa en el catálogo público: sin datos internos (SCRUM-52).</summary>
public sealed record CatalogProgramSummary(Guid Id, string Name, string? Description, string? Contact, string? ReferenceSchedule);
