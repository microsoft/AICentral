namespace AICentral.Core;

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
    public AICentralPipelineConfig[]? Pipelines { get; init; }
    
    /// <summary>
    /// A set of Endpoints representing the downstream services we can call.
    /// </summary>
    public AICentralTypeAndNameConfig[]? Endpoints { get; set; }
    
    /// <summary>
    /// A set of Endpoint Selectors that can be used to select an endpoint based on the request.
    /// </summary>
    public AICentralTypeAndNameConfig[]? EndpointSelectors { get; set; }
    
    /// <summary>
    /// A set of Auth Providers that can be used to authenticate the request.
    /// </summary>
    public AICentralTypeAndNameConfig[]? AuthProviders { get; set; }
    
    /// <summary>
    /// A set of generic steps that can be used in pipelines
    /// </summary>
    public AICentralTypeAndNameConfig[]? GenericSteps { get; set; }
    
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
    }

    private List<AICentralTypeAndNameConfig> FillCollection(
        string property,
        IConfigurationSection configurationSection)
    {
        var newList = new List<AICentralTypeAndNameConfig>();
        foreach (var item in configurationSection.GetSection(property).GetChildren())
        {
            newList.Add(new AICentralTypeAndNameConfig()
            {
                Name = Guard.NotNull(item.GetValue<string>("Name"), item, "Name"),
                Type = Guard.NotNull(item.GetValue<string>("Type"), item, "Type"),
                ConfigurationSection = item,
            });
        }

        return newList;
    }
}