using AICentral.Core;
using AICentral.Endpoints;
using AICentral.EndpointSelectors.Single;

namespace AICentral.EndpointSelectors;

internal class AffinityEndpointHelper
{
    /// <summary>
    /// Affinity requests are denoted by an additional query-string entry that ai-central adds to an outgoing 'location' header.
    /// </summary>
    /// <param name="callInformation"></param>
    /// <param name="availableDispatchers"></param>
    /// <returns></returns>
    public static IEndpointSelector? FindAffinityRequestEndpoint(
        IncomingCallDetails callInformation,
        IEnumerable<IEndpointDispatcher> availableDispatchers)
    {
        if (callInformation.PreferredEndpoint != null)
        {
            var aiCentralEndpointDispatcher =
                availableDispatchers.SingleOrDefault(
                    x => x.IsAffinityRequestToMe(callInformation.PreferredEndpoint));
            if (aiCentralEndpointDispatcher != null)
            {
                return new SingleEndpointSelector(aiCentralEndpointDispatcher);
            }
        }

        return null;
    }

    public static IEnumerable<IEndpointDispatcher> FlattenedEndpoints(
        IEndpointSelector iaiCentralEndpointSelector)
    {
        foreach (var endpoint in iaiCentralEndpointSelector.ContainedEndpoints())
        {
            if (endpoint is EndpointSelectorAdapterDispatcher endpointSelectorAdapter)
            {
                foreach (var wrappedEndpoint in endpointSelectorAdapter.ContainedEndpoints())
                {
                    yield return wrappedEndpoint;
                }
            }
            else
            {
                yield return endpoint;
            }
        }
    }
}