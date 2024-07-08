using AICentral;
using AICentral.Configuration;
using AICentral.ConsumerAuth.Entra;
using AICentral.Core;
using AICentral.Endpoints;
using AICentral.Endpoints.AzureOpenAI;
using AICentral.Endpoints.AzureOpenAI.Authorisers.BearerPassThroughWithAdditionalKey;
using AICentral.EndpointSelectors.Single;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Tokens;

namespace AICentralWeb.QuickStartConfigs;

public static class APImProxyWithCosmosLogging
{
    public class Config
    {
        public string? TenantId { get; init; }
        public string? ApimEndpointName { get; init; }
        public ClaimValueToSubscriptionKey[]? ClaimsToKeys { get; init; }
    }

    public static AICentralPipelineAssembler BuildAssembler(Config config)
    {
        Guard.NotNull(config.TenantId, nameof(config.TenantId));
        Guard.NotNull(config.ApimEndpointName, nameof(config.ApimEndpointName));
        Guard.NotNull(config.ClaimsToKeys, nameof(config.ClaimsToKeys));

        var downstreamEndpointDispatcherFactory = new DownstreamEndpointDispatcherFactory(
            new AzureOpenAIDownstreamEndpointAdapterFactory(
                "apim",
                config.ApimEndpointName!,
                new BearerPassThroughWithAdditionalKeyAuthFactory(
                    new BearerPassThroughWithAdditionalKeyAuthFactoryConfig()
                    {
                        IncomingClaimName = "appid",
                        KeyHeaderName = "api-key",
                        ClaimsToKeys = config.ClaimsToKeys!
                    }),
                new Dictionary<string, string>(),
                new Dictionary<string, string>(),
                false));

        return new AICentralPipelineAssembler(
            _ => new HostNameMatchRouter("*"),
            new Dictionary<string, IPipelineStepFactory>()
            {
                ["auth"] = new EntraClientAuthFactory(new EntraClientAuthConfig()
                {
                    Entra = new MicrosoftIdentityApplicationOptions()
                    {
                        ClientId = "https://cognitiveservices.azure.com",
                        TenantId = config.TenantId,
                        Audience = "https://cognitiveservices.azure.com",
                    }
                }, (builder, schemeId) =>
                {
                    builder
                        .AddMicrosoftIdentityWebApi(options =>
                        {
                            options.Audience = "https://cognitiveservices.azure.com";
                            options.TokenValidationParameters = new TokenValidationParameters()
                            {
                                ValidateIssuer = true,
                                ValidAudiences = new[] { "https://cognitiveservices.azure.com" }
                            };
                        }, options =>
                        {
                            options.TenantId = config.TenantId;
                            options.Instance = "https://login.microsoftonline.com/";
                            options.ClientId = "https://cognitiveservices.azure.com";
                        }, schemeId);
                    
                    builder.Services.Configure<JwtBearerOptions>(
                        schemeId,
                        jwtBearerOptions =>
                        {
                            jwtBearerOptions.Events.OnTokenValidated = _ => Task.CompletedTask;
                        });

                })
            },
            new Dictionary<string, IEndpointDispatcherFactory>()
            {
                ["test-endpoint"] = downstreamEndpointDispatcherFactory
            },
            new Dictionary<string, IEndpointSelectorFactory>()
            {
                ["default-endpoint-selector"] = new SingleEndpointSelectorFactory(downstreamEndpointDispatcherFactory)
            },
            new Dictionary<string, IPipelineStepFactory>(),
            [
                new PipelineConfig()
                {
                    Name = "APImProxy",
                    AuthProvider = "auth",
                    EndpointSelector = "default-endpoint-selector",
                    Host = "*",
                    OpenTelemetryConfig = new OTelConfig()
                    {
                        AddClientNameTag = true,
                        Transmit = true
                    },
                    Steps = []
                }
            ]
        );
    }
}