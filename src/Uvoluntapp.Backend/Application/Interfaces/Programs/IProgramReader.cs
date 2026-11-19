using Uvoluntapp.Backend.Application.DTOs.Common;
using Uvoluntapp.Backend.Domain.Entities.Programs;
using Uvoluntapp.Backend.Domain.Utils.Enums;

namespace Uvoluntapp.Backend.Application.Interfaces.Programs;

public interface IProgramReader
{
    Task<PagedResult<VolunteerProgram>> SearchAsync(
        ProgramStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
