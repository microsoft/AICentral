namespace AICentral.Steps.Auth.Entra;

public class EntraClientAuthProvider : IAICentralClientAuthStep
{
    private readonly string _entraAuthPolicyId;

    public EntraClientAuthProvider(string entraAuthPolicyId)
    {
        _entraAuthPolicyId = entraAuthPolicyId;
    }
    
    public Task<AICentralResponse> Handle(HttpContext context, AICallInformation aiCallInformation,
        AICentralPipelineExecutor pipeline,
        CancellationToken cancellationToken)
    {
        return pipeline.Next(context, aiCallInformation, cancellationToken);
    }
    
    
    /// <summary>
    /// Entra Auth is provided at the route scope. The runtime step is a no-op.
    /// </summary>
    /// <param name="app"></param>
    /// <param name="route"></param>
    public void ConfigureRoute(WebApplication app, IEndpointConventionBuilder route)
    {
        route.RequireAuthorization(_entraAuthPolicyId);
    }


    public object WriteDebug()
    {
        return new
        {
            Type = "Entra"
        };
    }
}