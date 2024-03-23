using AICentral.Core;
using Microsoft.Extensions.Primitives;

namespace AICentral.ConsumerAuth.AICentralJWT;

public class AICentralJwtAuthProvider : IPipelineStep
{
    public async Task<AICentralResponse> Handle(HttpContext context, IncomingCallDetails aiCallInformation,
        NextPipelineStep next, CancellationToken cancellationToken)
    {
        var validPipelines = context.User.FindFirst("pipelines")!;
        var pipelines = validPipelines.Value.Split(' ');
        
        if (pipelines.Contains("*") ||
            pipelines.Contains(aiCallInformation.PipelineName, StringComparer.InvariantCultureIgnoreCase))
        {
            return await next(context, aiCallInformation, cancellationToken);
        }

        return new AICentralResponse(
            DownstreamUsageInformation.Empty(context, aiCallInformation, null, null, null),
            Results.Unauthorized()
        );
    }

    public Task BuildResponseHeaders(HttpContext context, HttpResponseMessage rawResponse,
        Dictionary<string, StringValues> rawHeaders)
    {
        return Task.CompletedTask;
    }
}