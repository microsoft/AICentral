using System.Security.Claims;
using AICentral.Core;

namespace AICentral.Endpoints.AzureOpenAI.Authorisers.BearerPassThroughWithAdditionalKey;

public class BearerPassThroughWithAdditionalKeyAuth: BearerTokenPassThroughAuth
{
    private readonly BearerPassThroughWithAdditionalKeyAuthFactoryConfig _config;
    private readonly Dictionary<string,string> _mappings;

    public BearerPassThroughWithAdditionalKeyAuth(BearerPassThroughWithAdditionalKeyAuthFactoryConfig config)
    {
        _config = config;
        _mappings = _config.ClaimsToKeys!
            .SelectMany(x => x.ClaimValues.Select(cv => new {ClaimValue = cv, SubscriptionKey = x.SubscriptionKey}))
            .ToDictionary(x => x.ClaimValue!, x => x.SubscriptionKey!);
    }

    public override async Task ApplyAuthorisationToRequest(IRequestContext incomingRequest, HttpRequestMessage outgoingRequest)
    {
        var logger = incomingRequest.GetLogger<BearerPassThroughWithAdditionalKeyAuth>();

        await base.ApplyAuthorisationToRequest(incomingRequest, outgoingRequest);

        //expect a claim to match the subject on
        var incomingClaim = incomingRequest.User.FindFirst(_config.IncomingClaimName)?.Value;
        if (incomingClaim != null)
        {
            if (_mappings.TryGetValue(incomingClaim, out var key))
            {
                logger.LogDebug("Matched Claim. Claim Value: {ClaimValue}...",
                    incomingClaim.Substring(0, Math.Max(incomingClaim.Length, 4)));
                outgoingRequest.Headers.Add(_config.KeyHeaderName, key);
            }
            else
            {
                logger.LogWarning("Failed to match Claim. Claim Value: {ClaimValue}...",
                    incomingClaim.Substring(0, Math.Max(incomingClaim.Length, 4)));
                throw new HttpRequestException(HttpRequestError.UserAuthenticationError, "Missing mapping for subject");
            }
        }
        else
        {
            logger.LogWarning("Failed to find Claim on incoming request: {ClaimName}", _config.IncomingClaimName);
            throw new HttpRequestException(HttpRequestError.UserAuthenticationError, "Missing mapping for subject");
        }
    }

    public new object WriteDebug()
    {
        return new
        {
            Type = "Bearer Pass Through With Key"
        };
    }
}