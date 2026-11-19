using Uvoluntapp.Backend.Application.Common;
using Uvoluntapp.Backend.Domain.Programs;

namespace Uvoluntapp.Backend.Application.Programs.Ports;

public interface IProgramReader
{
    Task<PagedResult<VolunteerProgram>> SearchAsync(
        ProgramStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
