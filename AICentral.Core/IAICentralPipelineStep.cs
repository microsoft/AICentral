namespace AICentral.Core;

public interface IAICentralPipelineStep
{
    Task<AICentralResponse> Handle(HttpContext context, AICallInformation aiCallInformation,
        IAICentralPipelineExecutor pipeline,
        CancellationToken cancellationToken);

    object WriteDebug();

    void ConfigureRoute(WebApplication app, IEndpointConventionBuilder route);

    static virtual string ConfigName  => throw new NotImplementedException();

}
