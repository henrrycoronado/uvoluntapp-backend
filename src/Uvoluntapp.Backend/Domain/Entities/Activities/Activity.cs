using Uvoluntapp.Backend.Domain.Entities.Common;
using Uvoluntapp.Backend.Domain.Utils.Enums;
using Uvoluntapp.Backend.Domain.Utils.Exceptions;

namespace Uvoluntapp.Backend.Domain.Entities.Activities;

/// <summary>
/// Actividad de un programa o libre (SCRUM-47 a 51, 53, 123). No hay tipo de actividad:
/// la diferencia la da su configuración, que nunca se contradice (SCRUM-49).
/// </summary>
public sealed class Activity : Entity
{
    public const int TitleMaxLength = 200;

    public const int TextMaxLength = 2000;

    public const int LocationMaxLength = 300;

    public const int UrlMaxLength = 500;

    public const int GroupLabelMaxLength = 100;

    public List<MediaAsset> Images { get; } = [];

    public Guid? ProgramId { get; init; }

    public Guid AcademicPeriodId { get; init; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public required ActivityModality Modality { get; set; }

    public string? Location { get; set; }

    public string? MeetingUrl { get; set; }

    public DateTime StartsAt { get; set; }

    public DateTime EndsAt { get; set; }

    public bool RequiresRegistration { get; set; }

    public int? Capacity { get; set; }

    public decimal? ExpectedHours { get; set; }

    public string? ParticipationConditions { get; set; }

    public List<ActivityGroup> Groups { get; } = [];

    public bool AllowMultipleGroups { get; private set; }

    public decimal? Latitude { get; private set; }

    public decimal? Longitude { get; private set; }

    public int? GeofenceRadiusM { get; private set; }

    public bool AllowSelfCheckin { get; private set; }

    public int CheckinToleranceMin { get; private set; }

    // Etiqueta informativa heredada de la planificación.
    public string? GroupLabel { get; set; }

    public string? CancellationReason { get; private set; }

    public ActivityStatus Status { get; private set; } = ActivityStatus.DRAFT;

    public DateTime? StartedAt { get; private set; }

    public DateTime? FinishedAt { get; private set; }

    public bool IsPublic => DeletedAt is null && Status != ActivityStatus.DRAFT;

    public static Activity Create(Guid? programId, Guid periodId, ActivityPlan plan, DateTime nowUtc)
    {
        var activity = new Activity
        {
            ProgramId = programId,
            AcademicPeriodId = periodId,
            Modality = plan.Modality,
            CreatedAt = nowUtc,
        };
        activity.ApplyPlan(plan);
        activity.SetSchedule(plan.StartsAt, plan.EndsAt);
        return activity;
    }

    public bool AcceptsEnrollment(DateTime nowUtc) =>
        RequiresRegistration && Status == ActivityStatus.PUBLISHED && StartsAt > nowUtc && DeletedAt == null;

    public void ConfigureParticipation(List<ActivityGroup> groups, bool multiple, decimal? latitude, decimal? longitude, int? radius, bool selfCheckin, int tolerance, DateTime now)
    {
        EnsureNotDeleted();
        if (Status != ActivityStatus.DRAFT)
        {
            throw new DomainRuleViolationException("Solo se configura la participación en borrador.");
        }

        if ((latitude is null) != (longitude is null) || latitude is < -90 or > 90 || longitude is < -180 or > 180)
        {
            throw new DomainRuleViolationException("Las coordenadas del lugar deben formar un par válido.");
        }

        if (!RequiresRegistration && (groups.Count > 0 || multiple || radius.HasValue || selfCheckin || tolerance != 0))
        {
            throw new DomainRuleViolationException("SIMPLE no permite configuración de inscripción ni automarcación.");
        }

        if ((multiple && groups.Count == 0) || (groups.Count > 0 && Capacity.HasValue))
        {
            throw new DomainRuleViolationException("En actividades grupales el cupo se configura por grupo.");
        }

        if (radius is <= 0 || (radius.HasValue && latitude is null) || tolerance < 0)
        {
            throw new DomainRuleViolationException("Radio, coordenadas o tolerancia inválidos.");
        }

        foreach (var group in groups)
        {
            group.Validate(StartsAt, EndsAt);
        }

        // Conservar identificadores de turnos que llegan iguales en un reintento del PUT.
        var obsolete = Groups.ToList();
        foreach (var group in groups)
        {
            var match = obsolete.Find(g => g.Name == group.Name && g.StartsAt == group.StartsAt && g.EndsAt == group.EndsAt && g.Capacity == group.Capacity);
            if (match == null)
            {
                Groups.Add(group);
            }
            else
            {
                obsolete.Remove(match);
            }
        }

        foreach (var removed in obsolete)
        {
            Groups.Remove(removed);
        }

        AllowMultipleGroups = multiple;
        Latitude = latitude;
        Longitude = longitude;
        GeofenceRadiusM = radius;
        AllowSelfCheckin = selfCheckin;
        CheckinToleranceMin = tolerance;
        UpdatedAt = now;
    }

    /// <summary>Edición de planificación según el estado (SCRUM-48).</summary>
    /// <param name="plan">Datos completos de planificación.</param>
    /// <param name="nowUtc">Momento de la edición.</param>
    /// <returns>Los nombres de los campos que cambiaron, para la auditoría.</returns>
    public IReadOnlyList<string> UpdatePlan(ActivityPlan plan, DateTime nowUtc)
    {
        EnsureNotDeleted();
        if (Status is ActivityStatus.FINISHED or ActivityStatus.CANCELLED)
        {
            throw new DomainRuleViolationException("Una actividad finalizada o cancelada no admite cambios de planificación.");
        }

        var before = Snapshot();
        if (Status == ActivityStatus.DRAFT)
        {
            ApplyPlan(plan);
            SetSchedule(plan.StartsAt, plan.EndsAt);
        }
        else
        {
            // Publicada o en ejecución: solo campos informativos; el horario va por reprogramación.
            if (Clean(plan.Title, TitleMaxLength, "El título") != Title
                || Clean(plan.MeetingUrl, UrlMaxLength, "El enlace") != MeetingUrl
                || plan.ExpectedHours != ExpectedHours
                || Clean(plan.ParticipationConditions, TextMaxLength, "Los requisitos") != ParticipationConditions
                || Clean(plan.GroupLabel, GroupLabelMaxLength, "La etiqueta") != GroupLabel
                || plan.Modality != Modality
                || plan.RequiresRegistration != RequiresRegistration
                || plan.StartsAt != StartsAt
                || plan.EndsAt != EndsAt)
            {
                throw new DomainRuleViolationException(
                    "Una actividad publicada solo cambia descripción, lugar y cupo (solo hacia arriba). El horario se cambia reprogramando.");
            }

            if (!CapacityOnlyGrows(Capacity, plan.Capacity))
            {
                throw new DomainRuleViolationException("En una actividad publicada el cupo solo puede aumentar.");
            }

            ApplyPlan(plan);
            EnsurePublishable();
        }

        UpdatedAt = nowUtc;
        var after = Snapshot();
        return before.Where(pair => !Equals(pair.Value, after[pair.Key])).Select(pair => pair.Key).ToList();
    }

    public void Publish(DateTime? nowUtc = null)
    {
        EnsureNotDeleted();
        if (Status != ActivityStatus.DRAFT)
        {
            throw new DomainRuleViolationException("Solo se publica una actividad en borrador.");
        }

        EnsurePublishable();
        var now = nowUtc ?? DateTime.UtcNow;
        if (EndsAt <= now)
        {
            throw new DomainRuleViolationException("No se publica una actividad cuyo horario ya terminó.");
        }

        Status = ActivityStatus.PUBLISHED;
        UpdatedAt = now;
    }

    /// <summary>Inicio automático al llegar su horario (SCRUM-50). Es idempotente.</summary>
    /// <param name="nowUtc">Momento actual.</param>
    /// <returns>Verdadero si la actividad pasó a en ejecución.</returns>
    public bool StartIfDue(DateTime nowUtc)
    {
        if (DeletedAt is not null || Status != ActivityStatus.PUBLISHED || StartsAt > nowUtc)
        {
            return false;
        }

        Status = ActivityStatus.IN_PROGRESS;
        StartedAt = nowUtc;
        return true;
    }

    public void Cancel(string reason, DateTime? nowUtc = null)
    {
        EnsureNotDeleted();
        if (Status is not (ActivityStatus.DRAFT or ActivityStatus.PUBLISHED) || string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainRuleViolationException("Solo se cancela una actividad en borrador o publicada, con motivo.");
        }

        CancellationReason = reason.Trim();
        Status = ActivityStatus.CANCELLED;
        UpdatedAt = nowUtc ?? DateTime.UtcNow;
    }

    // No hay finalización automática: la decide un responsable.
    public void Finish(DateTime? nowUtc = null)
    {
        EnsureNotDeleted();
        if (Status != ActivityStatus.IN_PROGRESS)
        {
            throw new DomainRuleViolationException("Solo se finaliza una actividad en ejecución.");
        }

        var now = nowUtc ?? DateTime.UtcNow;
        Status = ActivityStatus.FINISHED;
        FinishedAt = now;
        UpdatedAt = now;
    }

    // Descartar un borrador es una baja lógica.
    public void Discard(DateTime nowUtc)
    {
        EnsureNotDeleted();
        if (Status != ActivityStatus.DRAFT)
        {
            throw new DomainRuleViolationException("Solo se descarta una actividad en borrador.");
        }

        DeletedAt = nowUtc;
        UpdatedAt = nowUtc;
    }

    public ActivityReschedule Reschedule(DateTime newStartsAt, DateTime newEndsAt, string reason, Guid authorId, DateTime nowUtc)
    {
        EnsureNotDeleted();
        if (Status != ActivityStatus.PUBLISHED)
        {
            throw new DomainRuleViolationException("Solo se reprograma una actividad publicada.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainRuleViolationException("El motivo de la reprogramación es obligatorio.");
        }

        if (newEndsAt <= nowUtc)
        {
            throw new DomainRuleViolationException("El nuevo horario ya terminó.");
        }

        var record = new ActivityReschedule
        {
            ActivityId = Id,
            AuthorId = authorId,
            PreviousStartsAt = StartsAt,
            PreviousEndsAt = EndsAt,
            NewStartsAt = newStartsAt,
            NewEndsAt = newEndsAt,
            Reason = reason.Trim(),
        };
        var shift = newStartsAt - StartsAt;
        foreach (var group in Groups)
        {
            group.StartsAt += shift;
            group.EndsAt += shift;
            group.Validate(newStartsAt, newEndsAt);
        }

        SetSchedule(newStartsAt, newEndsAt);
        UpdatedAt = nowUtc;
        return record;
    }

    private static bool CapacityOnlyGrows(int? current, int? next) =>
        next is null || (current is not null && next >= current);

    private static string? Clean(string? value, int max, string field)
    {
        var text = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        if (text is not null && text.Length > max)
        {
            throw new DomainRuleViolationException($"{field} admite hasta {max} caracteres.");
        }

        return text;
    }

    private void ApplyPlan(ActivityPlan plan)
    {
        var title = Clean(plan.Title, TitleMaxLength, "El título")
            ?? throw new DomainRuleViolationException("El título es obligatorio.");
        if (plan.Capacity is < 0)
        {
            throw new DomainRuleViolationException("El cupo debe ser un entero no negativo o quedar vacío (sin límite).");
        }

        if (plan.Capacity is not null && !plan.RequiresRegistration)
        {
            throw new DomainRuleViolationException("El cupo solo existe si la actividad requiere registro previo.");
        }

        if (plan.ExpectedHours is < 0 or > 1000)
        {
            throw new DomainRuleViolationException("Las horas previstas deben estar entre 0 y 1000.");
        }

        var url = Clean(plan.MeetingUrl, UrlMaxLength, "El enlace");
        if (url is not null && !(Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme is "https" or "http"))
        {
            throw new DomainRuleViolationException("El enlace debe ser una dirección web válida.");
        }

        if ((!plan.RequiresRegistration && (Groups.Count > 0 || AllowSelfCheckin || GeofenceRadiusM.HasValue || CheckinToleranceMin != 0)) || (Groups.Count > 0 && plan.Capacity.HasValue))
        {
            throw new DomainRuleViolationException("Ajusta primero la configuración de participación.");
        }

        foreach (var group in Groups)
        {
            group.Validate(plan.StartsAt, plan.EndsAt);
        }

        Title = title;
        Description = Clean(plan.Description, TextMaxLength, "La descripción");
        Modality = plan.Modality;
        Location = Clean(plan.Location, LocationMaxLength, "El lugar");
        MeetingUrl = url;
        RequiresRegistration = plan.RequiresRegistration;
        Capacity = plan.RequiresRegistration ? plan.Capacity : null;
        ExpectedHours = plan.ExpectedHours;
        ParticipationConditions = Clean(plan.ParticipationConditions, TextMaxLength, "Las condiciones");

        GroupLabel = Clean(plan.GroupLabel, GroupLabelMaxLength, "La etiqueta de grupo");
    }

    private void SetSchedule(DateTime startsAt, DateTime endsAt)
    {
        if (endsAt <= startsAt)
        {
            throw new DomainRuleViolationException("El fin debe ser posterior al inicio.");
        }

        StartsAt = startsAt;
        EndsAt = endsAt;
    }

    // Lo mínimo para publicar: lugar o enlace según la modalidad.
    private void EnsurePublishable()
    {
        if (Modality is ActivityModality.IN_PERSON or ActivityModality.HYBRID && Location is null)
        {
            throw new DomainRuleViolationException("Una actividad presencial o híbrida necesita lugar.");
        }

        if (Modality is ActivityModality.VIRTUAL or ActivityModality.HYBRID && MeetingUrl is null)
        {
            throw new DomainRuleViolationException("Una actividad virtual o híbrida necesita enlace.");
        }
    }

    private void EnsureNotDeleted()
    {
        if (DeletedAt is not null)
        {
            throw new DomainRuleViolationException("La actividad fue descartada.");
        }
    }

    private Dictionary<string, object?> Snapshot() => new()
    {
        ["title"] = Title,
        ["description"] = Description,
        ["modality"] = Modality,
        ["location"] = Location,
        ["meetingUrl"] = MeetingUrl,
        ["startsAt"] = StartsAt,
        ["endsAt"] = EndsAt,
        ["requiresRegistration"] = RequiresRegistration,
        ["capacity"] = Capacity,
        ["expectedHours"] = ExpectedHours,
        ["participationConditions"] = ParticipationConditions,

        ["groupLabel"] = GroupLabel,
    };
}
