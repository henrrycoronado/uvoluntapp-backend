using Uvoluntapp.Backend.Domain.Entities.Common;

namespace Uvoluntapp.Backend.Domain.Entities.Accounts;

public sealed class Career : Entity
{
    public string Name { get; set; } = string.Empty;
}
