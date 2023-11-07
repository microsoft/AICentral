using AICentral.Steps;
using AICentral.Steps.EndpointSelectors;

namespace AICentral;

public class AICentralPipelineExecutor : IDisposable
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
            var response = await _pipelineEnumerator.Current.Handle(context, requestDetails, this, cancellationToken);
            return response;
        }

        return await _endpointSelector.Handle(context, requestDetails, this, cancellationToken);
    }

    public void Dispose()
    {
        _pipelineEnumerator.Dispose();
    }
}