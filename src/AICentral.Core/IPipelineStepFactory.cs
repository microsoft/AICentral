namespace AICentral.Core;

/// <summary>
/// Used to build pipeline steps that form the basis of AI Central's Pipelines.
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IPipelineStepFactory
{
    /// <summary>
    /// The name AICentral looks for in your configuration to trigger this step.
    /// </summary>
    /// <exception cref="NotImplementedException"></exception>
    static virtual string ConfigName  => throw new NotImplementedException();

    /// <summary>
    /// Register any dependencies you need here. You may need to do this if you want to use the IServiceProvider to build your steps or their dependencies, later on. 
    /// </summary>
    /// <returns></returns>
    void RegisterServices(IServiceCollection services);

    /// <summary>
    /// When a pipeline is executed you need to provide an instance of your step. If you need to store state within an execution then
    /// you should provide a new instance of your Step every time. You may use the standard Asp.Net IServiceProvider to build your step if it helps.
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <returns></returns>
    IPipelineStep Build(IServiceProvider serviceProvider);
    
    static virtual IPipelineStepFactory BuildFromConfig(ILogger logger, TypeAndNameConfig config) => throw new NotImplementedException();

    void ConfigureRoute(WebApplication app, IEndpointConventionBuilder route);

    object WriteDebug();

}
