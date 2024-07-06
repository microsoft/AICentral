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
        await base.ApplyAuthorisationToRequest(incomingRequest, outgoingRequest);

        //expect a claim to match the subject on
        var incomingClaim = incomingRequest.HttpContext.User.FindFirstValue(_config.IncomingClaimName)!;
        if (_config.SubjectToKeyMappings.TryGetValue(incomingClaim, out var key))
        {
            outgoingRequest.Headers.Add(_config.KeyHeaderName, key);
        }
        else
        {
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