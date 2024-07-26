using System.Text.Json;
using AICentral.Core;
using Microsoft.Extensions.Primitives;

namespace AICentral.ConsumerAuth.AICentralJWT;

public class AICentralJwtAuthProvider : IPipelineStep
{
    private readonly Dictionary<string, string[]> _config;

    public AICentralJwtAuthProvider(Dictionary<string, string[]> config)
    {
        _config = config;
    }

    public async Task<AICentralResponse> Handle(
        IRequestContext context,
        IncomingCallDetails aiCallInformation,
        NextPipelineStep next,
        CancellationToken cancellationToken)
    {
        var validPipelines = context.User.FindFirst("pipelines")!;
        var pipelines = JsonSerializer.Deserialize<Dictionary<string, string[]>>(validPipelines.Value);

        if (pipelines == null)
        {
            return new AICentralResponse(
                DownstreamUsageInformation.Empty(context, aiCallInformation, null, null),
                Results.Unauthorized()
            );
        }

        if (pipelines.TryGetValue(aiCallInformation.PipelineName, out var allowedDeploymentForThisToken))
        {
            if (_config.TryGetValue(aiCallInformation.PipelineName, out var configuredAllowedDeployments))
            {
                var allowed = false;
                allowed = allowed || allowedDeploymentForThisToken.Contains("*") &&
                    configuredAllowedDeployments.Contains("*");
                allowed = allowed || (
                    aiCallInformation.IncomingModelName != null &&
                    allowedDeploymentForThisToken.Contains(aiCallInformation.IncomingModelName,
                        StringComparer.InvariantCultureIgnoreCase) &&
                    configuredAllowedDeployments.Contains(aiCallInformation.IncomingModelName,
                        StringComparer.InvariantCultureIgnoreCase));

                if (allowed)
                {
                    return await next(context, aiCallInformation, cancellationToken);
                }

                context.RequestServices.GetRequiredService<ILogger<AICentralJwtAuthProvider>>()
                    .LogWarning("Unauthorized request for pipeline {Pipeline} and model {Model} from User {User}",
                        aiCallInformation.PipelineName, aiCallInformation.IncomingModelName,
                        context.UserName);
            }
        }

        return new AICentralResponse(
            DownstreamUsageInformation.Empty(context, aiCallInformation, null, null),
            Results.Unauthorized()
        );
    }

    public Task BuildResponseHeaders(IRequestContext context, HttpResponseMessage rawResponse,
        Dictionary<string, StringValues> rawHeaders)
    {
        return Task.CompletedTask;
    }
}