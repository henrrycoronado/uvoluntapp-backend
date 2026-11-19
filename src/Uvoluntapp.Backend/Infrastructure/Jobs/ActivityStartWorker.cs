using Microsoft.EntityFrameworkCore;
using Uvoluntapp.Backend.Application.Services.Activities;

namespace Uvoluntapp.Backend.Infrastructure.Jobs;

/// <summary>
/// Revisa cada ACTIVITY_START_INTERVAL_SECONDS (60 por defecto) las actividades publicadas cuyo horario
/// llegó y las pasa a en ejecución (SCRUM-50). Si otro nodo ganó la carrera, xmin lo detecta y se ignora.
/// </summary>
public sealed class ActivityStartWorker(
    IServiceScopeFactory scopes,
    IConfiguration configuration,
    TimeProvider clock,
    ILogger<ActivityStartWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(Math.Max(5, configuration.GetValue("ACTIVITY_START_INTERVAL_SECONDS", 60)));
        using var timer = new PeriodicTimer(interval, clock);
        do
        {
            try
            {
                using var scope = scopes.CreateScope();
                var started = await scope.ServiceProvider.GetRequiredService<ActivityStarter>()
                    .StartDueAsync(clock.GetUtcNow().UtcDateTime, stoppingToken);
                if (started > 0)
                {
                    logger.LogInformation("Actividades iniciadas automáticamente: {Count}.", started);
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                // Otro nodo las inició primero: la siguiente vuelta ya no las encuentra.
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Falló el inicio automático de actividades.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
