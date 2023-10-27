using Microsoft.Identity.Web;

namespace AICentral.Pipelines.Auth;

public class EntraAuthProviderProvider : IAICentralClientAuthProvider
{
    private readonly IConfigurationSection _configSection;
    private readonly string _id;

    public EntraAuthProviderProvider(IConfigurationSection configSection)
    {
        _configSection = configSection;
        _id = Guid.NewGuid().ToString();
    }

    /// <summary>
    /// Add an AAD provider for this particular config section.
    /// </summary>
    /// <param name="builder"></param>`
    public void RegisterServices(IServiceCollection services)
    {
        services.AddAuthentication().AddMicrosoftIdentityWebApi(_configSection, "Properties", _id);
        services.AddAuthorizationBuilder().AddPolicy(_id,
            policyBuilder => policyBuilder.RequireAuthenticatedUser().AddAuthenticationSchemes(_id));
    }

    public void ConfigureRoute(WebApplication app, IEndpointConventionBuilder route)
    {
        route.RequireAuthorization(_id);
    }

    public static string ConfigName => "Entra";

    public static IAICentralClientAuthProvider BuildFromConfig(
        IConfigurationSection configurationSection, 
        Dictionary<string, string> parameters)
    {
        return new EntraAuthProviderProvider(configurationSection);
    }

    public object WriteDebug()
    {
        return new
        {
            Type = "Entra"
        };
    }
}