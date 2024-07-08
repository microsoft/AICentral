using System.Security.Claims;

namespace AICentral.Endpoints.AzureOpenAI.Authorisers.BearerPassThroughWithAdditionalKey;

public class BearerPassThroughWithAdditionalKeyAuth: BearerTokenPassThroughAuth
{
    private readonly BearerPassThroughWithAdditionalKeyAuthFactoryConfig _config;

    public BearerPassThroughWithAdditionalKeyAuth(BearerPassThroughWithAdditionalKeyAuthFactoryConfig config)
    {
        _config = config;
    }

    public override async Task ApplyAuthorisationToRequest(HttpRequest incomingRequest, HttpRequestMessage outgoingRequest)
    {
        var logger = incomingRequest.HttpContext.RequestServices
            .GetRequiredService<ILogger<BearerPassThroughWithAdditionalKeyAuth>>();

        await base.ApplyAuthorisationToRequest(incomingRequest, outgoingRequest);

        //expect a claim to match the subject on
        var incomingClaim = incomingRequest.HttpContext.User.FindFirstValue(_config.IncomingClaimName);
        if (incomingClaim != null)
        {
            if (_config.SubjectToKeyMappings.TryGetValue(incomingClaim, out var key))
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