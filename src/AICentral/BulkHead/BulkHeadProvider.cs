using AICentral.Core;
using Microsoft.Extensions.Primitives;

namespace AICentral.BulkHead;

public class BulkHeadProvider : IPipelineStep
{
    private readonly SemaphoreSlim _semaphore;

    public BulkHeadProvider(BulkHeadConfiguration properties)
    {
        _semaphore = new SemaphoreSlim(properties.MaxConcurrency!.Value);
    }

    public async Task<AICentralResponse> Handle(HttpContext context, IncomingCallDetails aiCallInformation,
        NextPipelineStep next,
        CancellationToken cancellationToken)
    {
        try
        {
            await _semaphore.WaitAsync(cancellationToken);
            return await next(context, aiCallInformation, cancellationToken);
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