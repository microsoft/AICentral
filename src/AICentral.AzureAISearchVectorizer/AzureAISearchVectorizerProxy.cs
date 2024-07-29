using System.Text.Json.Nodes;
using AICentral.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace AICentral.AzureAISearchVectorizationProxy;

public class AzureAISearchVectorizerProxy : IRouteProxy
{
    private readonly string _proxyPath;
    private readonly string _embeddingsName;
    private readonly string _apiVersion;

    public AzureAISearchVectorizerProxy(string proxyPath, string embeddingsName, string apiVersion)
    {
        _proxyPath = proxyPath;
        _embeddingsName = embeddingsName;
        _apiVersion = apiVersion;
    }

    public RouteHandlerBuilder MapRoute(WebApplication application, AIHandler handler)
    {
        return application.MapMethods(
            _proxyPath,
            new[] { "Post" },
            async (HttpContext ctx, CancellationToken cancellationToken) =>
            {
                //break down the input object.
                var incomingJson = await JsonNode.ParseAsync(ctx.Request.Body, cancellationToken: cancellationToken);
                if (incomingJson == null)
                {
                    return Results.BadRequest(new { message = "This endpoint only supports text embedding requests." });
                }

                //only work for text inputs
                var input = incomingJson["values"]?[0]?["data"];
                if (input == null)
                {
                    return Results.BadRequest(new { message = "This endpoint only supports text embedding requests." });
                }

                if (input["text"] == null)
                {
                    return Results.BadRequest(new { message = "This endpoint only supports text embedding requests." });
                }

                var textElement = input["text"]!;

                var mappedObject = new
                {
                    input = textElement.GetValue<string>()
                };

                return (await handler(
                        new ProxyContext(
                            ctx,
                            new Uri($"/openai/deployments/{_embeddingsName}/embeddings"),
                            _apiVersion,
                            mappedObject,
                            incomingJson
                        ),
                        _embeddingsName,
                        null,
                        AICallType.Embeddings,
                        cancellationToken))
                    .ResultHandler;
            });
    }

    public static string ConfigName => "AzureAISearchVectorizerProxy";

    public static IRouteProxy BuildFromConfig(ILogger logger, TypeAndNameConfig config)
    {
        var typedConfig = config.TypedProperties<Config>();
        var embeddingsDeploymentName = Guard.NotNullOrEmptyOrWhitespace(typedConfig.EmbeddingsDeploymentName,
            nameof(typedConfig.EmbeddingsDeploymentName));
        var apiVersion = Guard.NotNullOrEmptyOrWhitespace(typedConfig.OpenAIApiVersion,
            nameof(typedConfig.OpenAIApiVersion));
        var proxyPath = Guard.NotNullOrEmptyOrWhitespace(typedConfig.ProxyPath,
            nameof(typedConfig.ProxyPath));

        if (!proxyPath.StartsWith("/"))
        {
            throw new ArgumentException($"Proxy Path must start with '/' for proxy {config.Name}");
        }

        return new AzureAISearchVectorizerProxy(proxyPath, embeddingsDeploymentName, apiVersion);
    }

    public object WriteDebug()
    {
        return new
        {
            Type = ConfigName,
            EmbeddingsDeployment = _embeddingsName,
            ApiVersion = _apiVersion,
            ProxyPath = _proxyPath
        };
    }
}

internal class Config
{
    public string? EmbeddingsDeploymentName { get; init; }
    public string? ProxyPath { get; init; }
    public string? OpenAIApiVersion { get; init; }
}