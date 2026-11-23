namespace Uvoluntapp.Backend.Domain.Entities.Common;

public sealed class MediaAsset : Entity
{
    public Guid? UserId { get; init; }

    public Guid? ProgramId { get; init; }

    public Guid? ActivityId { get; init; }

    public required string ContentHash { get; init; }

    public string Key(bool thumbnail) => $"media/{Id:N}/{(thumbnail ? "thumb" : "image")}.webp";

    public void Retire(DateTime now) => DeletedAt = now;
}
