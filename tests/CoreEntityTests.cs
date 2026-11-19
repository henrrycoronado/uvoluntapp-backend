using System;
using U_VoluntApp_Core.Src.Domain.Entities;
using Xunit;
using CoreProgram = U_VoluntApp_Core.Src.Domain.Entities.Program;

namespace U_VoluntApp_Core.Tests;

public class CoreEntityTests
{
    [Fact]
    public void ProgramCanActivateAndDeactivate()
    {
        var program = new CoreProgram();
        Assert.Equal(ProgramStatus.DRAFT, program.Status);

        program.Activate();
        Assert.Equal(ProgramStatus.ACTIVE, program.Status);

        program.Deactivate();
        Assert.Equal(ProgramStatus.INACTIVE, program.Status);

        program.Activate();
        Assert.Equal(ProgramStatus.ACTIVE, program.Status);
    }

    [Fact]
    public void ActivityRejectsInvalidCancellation()
    {
        var activity = new Activity { Modality = ActivityModality.VIRTUAL };
        Assert.Throws<InvalidOperationException>(() => activity.Cancel(""));
        Assert.Equal(ActivityStatus.DRAFT, activity.Status);
    }

    [Fact]
    public void PublishedActivityCanBeCancelled()
    {
        var activity = new Activity { Modality = ActivityModality.HYBRID };
        activity.Publish();
        Assert.Equal(ActivityStatus.PUBLISHED, activity.Status);

        activity.Cancel("Cierre del campus");
        Assert.Equal(ActivityStatus.CANCELLED, activity.Status);
        Assert.Equal("Cierre del campus", activity.CancellationReason);
        Assert.Throws<InvalidOperationException>(activity.Publish);
    }
}
