namespace AICentral.Pipelines.Auth;

public class NoClientAuthAuthProvider: IAICentralClientAuthProvider
{
    private readonly NoClientAuth _builtProvider = new();

    public void RegisterServices(IServiceCollection services)
    {
    }

    public void ConfigureRoute(WebApplication app, IEndpointConventionBuilder route)
    {
        //no-op
    }

    public IAICentralClientAuth Build()
    {
        return _builtProvider;
    }

    public static IAICentralClientAuthProvider BuildFromConfig(IConfigurationSection configurationSection, Dictionary<string, string> parameters)
    {
        return new NoClientAuthAuthProvider();
    }

    public static string ConfigName => "NoOp";
}

public class NoClientAuth : IAICentralClientAuth
{
    public Task<AICentralResponse> Handle(HttpContext context, AICentralPipelineExecutor pipeline, CancellationToken cancellationToken)
    {
        return pipeline.Next(context, cancellationToken);
    }


    public object WriteDebug()
    {
        return new { auth = "No Consumer Auth" };
    }

}