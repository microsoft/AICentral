using AICentral;
using AICentral.Configuration;
using AICentral.ConsumerAuth.Entra;
using AICentral.Core;
using AICentral.Endpoints;
using AICentral.Endpoints.AzureOpenAI;
using AICentral.Endpoints.AzureOpenAI.Authorisers.BearerPassThroughWithAdditionalKey;
using AICentral.EndpointSelectors.Single;
using AICentral.Logging.PIIStripping;
using AICentral.RequestFiltering;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.Validators;

namespace AICentralAzFunctions.Quickstarts;

public static class APImProxyWithCosmosLogging
{
    public class Config
    {
        public string? TenantId { get; init; }
        public string? ApimEndpointUri { get; init; }
        public string? IncomingClaimName { get; init; }
        public string? CosmosAccountEndpoint { get; init; }
        public string? TextAnalyticsEndpoint { get; init; }
        public string? TextAnalyticsKey { get; init; }
        public string? StorageUri { get; init; }
        public ClaimValueToSubscriptionKey[]? ClaimsToKeys { get; init; }
        public string[]? AllowedChatImageUriHostNames { get; init; }
    }

    public static AICentralPipelineAssembler BuildAssembler(Config config)
    {
        var tenantId = Guard.NotNull(config.TenantId, nameof(config.TenantId));
        var apimEndpointUri = Guard.NotNull(config.ApimEndpointUri, nameof(config.ApimEndpointUri));
        var textAnalyticsEndpoint = Guard.NotNull(config.TextAnalyticsEndpoint, nameof(config.TextAnalyticsEndpoint));
        var textAnalyticsKey = Guard.NotNull(config.TextAnalyticsKey, nameof(config.TextAnalyticsKey)); //RBAC not support
        var incomingClaimName = Guard.NotNull(config.IncomingClaimName, nameof(config.IncomingClaimName));
        var cosmosAccountEndpoint = Guard.NotNull(config.CosmosAccountEndpoint, nameof(config.CosmosAccountEndpoint));
        var storageUri = Guard.NotNull(config.StorageUri, nameof(config.StorageUri));
        var claimsToKeys = config.ClaimsToKeys ?? [];
        var allowedChatImageHostNames = config.AllowedChatImageUriHostNames ?? [];

        var cosmosLoggerStepName = "cosmosLogger";
        var cosmosLoggerConfig = new PIIStrippingLoggerConfig()
        {
            UseManagedIdentities = true,
            CosmosContainer = "aoaiLogContainer",
            CosmosDatabase = "aoaiLogs",
            QueueName = "prompt-and-response-queue",
            CosmosAccountEndpoint = cosmosAccountEndpoint,
            TextAnalyticsEndpoint = textAnalyticsEndpoint,
            TextAnalyticsKey = textAnalyticsKey,
            StorageUri = storageUri
        };

        var downstreamEndpointDispatcherFactory = new DownstreamEndpointDispatcherFactory(
            new AzureOpenAIDownstreamEndpointAdapterFactory(
                "apim",
                apimEndpointUri!,
                new BearerPassThroughWithAdditionalKeyAuthFactory(
                    new BearerPassThroughWithAdditionalKeyAuthFactoryConfig()
                    {
                        IncomingClaimName = incomingClaimName,
                        KeyHeaderName = "api-key",
                        ClaimsToKeys = claimsToKeys
                    }),
                new Dictionary<string, string>(),
                new Dictionary<string, string>(),
                false));

        var chatImageFilterStepName = "chatImageFilter";
        var chatImageFilter = new RequestFilteringProviderFactory(new RequestFilteringConfiguration()
        {
            AllowedHostNames = allowedChatImageHostNames,
            AllowDataUris = true
        });

        return new AICentralPipelineAssembler(
            _ => new HostNameMatchRouter("*"),
            new Dictionary<string, IPipelineStepFactory>()
            {
                ["auth"] = new EntraClientAuthFactory(new EntraClientAuthConfig()
                {
                    Entra = new MicrosoftIdentityApplicationOptions()
                    {
                        TenantId = tenantId,
                        ClientId = "ignored-as-not-exchanging-codes-for-tokens",
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
                            options.TokenValidationParameters.EnableAadSigningKeyIssuerValidation();
                        }, options =>
                        {
                            options.TenantId = tenantId;
                            options.Instance = "https://login.microsoftonline.com/";
                            options.ClientId = "ignored-as-not-exchanging-codes-for-tokens";
                        }, schemeId);
                
                    builder.Services.Configure<JwtBearerOptions>(
                        schemeId,
                        jwtBearerOptions => { jwtBearerOptions.Events.OnTokenValidated = _ => Task.CompletedTask; });
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
            new Dictionary<string, IPipelineStepFactory>()
            {
                [cosmosLoggerStepName] = new PIIStrippingLoggerFactory(cosmosLoggerStepName, cosmosLoggerConfig),
                [chatImageFilterStepName] = chatImageFilter
            },
            new Dictionary<string, IRouteProxy>(),
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
                    Steps =
                    [
                        chatImageFilterStepName,
                        cosmosLoggerStepName
                    ],
                    RouteProxies = []
                }
            ],
            false
        );
    }
}