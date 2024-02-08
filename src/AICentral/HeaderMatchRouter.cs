using AICentral.Core;

namespace AICentral;

public class HeaderMatchRouter
{
    private readonly string _hostName;

    public HeaderMatchRouter(string hostName)
    {
        _hostName = hostName;
    }

    public object WriteDebug()
    {
        return new { Host = _hostName };
    }

    public IEnumerable<RouteHandlerBuilder> BuildRoutes(WebApplication application,
        Func<HttpContext, string?, AICallType, CancellationToken, Task<AICentralResponse>> handler)
    {
        yield return application.MapMethods(
                "/openai/images/generations:submit",
                new[] { "Post" },
                async (HttpContext ctx, CancellationToken cancellationToken) =>
                    (await handler(ctx, null, AICallType.DALLE2, cancellationToken)).ResultHandler)
            .RequireHost(_hostName);

        yield return application.MapMethods(
                "/openai/operations/{*:rest}",
                new[] { "Get" },
                async (HttpContext ctx, CancellationToken cancellationToken) =>
                    (await handler(ctx, null, AICallType.Operations, cancellationToken)).ResultHandler)
            .RequireHost(_hostName);

        yield return application.MapMethods(
                "/openai/deployments/{deploymentName}/images/generations",
                new[] { "Post" },
                async (HttpContext ctx, CancellationToken cancellationToken, string deploymentName) =>
                    (await handler(ctx, deploymentName, AICallType.DALLE3, cancellationToken)).ResultHandler)
            .RequireHost(_hostName);

        yield return application.MapMethods(
                "/openai/deployments/{deploymentName}/audio/transcriptions",
                new[] { "Post" },
                async (HttpContext ctx, CancellationToken cancellationToken, string deploymentName) =>
                    (await handler(ctx, deploymentName, AICallType.Transcription, cancellationToken)).ResultHandler)
            .RequireHost(_hostName);

        yield return application.MapMethods(
                "/openai/deployments/{deploymentName}/audio/translations",
                new[] { "Post" },
                async (HttpContext ctx, CancellationToken cancellationToken, string deploymentName) =>
                    (await handler(ctx, deploymentName, AICallType.Translation, cancellationToken)).ResultHandler)
            .RequireHost(_hostName);

        yield return application.MapMethods(
                "/openai/deployments/{deploymentName}/chat/completions",
                new[] { "Post" },
                async (HttpContext ctx, CancellationToken cancellationToken, string deploymentName) =>
                    (await handler(ctx, deploymentName, AICallType.Chat, cancellationToken)).ResultHandler)
            .RequireHost(_hostName);

        yield return application.MapMethods(
                "/openai/deployments/{deploymentName}/embeddings",
                new[] { "Post" },
                async (HttpContext ctx, CancellationToken cancellationToken, string deploymentName) =>
                    (await handler(ctx, deploymentName, AICallType.Embeddings, cancellationToken)).ResultHandler)
            .RequireHost(_hostName);

        yield return application.MapMethods(
                "/openai/deployments/{deploymentName}/completions",
                new[] { "Post" },
                async (HttpContext ctx, CancellationToken cancellationToken, string deploymentName) =>
                    (await handler(ctx, deploymentName, AICallType.Completions, cancellationToken)).ResultHandler)
            .RequireHost(_hostName);

        yield return application.MapMethods(
                "{*:rest}",
                new[] { "Get", "Post", "Delete" },
                async (HttpContext ctx, CancellationToken cancellationToken) =>
                    (await handler(ctx, null, AICallType.Other, cancellationToken)).ResultHandler)
            .RequireHost(_hostName);
    }

    public static HeaderMatchRouter WithHostHeader(string host)
    {
        return new HeaderMatchRouter(Guard.NotNullOrEmptyOrWhitespace(host, nameof(host)));
    }
}