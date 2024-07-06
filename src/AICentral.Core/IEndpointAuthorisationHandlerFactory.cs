namespace AICentral.Core;

/// <summary>
/// Used to build classes that add authorisation to outgoing requests to an AI Service
/// </summary>
public interface IEndpointAuthorisationHandlerFactory
{
    /// <summary>
    /// The name AICentral looks for in your configuration to use this backend authoriser.
    /// </summary>
    /// <exception cref="NotImplementedException"></exception>
    static virtual string ConfigName  => throw new NotImplementedException();

    /// <summary>
    /// Register any dependencies you need here. You may need to do this if you want to use the IServiceProvider to build your steps or their dependencies, later on. 
    /// </summary>
    /// <returns></returns>
    void RegisterServices(IServiceCollection services);

    /// <summary>
    /// When a pipeline is executed you need to provide an instance of your authoriser. If you need to store state within an execution then
    /// you should provide a new instance of your authoriser every time. You may use the standard Asp.Net IServiceProvider to build your step if it helps.
    /// </summary>
    /// <returns></returns>
    IEndpointAuthorisationHandler Build();
    
    static virtual IEndpointAuthorisationHandlerFactory BuildFromConfig(ILogger logger, TypeAndNameConfig config) => throw new NotImplementedException();

    object WriteDebug();}