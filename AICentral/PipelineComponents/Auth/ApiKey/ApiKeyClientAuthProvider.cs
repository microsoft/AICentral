using AICentral.Configuration.JSON;

namespace AICentral.PipelineComponents.Auth.ApiKey;

public class ApiKeyClientAuthProvider : IAICentralClientAuthStep
{
    private readonly string _apiKeyPolicyId;
    private readonly ConfigurationTypes.ApiKeyClientAuthConfig _config;

    public ApiKeyClientAuthProvider(string apiKeyPolicyId, ConfigurationTypes.ApiKeyClientAuthConfig config)
    {
        _apiKeyPolicyId = apiKeyPolicyId;
        _config = config;
    }

    public Task<AICentralResponse> Handle(HttpContext context, AICentralPipelineExecutor pipeline, CancellationToken cancellationToken)
    {
        return pipeline.Next(context, cancellationToken);
    }

    public object WriteDebug()
    {
        return new
        {
            Type = "Client Api Key",
            HeaderName = _config.HeaderName,
            Clients = _config.Clients!.Select(x => new
            {
                ClientName = x.ClientName,
                Key1 = x.Key1!.Substring(0,1) + "****" + x.Key1[^1],
                Key2 = x.Key2!.Substring(0,1) + "****" + x.Key1[^1],
            })
        };
    }

    public void ConfigureRoute(WebApplication app, IEndpointConventionBuilder route)
    {
        route.RequireAuthorization(_apiKeyPolicyId);
    }
}