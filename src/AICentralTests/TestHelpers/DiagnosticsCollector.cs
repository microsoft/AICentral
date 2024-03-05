using System.Collections.Concurrent;
using AICentral.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace AICentralTests.TestHelpers;

public class DiagnosticsCollector:  IPipelineStep
{
    public readonly ConcurrentDictionary<string, DownstreamUsageInformation> DownstreamUsageInformation = new();

    public IPipelineStep Build(IServiceProvider serviceProvider)
    {
        return this;
    }

    public async Task<AICentralResponse> Handle(HttpContext context, IncomingCallDetails aiCallInformation, NextPipelineStep next,
        CancellationToken cancellationToken)
    {
        var id = Guid.NewGuid().ToString();
        context.Response.Headers.Append("x-aicentral-test-diagnostics", id);
        var response = await next(context, aiCallInformation, cancellationToken);
        DownstreamUsageInformation.TryAdd(id, response.DownstreamUsageInformation);
        return response;
    }

    public Task BuildResponseHeaders(HttpContext context, HttpResponseMessage rawResponse, Dictionary<string, StringValues> rawHeaders)
    {
        return Task.CompletedTask;
    }
}