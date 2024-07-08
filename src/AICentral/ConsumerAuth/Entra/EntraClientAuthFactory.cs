using AICentral.Core;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Tokens;

namespace AICentral.ConsumerAuth.Entra;

public class EntraClientAuthFactory : IPipelineStepFactory
{
    private readonly EntraClientAuthConfig _config;
    private readonly Action<AuthenticationBuilder, string> _registerAuthentication;
    private readonly Lazy<EntraClientAuthProvider> _provider;
    private readonly string _id;

    public EntraClientAuthFactory(EntraClientAuthConfig config, Action<AuthenticationBuilder, string> registerAuthentication)
    {
        _config = config;
        _registerAuthentication = registerAuthentication;
        _id = Guid.NewGuid().ToString();
        _provider = new Lazy<EntraClientAuthProvider>(() => new EntraClientAuthProvider());
    }

    /// <summary>
    /// Add an AAD provider for this particular config section.
    /// </summary>
    public void RegisterServices(IServiceCollection services)
    {
        _registerAuthentication(services.AddAuthentication(), _id);
        
        services.AddAuthorizationBuilder().AddPolicy(_id, policyBuilder =>
        {
            var builder=  policyBuilder.RequireAuthenticatedUser();
            if (_config.Requirements?.Roles != null)
            {
                builder.RequireRole(_config.Requirements.Roles);
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
        Guard.NotNull(customSection.Entra, nameof(customSection.Entra));

        if (customSection.Requirements == null || customSection.Requirements.Roles.IsNullOrEmpty() || customSection.Requirements.DisableScopeAndRoleCheck == true)
        {
            logger.LogWarning("Entra auth is configured but no roles are specified. Unless the Application is configured for specific user-assignment, this will allow all users and applications to access the endpoint.");
        }

        var section = config.ConfigurationSection!.GetSection("Properties");

        return new EntraClientAuthFactory(customSection, (builder, schemeId) =>
        {
            builder.AddMicrosoftIdentityWebApi(section, "Entra", schemeId);
            DisableScopeAndRoleCheckForManagedIdentityTokensDestinedForAzureOpenAI(customSection, builder, schemeId);
        });
    }

    private static void DisableScopeAndRoleCheckForManagedIdentityTokensDestinedForAzureOpenAI(
        EntraClientAuthConfig customSection, AuthenticationBuilder builder, string schemeId)
    {
        //To support scenarios such as passing through managed identity tokens deemed for Azure Open AI, where there won't be a scope or role in the token...
        if (customSection.Requirements?.DisableScopeAndRoleCheck ?? false)
        {
            builder.Services.Configure<JwtBearerOptions>(schemeId, options =>
            {
                options.Events.OnTokenValidated = _ => Task.CompletedTask;
            });
        }
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