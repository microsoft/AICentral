using AICentral.Core;

namespace AICentral;

/// <summary>
/// Main configuration class for AI Central. This is used to configure the pipelines, endpoints, and other settings.
/// </summary>
public class AICentralConfig
{
    /// <summary>
    /// Set to true to enable the diagnostics headers. This adds headers indicating which downstream server handled the response, and which failed
    /// </summary>
    public bool EnableDiagnosticsHeaders { get; set; } = false;
    
    /// <summary>
    /// The pipelines to use. These are the main entry points for the AI Central service.
    /// </summary>
    public PipelineConfig[]? Pipelines { get; init; }
    
    /// <summary>
    /// A set of Endpoints representing the downstream services we can call.
    /// </summary>
    public TypeAndNameConfig[]? Endpoints { get; set; }
    
    /// <summary>
    /// A set of Endpoint Selectors that can be used to select an endpoint based on the request.
    /// </summary>
    public TypeAndNameConfig[]? EndpointSelectors { get; set; }
    
    /// <summary>
    /// A set of Auth Providers that can be used to authenticate the request.
    /// </summary>
    public TypeAndNameConfig[]? AuthProviders { get; set; }
    
    /// <summary>
    /// A set of generic steps that can be used in pipelines
    /// </summary>
    public TypeAndNameConfig[]? GenericSteps { get; set; }
    
    
    /// <summary>
    /// A set of additional routes that can map incoming requests to downstream Open AI calls
    /// </summary>
    public TypeAndNameConfig[]? RouteProxies { get; set; }
    
    /// <summary>
    /// A set of backend authorisers to provide custom auth for backend services
    /// </summary>
    public TypeAndNameConfig[]? BackendAuths { get; set; }

    /// <summary>
    /// Optional Message Handler to use when making downstream requests. This can be used to add custom headers, or to add a proxy, etc.
    /// </summary>
    public HttpMessageHandler? HttpMessageHandler { get; set; }

    /// <summary>
    /// Helper method for binding the configuration so we can access strongly typed configuration.
    /// </summary>
    /// <param name="configurationSection"></param>
    public void FillInPropertiesFromConfiguration(IConfigurationSection configurationSection)
    {
        Endpoints = FillCollection(nameof(Endpoints), configurationSection).ToArray();
        EndpointSelectors = FillCollection(nameof(EndpointSelectors), configurationSection).ToArray();
        AuthProviders = FillCollection(nameof(AuthProviders), configurationSection).ToArray();
        GenericSteps = FillCollection(nameof(GenericSteps), configurationSection).ToArray();
        BackendAuths = FillCollection(nameof(BackendAuths), configurationSection).ToArray();
        RouteProxies = FillCollection(nameof(RouteProxies), configurationSection).ToArray();
    }

    private List<TypeAndNameConfig> FillCollection(
        string property,
        IConfigurationSection configurationSection)
    {
        var newList = new List<TypeAndNameConfig>();
        foreach (var item in configurationSection.GetSection(property).GetChildren())
        {
            newList.Add(new TypeAndNameConfig()
            {
                Name = Guard.NotNull(item.GetValue<string>("Name"), item, "Name"),
                Type = Guard.NotNull(item.GetValue<string>("Type"), item, "Type"),
                ConfigurationSection = item,
            });
        }

        return newList;
    }
}