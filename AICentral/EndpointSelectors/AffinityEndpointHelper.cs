using AICentral.Core;
using AICentral.Endpoints;
using AICentral.EndpointSelectors.Single;

namespace AICentral.EndpointSelectors;

public class AffinityEndpointHelper
{
    /// <summary>
    /// Affinity requests are denoted by an additional query-string entry that ai-central adds to an outgoing 'location' header.
    /// </summary>
    /// <param name="callInformation"></param>
    /// <param name="availableDispatchers"></param>
    /// <param name="singleEndpointSelector"></param>
    /// <returns></returns>
    public static bool IsAffinityRequest(
        AICallInformation callInformation,
        IEnumerable<IAICentralEndpointDispatcher> availableDispatchers,
        out IAICentralEndpointSelector? singleEndpointSelector)
    {
        if (callInformation.IncomingCallDetails.AICallType == AICallType.Other)
        {
            if (callInformation.QueryString.TryGetValue(AICentralHeaders.AzureOpenAIHostAffinityHeader,
                    out var affinityHeader))
            {
                if (affinityHeader.Count == 1)
                {
                    var aiCentralEndpointDispatcher =
                        availableDispatchers.SingleOrDefault(x => x.IsAffinityRequestToMe(affinityHeader[0]!));
                    if (aiCentralEndpointDispatcher != null)
                    {
                        singleEndpointSelector = new SingleEndpointSelector(aiCentralEndpointDispatcher);
                        return true;
                    }
                }
            }
        }

        singleEndpointSelector = null;
        return false;
    }

    public static IEnumerable<IAICentralEndpointDispatcher> FlattenedEndpoints(IAICentralEndpointSelector iaiCentralEndpointSelector)
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