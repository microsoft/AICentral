using AICentral.Core;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Tokens;

namespace AICentral.ConsumerAuth.Entra;

public class EntraClientAuthFactory : IPipelineStepFactory
{
    private readonly TypeAndNameConfig _configSection;
    private readonly string _id;
    private readonly Lazy<EntraClientAuthProvider> _provider;

    public EntraClientAuthFactory(TypeAndNameConfig configSection)
    {
        _configSection = configSection;
        _id = Guid.NewGuid().ToString();
        _provider = new Lazy<EntraClientAuthProvider>(() => new EntraClientAuthProvider());
    }

    /// <summary>
    /// Add an AAD provider for this particular config section.
    /// </summary>
    public void RegisterServices(IServiceCollection services)
    {
        var section = _configSection.ConfigurationSection!.GetSection("Properties");
        var customSection = _configSection.TypedProperties<EntraClientAuthConfig>();

        services.AddAuthentication().AddMicrosoftIdentityWebApi(section, "Entra", _id);
        services.AddAuthorizationBuilder().AddPolicy(_id, policyBuilder =>
        {
            var builder=  policyBuilder.RequireAuthenticatedUser();
            if (customSection.Requirements?.Roles != null)
            {
                builder.RequireRole(customSection.Requirements.Roles);
            }

            builder.AddAuthenticationSchemes(_id);
        });
    }

    public static string ConfigName => "Entra";

    public IPipelineStep Build(IServiceProvider serviceProvider)
    {
        return _provider.Value;
    }

    public static IPipelineStepFactory BuildFromConfig(
        ILogger logger, 
        TypeAndNameConfig config)
    {

        var customSection = config.TypedProperties<EntraClientAuthConfig>();
        if (customSection.Requirements == null || customSection.Requirements.Roles.IsNullOrEmpty())
        {
            logger.LogWarning("Entra auth is configured but no roles are specified. Unless the Application is configured for specific user-assignment, this will allow all users and applications to access the endpoint.");
        }

        return new EntraClientAuthFactory(config);
    }
    
    /// <summary>
    /// Entra Auth is provided at the route scope. The runtime step is a no-op.
    /// </summary>
    /// <param name="app"></param>
    /// <param name="route"></param>
    public void ConfigureRoute(WebApplication app, IEndpointConventionBuilder route)
    {
        route.RequireAuthorization(_id);
    }

    public object WriteDebug()
    {
        return new
        {
            Type = "Entra"
        };
    }
}