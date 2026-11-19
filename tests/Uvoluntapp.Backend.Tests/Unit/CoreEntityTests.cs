using System;
using Uvoluntapp.Backend.Domain.Entities.Activities;
using Uvoluntapp.Backend.Domain.Entities.Programs;
using Uvoluntapp.Backend.Domain.Utils.Enums;
using Uvoluntapp.Backend.Domain.Utils.Exceptions;
using Xunit;

namespace Uvoluntapp.Backend.Tests.Unit;

public class CoreEntityTests
{
    private static readonly DateTime _now = new(2026, 11, 19, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void ProgramCanActivateAndDeactivate()
    {
        var program = VolunteerProgram.Create("Voluntariado comunitario", null, "pastoral@ucb.edu.bo", "Sábados 9:00", _now);
        Assert.Equal(ProgramStatus.DRAFT, program.Status);
        Assert.Equal("pastoral@ucb.edu.bo", program.Contact);

        program.Activate(_now);
        Assert.Equal(ProgramStatus.ACTIVE, program.Status);
        program.Deactivate(_now);
        Assert.Equal(ProgramStatus.INACTIVE, program.Status);
        program.Activate(_now);
        Assert.Equal(ProgramStatus.ACTIVE, program.Status);
        Assert.Throws<DomainRuleViolationException>(() => program.Activate(_now));
    }

    [Fact]
    public void ArchivedProgramIsReadOnly()
    {
        var program = VolunteerProgram.Create("Tutorías", "Apoyo académico", null, null, _now);
        program.Archive(_now);

        Assert.True(program.IsArchived);
        Assert.Throws<DomainRuleViolationException>(() => program.UpdateDetails("Otro", null, null, null, _now));
        Assert.Throws<DomainRuleViolationException>(() => program.Activate(_now));
        Assert.Throws<DomainRuleViolationException>(() => VolunteerProgram.Create(" ", null, null, null, _now));
    }

    [Fact]
    public void ActivityRejectsInvalidCancellation()
    {
        var activity = Activity.Create(null, Guid.NewGuid(), Plan(), _now);
        Assert.Throws<DomainRuleViolationException>(() => activity.Cancel(""));
        Assert.Equal(ActivityStatus.DRAFT, activity.Status);
    }

    [Fact]
    public void PublishedActivityCanBeCancelled()
    {
        var activity = Activity.Create(null, Guid.NewGuid(), Plan(), _now);
        activity.Publish(_now);
        Assert.Equal(ActivityStatus.PUBLISHED, activity.Status);

        activity.Cancel("Cierre del campus", _now);
        Assert.Equal(ActivityStatus.CANCELLED, activity.Status);
        Assert.Equal("Cierre del campus", activity.CancellationReason);
        Assert.Throws<DomainRuleViolationException>(() => activity.Publish(_now));
    }

    [Fact]
    public void ConfigurationNeverContradictsItself()
    {
        Assert.Throws<DomainRuleViolationException>(() => Activity.Create(null, Guid.NewGuid(), Plan() with { Capacity = 20 }, _now));
        Assert.Throws<DomainRuleViolationException>(() => Activity.Create(null, Guid.NewGuid(), Plan() with { RequiresRegistration = true, Capacity = 0 }, _now));
        Assert.Throws<DomainRuleViolationException>(() => Activity.Create(null, Guid.NewGuid(), Plan() with { MeetingUrl = "no es un enlace" }, _now));

        var limited = Activity.Create(null, Guid.NewGuid(), Plan() with { RequiresRegistration = true, Capacity = 20 }, _now);
        Assert.Equal(20, limited.Capacity);

        // Quitar el registro previo borra el cupo.
        limited.UpdatePlan(Plan() with { RequiresRegistration = false, Capacity = null }, _now);
        Assert.Null(limited.Capacity);
    }

    [Fact]
    public void DraftAcceptsIncompleteDataButPublishingNeedsLocationOrLinkByModality()
    {
        var hybrid = Activity.Create(null, Guid.NewGuid(), Plan() with { Modality = ActivityModality.HYBRID, MeetingUrl = null }, _now);
        Assert.Throws<DomainRuleViolationException>(() => hybrid.Publish(_now));

        hybrid.UpdatePlan(Plan() with { Modality = ActivityModality.HYBRID, MeetingUrl = "https://meet.example.org/x" }, _now);
        hybrid.Publish(_now);
        Assert.Equal(ActivityStatus.PUBLISHED, hybrid.Status);
    }

    [Fact]
    public void PublishedActivityOnlyChangesInformativeFieldsAndCapacityGrows()
    {
        var activity = Activity.Create(null, Guid.NewGuid(), Plan() with { RequiresRegistration = true, Capacity = 10 }, _now);
        activity.Publish(_now);

        var changed = activity.UpdatePlan(Plan() with { RequiresRegistration = true, Capacity = 15, Location = "Aula 2" }, _now);
        Assert.Contains("capacity", changed);
        Assert.Contains("location", changed);
        Assert.Throws<DomainRuleViolationException>(() => activity.UpdatePlan(Plan() with { RequiresRegistration = true, Capacity = 5 }, _now));
        Assert.Throws<DomainRuleViolationException>(() => activity.UpdatePlan(Plan() with { RequiresRegistration = true, Capacity = 15, Title = "Otro título" }, _now));
        Assert.Throws<DomainRuleViolationException>(() => activity.UpdatePlan(
            Plan() with { RequiresRegistration = true, Capacity = 15, StartsAt = _now.AddDays(3), EndsAt = _now.AddDays(3).AddHours(2) },
            _now));
    }

    [Fact]
    public void PublishedActivityStartsAutomaticallyOnceAndFinishesOnlyByHand()
    {
        var activity = Activity.Create(null, Guid.NewGuid(), Plan(), _now);
        activity.Publish(_now);

        Assert.False(activity.StartIfDue(_now));
        Assert.True(activity.StartIfDue(activity.StartsAt));
        Assert.False(activity.StartIfDue(activity.StartsAt.AddMinutes(1)));
        Assert.Equal(ActivityStatus.IN_PROGRESS, activity.Status);
        Assert.Throws<DomainRuleViolationException>(() => activity.Cancel("tarde", _now));

        activity.Finish(activity.EndsAt.AddHours(5));
        Assert.Equal(ActivityStatus.FINISHED, activity.Status);
        Assert.Throws<DomainRuleViolationException>(() => activity.UpdatePlan(Plan(), _now));
    }

    [Fact]
    public void RescheduleKeepsBeforeAndAfterAndOnlyFromPublished()
    {
        var authorId = Guid.NewGuid();
        var activity = Activity.Create(null, Guid.NewGuid(), Plan(), _now);
        Assert.Throws<DomainRuleViolationException>(() => activity.Reschedule(_now.AddDays(2), _now.AddDays(2).AddHours(1), "lluvia", authorId, _now));

        activity.Publish(_now);
        var previousStart = activity.StartsAt;
        var record = activity.Reschedule(_now.AddDays(2), _now.AddDays(2).AddHours(1), "lluvia", authorId, _now);

        Assert.Equal(previousStart, record.PreviousStartsAt);
        Assert.Equal(_now.AddDays(2), activity.StartsAt);
        Assert.Throws<DomainRuleViolationException>(() => activity.Reschedule(_now.AddDays(3), _now.AddDays(2), "x", authorId, _now));
        Assert.Throws<DomainRuleViolationException>(() => activity.Reschedule(_now.AddDays(3), _now.AddDays(3).AddHours(1), " ", authorId, _now));
    }

    [Fact]
    public void ActivityMovesBetweenProgramsInAnyStateAndOnlyDraftsAreDiscarded()
    {
        var activity = Activity.Create(Guid.NewGuid(), Guid.NewGuid(), Plan(), _now);
        activity.Publish(_now);
        activity.Cancel("sin sede", _now);

        activity.MoveTo(null, _now);
        Assert.Null(activity.ProgramId);
        Assert.Throws<DomainRuleViolationException>(() => activity.MoveTo(null, _now));
        Assert.Throws<DomainRuleViolationException>(() => activity.Discard(_now));

        var draft = Activity.Create(null, Guid.NewGuid(), Plan(), _now);
        draft.Discard(_now);
        Assert.NotNull(draft.DeletedAt);
        Assert.False(draft.IsPublic);
    }

    private static ActivityPlan Plan() => new(
        "Taller de lectura",
        "Lectura guiada",
        ActivityModality.IN_PERSON,
        "Aula 1",
        "https://meet.example.org/taller",
        _now.AddDays(1),
        _now.AddDays(1).AddHours(2),
        false,
        null,
        2,
        null,
        null,
        null);
}
