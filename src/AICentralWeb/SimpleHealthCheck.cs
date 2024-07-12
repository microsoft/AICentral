using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AICentralWeb;

/// <summary>
/// Could go further, but for now just response OK if we are up and running.
/// </summary>
public class SimpleHealthCheck: IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = new CancellationToken())
    {
        return Task.FromResult(HealthCheckResult.Healthy());
    }
}