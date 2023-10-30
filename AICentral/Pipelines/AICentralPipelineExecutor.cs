using AICentral.Pipelines.EndpointSelectors;

namespace AICentral.Pipelines;

public class AICentralPipelineExecutor : IDisposable
{
    private readonly IAICentralEndpointSelectorRuntime _endpointSelector;
    private readonly IEnumerator<IAICentralPipelineStepRuntime> _pipelineEnumerator;

    public AICentralPipelineExecutor(
        IList<IAICentralPipelineStepRuntime> steps,
        IAICentralEndpointSelectorRuntime endpointSelector)
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