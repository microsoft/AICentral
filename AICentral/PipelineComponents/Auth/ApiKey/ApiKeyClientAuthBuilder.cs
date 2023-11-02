using AICentral.Configuration.JSON;
using AICentral.PipelineComponents.Auth.AllowAnonymous;

namespace AICentral.PipelineComponents.Auth.ApiKey;

public class ApiKeyClientAuthBuilder : IAICentralClientAuthBuilder
{
    private readonly string _headerName;
    private readonly ConfigurationTypes.ApiKeyClientAuth[] _clients;
    private readonly string _policyId = Guid.NewGuid().ToString();

    public ApiKeyClientAuthBuilder(string headerName,
        ConfigurationTypes.ApiKeyClientAuth[] clients)
    {
        _headerName = headerName;
        _clients = clients;
    }

    public void RegisterServices(IServiceCollection services)
    {
        var schemeName = $"AICentralApiKey_{_policyId}";
        services.AddAuthentication().AddScheme<ApiKeyOptions, ApiKeyAuthenticationHandler>(schemeName, options =>
        {
            options.Clients = _clients;
            options.HeaderName = _headerName;
        });

        services.AddAuthorizationBuilder().AddPolicy(_policyId,
            policyBuilder => policyBuilder
                .RequireAuthenticatedUser()
                .AddAuthenticationSchemes(schemeName));
    }

    public IAICentralClientAuthStep Build()
    {
        return new ApiKeyClientAuthProvider(_policyId);
    }

    public static IAICentralClientAuthBuilder BuildFromConfig(
        IConfigurationSection configurationSection,
        Dictionary<string, string> parameters)
    {
        var clientKeys = configurationSection
            .GetRequiredSection("Properties")
            .GetRequiredSection("Clients")
            .Get<ConfigurationTypes.ApiKeyClientAuth[]>()!;

        return new ApiKeyClientAuthBuilder(
            parameters["HeaderName"],
            clientKeys
        );
    }

    public static string ConfigName => "ApiKey";
}