namespace AICentral.PipelineComponents.Auth.ApiKey;

public class ApiKeyClientAuthProvider : IAICentralClientAuthStep
{
    private readonly string _apiKeyPolicyId;

    public ApiKeyClientAuthProvider(string apiKeyPolicyId)
    {
        _apiKeyPolicyId = apiKeyPolicyId;
    }

    public Task<AICentralResponse> Handle(HttpContext context, AICentralPipelineExecutor pipeline, CancellationToken cancellationToken)
    {
        return pipeline.Next(context, cancellationToken);
    }

    public object WriteDebug()
    {
        return new { auth = "Client Api Key" };
    }

    public void ConfigureRoute(WebApplication app, IEndpointConventionBuilder route)
    {
        route.RequireAuthorization(_apiKeyPolicyId);
    }
}