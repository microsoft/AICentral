using AICentral.Core;
using AICentral.Steps;
using AICentral.Steps.EndpointSelectors;

namespace AICentral;

public class AICentralPipelineExecutor : IAICentralPipelineExecutor, IDisposable
{
    private readonly IEndpointSelector _endpointSelector;
    private readonly IEnumerator<IAICentralPipelineStep> _pipelineEnumerator;

    public AICentralPipelineExecutor(
        IList<IAICentralPipelineStep> steps,
        IEndpointSelector endpointSelector)
    {
        _endpointSelector = endpointSelector;
        _pipelineEnumerator = steps.GetEnumerator();
    }

    public async Task<AICentralResponse> Next(HttpContext context, AICallInformation requestDetails, CancellationToken cancellationToken)
    {
        if (_pipelineEnumerator.MoveNext())
        {
            return await _pipelineEnumerator.Current.Handle(context, requestDetails, this, cancellationToken);
        }

        return await _endpointSelector.Handle(context, requestDetails, cancellationToken);
    }

    public void Dispose()
    {
        _pipelineEnumerator.Dispose();
    }
}

