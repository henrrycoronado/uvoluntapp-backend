using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Uvoluntapp.Backend.Application.DTOs.Accounts;
using Uvoluntapp.Backend.Application.Services.Activities;
using Uvoluntapp.Backend.Domain.Entities.Accounts;
using Uvoluntapp.Backend.Domain.Entities.Programs;
using Uvoluntapp.Backend.Domain.Utils.Enums;
using Uvoluntapp.Backend.Infrastructure.Identity;
using Uvoluntapp.Backend.Tests.Integration.Accounts;
using Xunit;

namespace Uvoluntapp.Backend.Tests.Integration.Planning;

/// <summary>Matriz de acceso y flujos de M2 contra la API real (SCRUM-45 a 54, 103, 122, 123).</summary>
public sealed class PlanningFlowTests(AuthFixture fixture) : IClassFixture<AuthFixture>
{
    [Fact]
    public async Task ProgramsFollowTheRoleMatrix()
    {
        var admin = await ClientAsync(AssignedRole.ADMIN, RoleScope.GLOBAL, null);
        var programId = await CreateProgramAsync(admin);
        var otherProgramId = await CreateProgramAsync(admin);
        var leader = await ClientAsync(AssignedRole.PROGRAM_LEADER, RoleScope.PROGRAM, programId);
        var operative = await ClientAsync(AssignedRole.OPERATIONAL_LEADER, RoleScope.PROGRAM, programId);
        var volunteer = await ClientAsync(null, RoleScope.GLOBAL, null);

        // Solo ADMIN crea programas.
        Assert.Equal(HttpStatusCode.Forbidden, (await leader.Http.PostAsJsonAsync("/api/v1/programs", new { name = "X" })).StatusCode);

        // El líder gestiona su programa, no otro; el operativo y el voluntario no gestionan.
        var edit = new { name = "Comunitario", contact = "pastoral@ucb.edu.bo", referenceSchedule = "Sábados 9:00" };
        var edited = await Json(await leader.Http.PatchAsJsonAsync($"/api/v1/programs/{programId}", edit), HttpStatusCode.OK);
        Assert.Equal("Sábados 9:00", edited.GetProperty("referenceSchedule").GetString());
        Assert.True(edited.GetProperty("canManage").GetBoolean());
        Assert.Equal(HttpStatusCode.Forbidden, (await leader.Http.PatchAsJsonAsync($"/api/v1/programs/{otherProgramId}", edit)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await operative.Http.PatchAsJsonAsync($"/api/v1/programs/{programId}", edit)).StatusCode);

        // Líderes y operativos ven todos los programas (también borradores); el voluntario solo los activos.
        var seenByOperative = await Json(await operative.Http.GetAsync($"/api/v1/programs/{otherProgramId}"), HttpStatusCode.OK);
        Assert.False(seenByOperative.GetProperty("canManage").GetBoolean());
        Assert.Equal(HttpStatusCode.NotFound, (await volunteer.Http.GetAsync($"/api/v1/programs/{programId}")).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await leader.Http.PostAsync($"/api/v1/programs/{programId}/activate", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await volunteer.Http.GetAsync($"/api/v1/programs/{programId}")).StatusCode);

        // Archivar exige motivo y es una baja lógica.
        Assert.Equal(HttpStatusCode.Forbidden, (await leader.Http.PostAsJsonAsync($"/api/v1/programs/{otherProgramId}/archive", new { reason = "Duplicado" })).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await admin.Http.PostAsJsonAsync($"/api/v1/programs/{otherProgramId}/archive", new { reason = " " })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.Http.PostAsJsonAsync($"/api/v1/programs/{otherProgramId}/archive", new { reason = "Duplicado" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await leader.Http.GetAsync($"/api/v1/programs/{otherProgramId}")).StatusCode);

        // Los rechazos quedan auditados con su motivo (SCRUM-50, criterio 6).
        await using var db = fixture.Context();
        Assert.True(await db.AuditEvents.AnyAsync(e => e.Action == "PLANNING_OPERATION_REJECTED" && e.ActorId == leader.UserId && e.ResourceId == otherProgramId));
        Assert.True(await db.AuditEvents.AnyAsync(e => e.Action == "PLANNING_OPERATION_REJECTED" && e.ResourceId == otherProgramId && e.ActorId != leader.UserId && e.ActorId != operative.UserId));
    }

    [Fact]
    public async Task ActivitiesFollowTheRoleMatrixAndTheCatalogShowsOnlyPublicData()
    {
        var admin = await ClientAsync(AssignedRole.ADMIN, RoleScope.GLOBAL, null);
        var programId = await CreateProgramAsync(admin, activate: true);
        var otherProgramId = await CreateProgramAsync(admin, activate: true);
        var leader = await ClientAsync(AssignedRole.PROGRAM_LEADER, RoleScope.PROGRAM, programId);
        var operative = await ClientAsync(AssignedRole.OPERATIONAL_LEADER, RoleScope.PROGRAM, programId);
        var period = await PeriodAsync();

        // El líder crea en su programa; no en otro ni actividades libres.
        var activityId = await CreateActivityAsync(leader, programId, period, HttpStatusCode.Created);
        await CreateActivityAsync(leader, otherProgramId, period, HttpStatusCode.Forbidden);
        await CreateActivityAsync(leader, null, period, HttpStatusCode.Forbidden);
        var freeId = await CreateActivityAsync(admin, null, period, HttpStatusCode.Created, groupLabel: "Campaña de Navidad");

        // El operativo ve el borrador de su programa pero no lo gestiona.
        Assert.Equal(HttpStatusCode.NotFound, (await operative.Http.GetAsync($"/api/v1/activities/{activityId}")).StatusCode);
        await leader.Http.PostAsJsonAsync("/api/v1/role-assignments", new { userId = operative.UserId, role = "OPERATIONAL_LEADER", scope = "ACTIVITY", scopeId = activityId });
        var draft = await Json(await operative.Http.GetAsync($"/api/v1/activities/{activityId}"), HttpStatusCode.OK);
        Assert.False(draft.GetProperty("canManage").GetBoolean());
        Assert.Equal(HttpStatusCode.Forbidden, (await operative.Http.PostAsync($"/api/v1/activities/{activityId}/publish", null)).StatusCode);

        // El cupo sin registro previo es una contradicción.
        var contradictory = await leader.Http.PatchAsJsonAsync($"/api/v1/activities/{activityId}", ActivityBody(programId, period, capacity: 10, requiresRegistration: false));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, contradictory.StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await leader.Http.PostAsync($"/api/v1/activities/{activityId}/publish", null)).StatusCode);

        // Catálogo sin sesión: lo publicado aparece sin enlace; el borrador libre no.
        using var anonymous = fixture.Client();
        var catalog = await Json(await anonymous.GetAsync($"/api/v1/catalog/activities?programId={programId}"), HttpStatusCode.OK);
        var item = Assert.Single(catalog.GetProperty("items").EnumerateArray());
        Assert.Equal(activityId, item.GetProperty("id").GetGuid());
        Assert.False(item.TryGetProperty("meetingUrl", out _));
        var free = await Json(await anonymous.GetAsync("/api/v1/catalog/activities?groupLabel=Campa%C3%B1a%20de%20Navidad"), HttpStatusCode.OK);
        Assert.Empty(free.GetProperty("items").EnumerateArray());
        Assert.Equal(HttpStatusCode.NotFound, (await (await ClientAsync(null, RoleScope.GLOBAL, null)).Http.GetAsync($"/api/v1/activities/{freeId}")).StatusCode);

        // No se desactiva un programa con actividades publicadas.
        Assert.Equal(HttpStatusCode.Conflict, (await admin.Http.PostAsync($"/api/v1/programs/{programId}/deactivate", null)).StatusCode);
    }

    [Fact]
    public async Task OperativesAreDelegatedOnlyWithinTheirLeadersProgram()
    {
        var admin = await ClientAsync(AssignedRole.ADMIN, RoleScope.GLOBAL, null);
        var programId = await CreateProgramAsync(admin, activate: true);
        var otherProgramId = await CreateProgramAsync(admin, activate: true);
        var leader = await ClientAsync(AssignedRole.PROGRAM_LEADER, RoleScope.PROGRAM, programId);
        var operative = await ClientAsync(null, RoleScope.GLOBAL, null);
        var period = await PeriodAsync();
        var activityId = await CreateActivityAsync(leader, programId, period, HttpStatusCode.Created);
        var foreignActivityId = await CreateActivityAsync(admin, otherProgramId, period, HttpStatusCode.Created);

        var delegation = new { userId = operative.UserId, role = "OPERATIONAL_LEADER", scope = "ACTIVITY", scopeId = activityId };
        Assert.True((await leader.Http.PostAsJsonAsync("/api/v1/role-assignments", delegation)).IsSuccessStatusCode);

        // El líder no delega en actividades de otro programa.
        var foreign = new { userId = operative.UserId, role = "OPERATIONAL_LEADER", scope = "ACTIVITY", scopeId = foreignActivityId };
        Assert.Equal(HttpStatusCode.Forbidden, (await leader.Http.PostAsJsonAsync("/api/v1/role-assignments", foreign)).StatusCode);

        var team = await Json(await leader.Http.GetAsync($"/api/v1/programs/{programId}/team"), HttpStatusCode.OK);
        Assert.Contains(team.EnumerateArray(), m => m.GetProperty("scope").GetString() == "ACTIVITY" && m.GetProperty("userId").GetGuid() == operative.UserId);

    }

    [Fact]
    public async Task RevokingTheProgramAssignmentRemovesManagementImmediately()
    {
        var admin = await ClientAsync(AssignedRole.ADMIN, RoleScope.GLOBAL, null);
        var programId = await CreateProgramAsync(admin, activate: true);
        var leader = await ClientAsync(AssignedRole.PROGRAM_LEADER, RoleScope.PROGRAM, programId);
        var edit = new { name = "Renombrado" };
        Assert.Equal(HttpStatusCode.OK, (await leader.Http.PatchAsJsonAsync($"/api/v1/programs/{programId}", edit)).StatusCode);

        await using (var db = fixture.Context())
        {
            var assignment = await db.RoleAssignments.SingleAsync(r => r.UserId == leader.UserId && r.ScopeId == programId);
            Assert.Equal(HttpStatusCode.OK, (await admin.Http.PostAsync($"/api/v1/role-assignments/{assignment.Id}/revoke", null)).StatusCode);
        }

        Assert.Equal(HttpStatusCode.Forbidden, (await leader.Http.PatchAsJsonAsync($"/api/v1/programs/{programId}", edit)).StatusCode);
    }

    [Fact]
    public async Task RescheduleWarnsAboutOverlapsAndPublishedActivitiesStartAutomaticallyOnce()
    {
        var admin = await ClientAsync(AssignedRole.ADMIN, RoleScope.GLOBAL, null);
        var programId = await CreateProgramAsync(admin, activate: true);
        var leader = await ClientAsync(AssignedRole.PROGRAM_LEADER, RoleScope.PROGRAM, programId);
        var period = await PeriodAsync();
        var place = $"Aula {Guid.NewGuid():N}"[..14];
        var first = await CreateActivityAsync(leader, programId, period, HttpStatusCode.Created, location: place, startsInHours: 48);
        var second = await CreateActivityAsync(leader, programId, period, HttpStatusCode.Created, location: place, startsInHours: 96);
        await leader.Http.PostAsync($"/api/v1/activities/{first}/publish", null);
        await leader.Http.PostAsync($"/api/v1/activities/{second}/publish", null);

        // Mover la segunda encima de la primera: mismo lugar y mismo líder responsable.
        var newStart = DateTime.UtcNow.AddHours(48).AddMinutes(30);
        var check = await Json(
            await leader.Http.PostAsJsonAsync("/api/v1/activities/check-conflicts", new { activityId = second, startsAt = newStart, endsAt = newStart.AddHours(1), location = place }),
            HttpStatusCode.OK);
        var types = check.GetProperty("conflicts").EnumerateArray().Select(c => c.GetProperty("type").GetString()).ToList();
        Assert.Contains("LOCATION_OVERLAP", types);
        Assert.Contains("LEADER_OVERLAP", types);

        var reschedule = await Json(
            await leader.Http.PostAsJsonAsync($"/api/v1/activities/{second}/reschedules", new { reason = "Cambio de sede", newStartsAt = newStart, newEndsAt = newStart.AddHours(1) }),
            HttpStatusCode.OK);
        Assert.NotEmpty(reschedule.GetProperty("conflicts").EnumerateArray());
        var history = await Json(await leader.Http.GetAsync($"/api/v1/activities/{second}/reschedules"), HttpStatusCode.OK);
        Assert.Single(history.EnumerateArray());

        // Una actividad que ya empezó pasa sola a IN_PROGRESS, una sola vez.
        var running = await CreateActivityAsync(leader, programId, period, HttpStatusCode.Created, startsInHours: -1);
        await leader.Http.PostAsync($"/api/v1/activities/{running}/publish", null);
        using (var scope = fixture.Services.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<ActivityStarter>().StartDueAsync(DateTime.UtcNow);
            await scope.ServiceProvider.GetRequiredService<ActivityStarter>().StartDueAsync(DateTime.UtcNow);
        }

        var started = await Json(await leader.Http.GetAsync($"/api/v1/activities/{running}"), HttpStatusCode.OK);
        Assert.Equal("IN_PROGRESS", started.GetProperty("status").GetString());
        await using var db = fixture.Context();
        Assert.Equal(1, await db.AuditEvents.CountAsync(e => e.Action == "ACTIVITY_STARTED" && e.ResourceId == running && e.ActorId == null));
        Assert.Equal(HttpStatusCode.OK, (await leader.Http.PostAsync($"/api/v1/activities/{running}/finish", null)).StatusCode);
    }

    [Fact]
    public async Task LastSeatIsAtomicAndCancellationReleasesOnlyItsOwnSeat()
    {
        var admin = await ClientAsync(AssignedRole.ADMIN, RoleScope.GLOBAL, null);
        var one = await ClientAsync(null, RoleScope.GLOBAL, null);
        var two = await ClientAsync(null, RoleScope.GLOBAL, null);
        var activity = await Json(await admin.Http.PostAsJsonAsync("/api/v1/activities", ActivityBody(null, await PeriodAsync(), capacity: 1, requiresRegistration: true)), HttpStatusCode.Created);
        var path = $"/api/v1/activities/{activity.GetProperty("id").GetGuid()}";
        (await admin.Http.PostAsync(path + "/publish", null)).EnsureSuccessStatusCode();
        var signup = path + "/enrollments";
        var results = await Task.WhenAll(one.Http.PostAsJsonAsync(signup, new { }), two.Http.PostAsJsonAsync(signup, new { }));
        var success = await Json(Assert.Single(results, r => r.IsSuccessStatusCode), HttpStatusCode.OK);
        var rejection = await Json(Assert.Single(results, r => !r.IsSuccessStatusCode), HttpStatusCode.Conflict);
        Assert.Equal("CAPACITY_FULL", rejection.GetProperty("code").GetString());
        var winner = success.GetProperty("userId").GetGuid() == one.UserId ? one : two;
        var other = winner == one ? two : one;
        var cancel = signup + "/" + success.GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.NotFound, (await other.Http.DeleteAsync(cancel)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await other.Http.GetAsync(signup)).StatusCode);
        (await winner.Http.DeleteAsync(cancel)).EnsureSuccessStatusCode();
        (await other.Http.PostAsJsonAsync(signup, new { })).EnsureSuccessStatusCode();
        (await winner.Http.DeleteAsync(cancel)).EnsureSuccessStatusCode();
        var mine = await Json(await winner.Http.GetAsync("/api/v1/me/enrollments"), HttpStatusCode.OK);
        Assert.Equal("CANCELLED", Assert.Single(mine.EnumerateArray()).GetProperty("status").GetString());
    }

    [Fact]
    public async Task GroupEnrollmentChecksEveryActiveIntervalAndAllowsAdjacentSlots()
    {
        var admin = await ClientAsync(AssignedRole.ADMIN, RoleScope.GLOBAL, null);
        var person = await ClientAsync(null, RoleScope.GLOBAL, null);
        var period = await PeriodAsync();
        var draft = await Json(await admin.Http.PostAsJsonAsync("/api/v1/activities", ActivityBody(null, period, requiresRegistration: true)), HttpStatusCode.Created);
        var start = draft.GetProperty("startsAt").GetDateTime().AddMinutes(2);
        var path = $"/api/v1/activities/{draft.GetProperty("id").GetGuid()}";
        var groups = new[] {
            new { name = "A", startsAt = start, endsAt = start.AddMinutes(30), capacity = 1 },
            new { name = "B", startsAt = start.AddMinutes(30), endsAt = start.AddHours(1), capacity = 1 },
            new { name = "C", startsAt = start.AddMinutes(15), endsAt = start.AddMinutes(45), capacity = 2 }
        };
        var setup = await Json(await admin.Http.PutAsJsonAsync(path + "/participation", new { allowMultipleGroups = true, groups }), HttpStatusCode.OK);
        var ids = setup.GetProperty("participation").GetProperty("groups").EnumerateArray().ToDictionary(g => g.GetProperty("name").GetString()!, g => g.GetProperty("id").GetGuid());
        (await admin.Http.PostAsync(path + "/publish", null)).EnsureSuccessStatusCode();
        var signup = path + "/enrollments";
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await person.Http.PostAsJsonAsync(signup, new { })).StatusCode);
        (await person.Http.PostAsJsonAsync(signup, new { groupId = ids["A"] })).EnsureSuccessStatusCode();
        var duplicate = await Json(await person.Http.PostAsJsonAsync(signup, new { groupId = ids["A"] }), HttpStatusCode.Conflict);
        Assert.Equal("ALREADY_ENROLLED", duplicate.GetProperty("code").GetString());
        var collision = await Json(await person.Http.PostAsJsonAsync(signup, new { groupId = ids["C"] }), HttpStatusCode.Conflict);
        Assert.Equal("TIME_OVERLAP", collision.GetProperty("code").GetString());
        (await person.Http.PostAsJsonAsync(signup, new { groupId = ids["B"] })).EnsureSuccessStatusCode();
        var second = await Json(await admin.Http.PostAsJsonAsync("/api/v1/activities", ActivityBody(null, period, requiresRegistration: true)), HttpStatusCode.Created);
        var otherPath = $"/api/v1/activities/{second.GetProperty("id").GetGuid()}";
        (await admin.Http.PostAsync(otherPath + "/publish", null)).EnsureSuccessStatusCode();
        var cross = await Json(await person.Http.PostAsJsonAsync(otherPath + "/enrollments", new { }), HttpStatusCode.Conflict);
        Assert.Equal("TIME_OVERLAP", cross.GetProperty("code").GetString());
    }

    [Fact]
    public async Task CoverUsesPrivateStorageAndChecksResourceAccess()
    {
        var admin = await ClientAsync(AssignedRole.ADMIN, RoleScope.GLOBAL, null);
        var person = await ClientAsync(null, RoleScope.GLOBAL, null);
        var id = await CreateProgramAsync(admin);
        var cover = $"/api/v1/programs/{id}/cover";
        using var png = new SkiaSharp.SKBitmap(20, 10);
        using var encoded = png.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
        using var upload = new MultipartFormDataContent();
        var content = new ByteArrayContent(encoded.ToArray());
        content.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        upload.Add(content, "file", "cover.png");
        Assert.Equal(HttpStatusCode.Forbidden, (await person.Http.PutAsync(cover, upload)).StatusCode);
        (await admin.Http.PutAsync(cover, upload)).EnsureSuccessStatusCode();
        (await admin.Http.PutAsync(cover, upload)).EnsureSuccessStatusCode();
        using var anonymous = fixture.Client();
        Assert.Equal(HttpStatusCode.NotFound, (await anonymous.GetAsync(cover)).StatusCode);
        (await admin.Http.PostAsync($"/api/v1/programs/{id}/activate", null)).EnsureSuccessStatusCode();
        var catalog = await Json(await anonymous.GetAsync("/api/v1/catalog/programs?pageSize=100"), HttpStatusCode.OK);
        Assert.Contains(catalog.GetProperty("items").EnumerateArray(), p => p.GetProperty("id").GetGuid() == id && p.GetProperty("imagePresent").GetBoolean());
        (await admin.Http.DeleteAsync(cover)).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.NotFound, (await anonymous.GetAsync(cover)).StatusCode);
    }

    [Fact]
    public async Task OpenDraftBlocksManualPeriodClosure()
    {
        var admin = await ClientAsync(AssignedRole.ADMIN, RoleScope.GLOBAL, null);
        var period = await PeriodAsync();
        await CreateActivityAsync(admin, null, period, HttpStatusCode.Created);
        var response = await admin.Http.PostAsJsonAsync($"/api/v1/academic-periods/{period}/close", new { successorLabel = "Siguiente" });
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        await using var db = fixture.Context();
        Assert.Equal(period, (await db.AcademicPeriods.SingleAsync(p => p.IsActive)).Id);
    }

    [Fact]
    public async Task ZeroCapacityRejectsWithoutRecordingAnEnrollment()
    {
        var admin = await ClientAsync(AssignedRole.ADMIN, RoleScope.GLOBAL, null);
        var person = await ClientAsync(null, RoleScope.GLOBAL, null);
        var created = await Json(await admin.Http.PostAsJsonAsync("/api/v1/activities", ActivityBody(null, await PeriodAsync(), capacity: 0, requiresRegistration: true)), HttpStatusCode.Created);
        var path = $"/api/v1/activities/{created.GetProperty("id").GetGuid()}";
        (await admin.Http.PostAsync(path + "/publish", null)).EnsureSuccessStatusCode();
        var error = await Json(await person.Http.PostAsJsonAsync(path + "/enrollments", new { }), HttpStatusCode.Conflict);
        Assert.Equal("CAPACITY_FULL", error.GetProperty("code").GetString());
        var mine = await Json(await person.Http.GetAsync("/api/v1/me/enrollments"), HttpStatusCode.OK);
        Assert.Empty(mine.EnumerateArray());
    }

    private static object ActivityBody(
        Guid? programId,
        Guid periodId,
        int? capacity = null,
        bool requiresRegistration = false,
        string? location = "Aula 1",
        string? groupLabel = null,
        int startsInHours = 24)
    {
        var start = DateTime.UtcNow.AddHours(startsInHours);
        return new
        {
            programId,
            periodId,
            title = "Taller " + Guid.NewGuid().ToString("N")[..6],
            modality = "IN_PERSON",
            location,
            startsAt = start,
            endsAt = start.AddHours(2),
            requiresRegistration,
            capacity,
            groupLabel,
        };
    }

    private static async Task<JsonElement> Json(HttpResponseMessage response, HttpStatusCode expected)
    {
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == expected, $"{(int)response.StatusCode}: {body}");
        return JsonDocument.Parse(body).RootElement.Clone();
    }

    private async Task<Guid> CreateActivityAsync(
        Actor actor,
        Guid? programId,
        Guid periodId,
        HttpStatusCode expected,
        string? groupLabel = null,
        string location = "Aula 1",
        int startsInHours = 24)
    {
        var response = await actor.Http.PostAsJsonAsync("/api/v1/activities", ActivityBody(programId, periodId, location: location, groupLabel: groupLabel, startsInHours: startsInHours));
        if (expected != HttpStatusCode.Created)
        {
            Assert.Equal(expected, response.StatusCode);
            return Guid.Empty;
        }

        return (await Json(response, HttpStatusCode.Created)).GetProperty("id").GetGuid();
    }

    private async Task<Guid> CreateProgramAsync(Actor admin, bool activate = false)
    {
        var program = await Json(await admin.Http.PostAsJsonAsync("/api/v1/programs", new { name = "Programa " + Guid.NewGuid().ToString("N")[..8] }), HttpStatusCode.Created);
        var id = program.GetProperty("id").GetGuid();
        if (activate)
        {
            Assert.Equal(HttpStatusCode.OK, (await admin.Http.PostAsync($"/api/v1/programs/{id}/activate", null)).StatusCode);
        }

        return id;
    }

    // Periodo propio que cubre el presente, para no depender de la fecha de ejecución.
    private async Task<Guid> PeriodAsync()
    {
        await using var db = fixture.Context();
        return (await db.AcademicPeriods.SingleAsync(p => p.IsActive)).Id;
    }

    private async Task<Actor> ClientAsync(AssignedRole? role, RoleScope scope, Guid? scopeId)
    {
        var email = $"{Guid.NewGuid():N}@ucb.edu.bo";
        var password = TestCredentials.NewValid();
        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            NormalizedUserName = email.ToUpperInvariant(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            EmailConfirmed = true,
            Status = AccountStatus.ACTIVE,
            SecurityStamp = Guid.NewGuid().ToString(),
        };
        user.PasswordHash = new PasswordHasher<AppUser>().HashPassword(user, password);
        await using (var db = fixture.Context())
        {
            db.Users.Add(user);
            db.Profiles.Add(new Profile { UserId = user.Id, FirstName = "Persona", LastName = role?.ToString() ?? "Voluntaria" });
            if (role is { } assigned)
            {
                db.RoleAssignments.Add(RoleAssignment.Assign(user.Id, assigned, scope, scopeId, user.Id));
            }

            await db.SaveChangesAsync();
        }

        var client = fixture.Client();
        var tokens = await (await client.PostAsJsonAsync("login", new { email, password })).Content.ReadFromJsonAsync<TokenResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens!.AccessToken);
        return new Actor(user.Id, client);
    }

    private sealed record Actor(Guid UserId, HttpClient Http);
}
