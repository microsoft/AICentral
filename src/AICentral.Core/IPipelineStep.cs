using Microsoft.Extensions.Primitives;

namespace AICentral.Core;

/// <summary>
/// Implement this interface to create steps that can execute in a pipeline.
/// </summary>
public interface IPipelineStep
{
    /// <summary>
    /// Core method for executing custom step logic.
    /// You can execute logic pre and post calling the AI service. Use the pipeline.Next(...) method to call the next step in the pipeline.
    /// When the pipeline has executed you can run more logic if you wish. You must return the AICentralResponse object back up the chain.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="aiCallInformation"></param>
    /// <param name="pipeline"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<AICentralResponse> Handle(HttpContext context, IncomingCallDetails aiCallInformation,
        IPipelineExecutor pipeline,
        CancellationToken cancellationToken);

    /// <summary>
    /// Opportunity for a Pipeline step to add and remove custom headers to the response sent back to the client.
    /// Add (or remove) headers to the rawHeaders dictionary. These will be sent before any response content is returned 
    /// </summary>
    /// <remarks>
    /// For streaming responses we do not have token counts when this method executes as we haven't sent the response to the client yet.
    /// </remarks>
    /// <returns></returns>
    Task BuildResponseHeaders(HttpContext context, HttpResponseMessage rawResponse, Dictionary<string,StringValues> rawHeaders);
}
