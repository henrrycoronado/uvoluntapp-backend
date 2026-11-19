using Xunit;

namespace Uvoluntapp.Backend.Tests.Integration;

// Las variables de entorno se comparten con las demás pruebas del proceso.
[CollectionDefinition(nameof(HostConnectionTests), DisableParallelization = true)]
public sealed class HostConnectionCollection { }
