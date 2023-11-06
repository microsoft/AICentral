using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;

namespace AICentral.PipelineComponents.Auth.Entra;

public class EntraClientAuthBuilder : IAICentralClientAuthBuilder
{
    private readonly IConfigurationSection _configSection;
    private readonly string _id;

    public EntraClientAuthBuilder(IConfigurationSection configSection)
    {
        _configSection = configSection;
        _id = Guid.NewGuid().ToString();
    }

    /// <summary>
    /// Add an AAD provider for this particular config section.
    /// </summary>
    public void RegisterServices(IServiceCollection services)
    {
        services.AddAuthentication().AddMicrosoftIdentityWebApi(_configSection, "Properties", _id);
        services.AddAuthorizationBuilder().AddPolicy(_id, policyBuilder => policyBuilder.RequireAuthenticatedUser().AddAuthenticationSchemes(_id));
    }

    public static string ConfigName => "Entra";

    public IAICentralClientAuthStep Build()
    {
        return new EntraClientAuthProvider(_id);
    }

    public static IAICentralClientAuthBuilder BuildFromConfig(
        IConfigurationSection configurationSection)
    {
        return new EntraClientAuthBuilder(configurationSection);
    }
}