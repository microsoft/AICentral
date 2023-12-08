using AICentral.Core;

namespace AICentral;

public class AICentralPipelineExecutor : IAICentralPipelineExecutor, IDisposable
{
    private readonly IAICentralEndpointSelector _iaiCentralEndpointSelector;
    private readonly IEnumerator<IAICentralPipelineStep> _pipelineEnumerator;

    public AICentralPipelineExecutor(
        IEnumerable<IAICentralPipelineStep> steps,
        IAICentralEndpointSelector iaiCentralEndpointSelector)
    {
        _iaiCentralEndpointSelector = iaiCentralEndpointSelector;
        _pipelineEnumerator = steps.GetEnumerator();
    }

    public async Task<AICentralResponse> Next(HttpContext context, AICallInformation requestDetails,
        CancellationToken cancellationToken)
    {
        if (_pipelineEnumerator.MoveNext())
        {
            return await _pipelineEnumerator.Current.Handle(context, requestDetails, this, cancellationToken);
        }

        return await _iaiCentralEndpointSelector.Handle(context, requestDetails, true, cancellationToken);
    }

    public void Dispose()
    {
        _pipelineEnumerator.Dispose();
    }
}