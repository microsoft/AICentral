using AICentral.Pipelines.EndpointSelectors;

namespace AICentral.Pipelines;

public class AICentralPipelineExecutor : IDisposable
{
    private readonly IAICentralEndpointSelector _endpointSelector;
    private readonly IEnumerator<IAICentralPipelineStep> _pipelineEnumerator;

    public AICentralPipelineExecutor(
        IList<IAICentralPipelineStep> steps,
        IAICentralEndpointSelector endpointSelector)
    {
        _endpointSelector = endpointSelector;
        _pipelineEnumerator = steps.GetEnumerator();
    }

    public async Task<AICentralResponse> Next(HttpContext context, CancellationToken cancellationToken)
    {
        if (_pipelineEnumerator.MoveNext())
        {
            var response = await _pipelineEnumerator.Current.Handle(context, this, cancellationToken);
            return response;
        }

        return await _endpointSelector.Handle(context, this, cancellationToken);
    }

    public void Dispose()
    {
        _pipelineEnumerator.Dispose();
    }
}