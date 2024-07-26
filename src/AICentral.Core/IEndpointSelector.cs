using Microsoft.Extensions.Primitives;

namespace AICentral.Core;

public interface IEndpointSelector
{
    /// <summary>
    /// Handles AI calls and decides which endpoint to dispatch them by. If the downstream fails this method should throw if it has exhausted its available endpoints, and isLastChance is false (which means there is another option to handle the endpoint). 
    /// </summary>
    /// <param name="context"></param>
    /// <param name="aiCallInformation"></param>
    /// <param name="isLastChance"></param>
    /// <param name="responseGenerator">Method to call if you  have a successful response. This will handle sending the response to the client, counting tokens, etc.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>AICentralResponse containing metadata about the call that steps can use.</returns>
    Task<AICentralResponse> Handle(
        IRequestContext context,
        IncomingCallDetails aiCallInformation,
        bool isLastChance,
        IResponseGenerator responseGenerator,
        CancellationToken cancellationToken);

    IEnumerable<IEndpointDispatcher> ContainedEndpoints();

    Task BuildResponseHeaders(IRequestContext context, HttpResponseMessage rawResponse, Dictionary<string, StringValues> rawHeaders);
}