using System.Security.Cryptography;
using Uvoluntapp.Backend.Application.Exceptions.Common;
using Uvoluntapp.Backend.Application.Interfaces.Activities;
using Uvoluntapp.Backend.Application.Interfaces.Common;
using Uvoluntapp.Backend.Application.Interfaces.Programs;
using Uvoluntapp.Backend.Application.Services.Audit;
using Uvoluntapp.Backend.Application.Services.Programs;
using Uvoluntapp.Backend.Domain.Entities.Common;
using Uvoluntapp.Backend.Domain.Utils.Enums;
using Uvoluntapp.Backend.Domain.Utils.Exceptions;

namespace Uvoluntapp.Backend.Application.Services.Common;

public sealed class ResourceImageService(PlanningAccess access, IMediaStore store, IProgramStore programs, IActivityStore activities, IImageProcessor processor, IFileStorage storage, AuditRecorder audit, ILogger<ResourceImageService> logger)
{
    public async Task<string> ReadAsync(Guid? viewer, ImageOwner owner, Guid id, bool thumbnail, CancellationToken ct)
    {
        await AuthorizeAsync(viewer, owner, id, false, ct);
        var asset = await store.FindAsync(owner, id, ct) ?? throw new NotFoundException("Sin imagen.");
        return await storage.ReadUrlAsync(asset.Key(thumbnail));
    }

    public async Task ReplaceAsync(Guid actor, ImageOwner owner, Guid id, Stream? source, string? mime, CancellationToken ct)
    {
        MediaAsset? previous = null;
        MediaAsset? uploaded = null;
        await store.WithOwnerLockAsync(
            owner,
            id,
            async () =>
        {
            await AuthorizeAsync(actor, owner, id, true, ct);
            previous = await store.FindAsync(owner, id, ct);
            if (source != null)
            {
                var image = processor.Decode(source, mime ?? string.Empty);
                var hash = Convert.ToHexString(SHA256.HashData(image.Full));
                if (previous?.ContentHash == hash)
                {
                    previous = null;
                    return;
                }

                uploaded = new MediaAsset { ContentHash = hash, UserId = owner == ImageOwner.Profile ? id : null, ProgramId = owner == ImageOwner.Program ? id : null, ActivityId = owner == ImageOwner.Activity ? id : null };
                try
                {
                    await storage.PutAsync(uploaded.Key(false), image.Full, ct);
                    await storage.PutAsync(uploaded.Key(true), image.Thumbnail, ct);
                }
                catch
                {
                    await CleanupAsync(uploaded);
                    throw;
                }
            }
            else if (previous == null)
            {
                return;
            }

            try
            {
                previous?.Retire(DateTime.UtcNow);
                await store.SaveAsync(null, ct);
                await audit.RecordAsync(actor, source == null ? "IMAGE_REMOVED" : "IMAGE_UPDATED", owner.ToString().ToUpperInvariant(), id, cancellationToken: ct);
                await store.SaveAsync(uploaded, ct);
            }
            catch
            {
                if (uploaded != null)
                {
                    await CleanupAsync(uploaded);
                }

                throw;
            }
        },
            ct);
        if (previous != null)
        {
            await CleanupAsync(previous);
        }
    }

    private async Task CleanupAsync(MediaAsset asset)
    {
        try
        {
            await storage.DeleteAsync(asset.Key(false), CancellationToken.None);
            await storage.DeleteAsync(asset.Key(true), CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Limpieza pendiente de la imagen privada {AssetId}", asset.Id);
        }
    }

    private async Task AuthorizeAsync(Guid? viewer, ImageOwner owner, Guid id, bool write, CancellationToken ct)
    {
        var actor = viewer.HasValue ? await access.ResolveAsync(viewer.Value, ct) : null;
        if (owner == ImageOwner.Profile)
        {
            if (actor?.ActorId != id)
            {
                throw new ForbiddenException("Se requiere el titular del perfil.");
            }

            return;
        }

        if (owner == ImageOwner.Program)
        {
            var program = await programs.FindAsync(id, ct) ?? throw new NotFoundException("El programa no existe.");
            if (!write && !program.IsArchived && program.Status == ProgramStatus.ACTIVE)
            {
                return;
            }

            if (actor == null || program.IsArchived)
            {
                throw new NotFoundException("El programa no está disponible.");
            }

            access.RequireProgramManager(actor, id);
            return;
        }

        var activity = await activities.FindAsync(id, ct) ?? throw new NotFoundException("La actividad no existe.");
        if (activity.DeletedAt != null)
        {
            throw new NotFoundException("La actividad no existe.");
        }

        if (!write && (activity.IsPublic || (actor != null && access.CanViewActivityInternals(actor, activity))))
        {
            return;
        }

        if (actor == null)
        {
            throw new NotFoundException("La actividad no está disponible.");
        }

        access.RequireActivityManager(actor, activity);
        if (write && activity.Status is ActivityStatus.CANCELLED or ActivityStatus.FINISHED)
        {
            throw new DomainRuleViolationException("Esta actividad ya no admite ediciones.");
        }
    }
}
