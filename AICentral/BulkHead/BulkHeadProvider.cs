using AICentral.Core;
using Microsoft.Extensions.Primitives;

namespace AICentral.BulkHead;

public class BulkHeadProvider : IAICentralPipelineStep
{
    private readonly SemaphoreSlim _semaphore;

    public BulkHeadProvider(BulkHeadConfiguration properties)
    {
        _semaphore = new SemaphoreSlim(properties.MaxConcurrency!.Value);
    }

    public async Task<AICentralResponse> Handle(HttpContext context, AICallInformation aiCallInformation,
        IAICentralPipelineExecutor pipeline,
        CancellationToken cancellationToken)
    {
        try
        {
            await _semaphore.WaitAsync(cancellationToken);
            return await pipeline.Next(context, aiCallInformation, cancellationToken);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public Task BuildResponseHeaders(HttpContext context, HttpResponseMessage rawResponse,
        Dictionary<string, StringValues> rawHeaders)
    {
        return Task.CompletedTask;
    }
}