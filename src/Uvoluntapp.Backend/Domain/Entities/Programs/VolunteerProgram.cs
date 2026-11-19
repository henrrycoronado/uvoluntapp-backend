using Uvoluntapp.Backend.Domain.Entities.Common;
using Uvoluntapp.Backend.Domain.Utils.Enums;
using Uvoluntapp.Backend.Domain.Utils.Exceptions;

namespace Uvoluntapp.Backend.Domain.Entities.Programs;

/// <summary>
/// Programa de voluntariado (SCRUM-45, SCRUM-103). Nace en borrador; archivar es baja lógica.
/// La ficha tiene nombre, descripción, contacto y horario de referencia.
/// </summary>
public sealed class VolunteerProgram : Entity
{
    public const int NameMaxLength = 150;

    public const int DescriptionMaxLength = 2000;

    public const int ContactMaxLength = 300;

    public const int ReferenceScheduleMaxLength = 500;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? Contact { get; set; }

    public string? ReferenceSchedule { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public ProgramStatus Status { get; private set; } = ProgramStatus.DRAFT;

    public bool IsArchived => DeletedAt is not null;

    public static VolunteerProgram Create(string name, string? description, string? contact, string? referenceSchedule, DateTime nowUtc)
    {
        var program = new VolunteerProgram { CreatedAt = nowUtc };
        program.ApplyDetails(name, description, contact, referenceSchedule);
        return program;
    }

    public void UpdateDetails(string name, string? description, string? contact, string? referenceSchedule, DateTime nowUtc)
    {
        EnsureNotArchived();
        ApplyDetails(name, description, contact, referenceSchedule);
        UpdatedAt = nowUtc;
    }

    public void Activate(DateTime? nowUtc = null)
    {
        EnsureNotArchived();
        if (Status is not (ProgramStatus.DRAFT or ProgramStatus.INACTIVE))
        {
            throw new DomainRuleViolationException("Solo se activa un programa en borrador o inactivo.");
        }

        Status = ProgramStatus.ACTIVE;
        UpdatedAt = nowUtc ?? DateTime.UtcNow;
    }

    public void Deactivate(DateTime? nowUtc = null)
    {
        EnsureNotArchived();
        if (Status != ProgramStatus.ACTIVE)
        {
            throw new DomainRuleViolationException("Solo se desactiva un programa activo.");
        }

        Status = ProgramStatus.INACTIVE;
        UpdatedAt = nowUtc ?? DateTime.UtcNow;
    }

    // Baja lógica: conserva periodos, actividades y asignaciones.
    public void Archive(DateTime nowUtc)
    {
        EnsureNotArchived();
        DeletedAt = nowUtc;
        UpdatedAt = nowUtc;
    }

    private static string? Optional(string? value, int max, string field)
    {
        var text = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        if (text is not null && text.Length > max)
        {
            throw new DomainRuleViolationException($"{field} admite hasta {max} caracteres.");
        }

        return text;
    }

    private void ApplyDetails(string name, string? description, string? contact, string? referenceSchedule)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainRuleViolationException("El nombre del programa es obligatorio.");
        }

        Name = Optional(name, NameMaxLength, "El nombre")!;
        Description = Optional(description, DescriptionMaxLength, "La descripción");
        Contact = Optional(contact, ContactMaxLength, "El contacto");
        ReferenceSchedule = Optional(referenceSchedule, ReferenceScheduleMaxLength, "El horario de referencia");
    }

    private void EnsureNotArchived()
    {
        if (IsArchived)
        {
            throw new DomainRuleViolationException("El programa está archivado.");
        }
    }
}
