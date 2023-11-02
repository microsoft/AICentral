using AICentral.PipelineComponents.Auth.AllowAnonymous;

namespace AICentral.PipelineComponents.Auth.ApiKey;

public class ApiKeyClientAuthBuilder : IAICentralClientAuthBuilder
{
    private readonly string _headerName;
    private readonly string _key1;
    private readonly string _key2;
    private readonly string _policyId = Guid.NewGuid().ToString();

    public ApiKeyClientAuthBuilder(string headerName, string key1, string key2)
    {
        _headerName = headerName;
        _key1 = key1;
        _key2 = key2;
    }

    public void RegisterServices(IServiceCollection services)
    {
        var schemeName = $"AICentralApiKey_{_policyId}";
        services.AddAuthentication().AddScheme<ApiKeyOptions, ApiKeyAuthenticationHandler>(schemeName, options =>
        {
            options.Key1 = _key1;
            options.Key2 = _key2;
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

    public static IAICentralClientAuthBuilder BuildFromConfig(IConfigurationSection configurationSection,
        Dictionary<string, string> parameters)
    {
        return new ApiKeyClientAuthBuilder(
            parameters["HeaderName"],
            parameters["Key1"],
            parameters["Key2"]
        );
    }

    public static string ConfigName => "ApiKey";
}