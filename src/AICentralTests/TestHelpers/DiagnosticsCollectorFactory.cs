using AICentral.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AICentralTests.TestHelpers;

public class DiagnosticsCollectorFactory: IPipelineStepFactory
{
    public void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton(new DiagnosticsCollector());
    }

    public IPipelineStep Build(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetRequiredService<DiagnosticsCollector>();
    }

    public void ConfigureRoute(WebApplication app, IEndpointConventionBuilder route)
    {
    }

    public object WriteDebug()
    {
        return new { Type = "TestHelperDiagnosticsCollector" };
    }

    public static string ConfigName => "DiagnosticsCollector";
    
    public static IPipelineStepFactory BuildFromConfig(ILogger logger, TypeAndNameConfig config)
    {
        throw new NotSupportedException();
    }
}