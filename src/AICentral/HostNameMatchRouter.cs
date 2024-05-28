using AICentral.Core;

namespace AICentral;

public class HostNameMatchRouter
{
    private readonly string[] _hostNames;

    public HostNameMatchRouter(string hostName)
    {
        _hostNames = hostName == "*" ? [] : [hostName];
    }

    public object WriteDebug()
    {
        return new { Host = _hostNames };
    }

    public IEnumerable<RouteHandlerBuilder> BuildRoutes(
        WebApplication application,
        AIHandler handler)
    {
        yield return application.MapMethods(
                "/openai/images/generations:submit",
                new[] { "Post" },
                async (HttpContext ctx, CancellationToken cancellationToken) =>
                    (await handler(ctx, null, null, AICallType.DALLE2, cancellationToken)).ResultHandler)
            .RequireHost(_hostNames);

        yield return application.MapMethods(
                "/openai/operations/{*:rest}",
                new[] { "Get" },
                async (HttpContext ctx, CancellationToken cancellationToken) =>
                    (await handler(ctx, null, null, AICallType.Operations, cancellationToken)).ResultHandler)
            .RequireHost(_hostNames);

        yield return application.MapMethods(
                "/openai/deployments/{deploymentName}/images/generations",
                new[] { "Post" },
                async (HttpContext ctx, CancellationToken cancellationToken, string deploymentName) =>
                    (await handler(ctx, deploymentName, null, AICallType.DALLE3, cancellationToken)).ResultHandler)
            .RequireHost(_hostNames);

        yield return application.MapMethods(
                "/openai/deployments/{deploymentName}/audio/transcriptions",
                new[] { "Post" },
                async (HttpContext ctx, CancellationToken cancellationToken, string deploymentName) =>
                    (await handler(ctx, deploymentName, null, AICallType.Transcription, cancellationToken)).ResultHandler)
            .RequireHost(_hostNames);

        yield return application.MapMethods(
                "/openai/deployments/{deploymentName}/audio/translations",
                new[] { "Post" },
                async (HttpContext ctx, CancellationToken cancellationToken, string deploymentName) =>
                    (await handler(ctx, deploymentName, null, AICallType.Translation, cancellationToken)).ResultHandler)
            .RequireHost(_hostNames);

        yield return application.MapMethods(
                "/openai/deployments/{deploymentName}/chat/completions",
                new[] { "Post" },
                async (HttpContext ctx, CancellationToken cancellationToken, string deploymentName) =>
                    (await handler(ctx, deploymentName, null, AICallType.Chat, cancellationToken)).ResultHandler)
            .RequireHost(_hostNames);

        yield return application.MapMethods(
                "/openai/deployments/{deploymentName}/embeddings",
                new[] { "Post" },
                async (HttpContext ctx, CancellationToken cancellationToken, string deploymentName) =>
                    (await handler(ctx, deploymentName, null, AICallType.Embeddings, cancellationToken)).ResultHandler)
            .RequireHost(_hostNames);

        yield return application.MapMethods(
                "/openai/deployments/{deploymentName}/completions",
                new[] { "Post" },
                async (HttpContext ctx, CancellationToken cancellationToken, string deploymentName) =>
                    (await handler(ctx, deploymentName, null, AICallType.Completions, cancellationToken)).ResultHandler)
            .RequireHost(_hostNames);

        yield return application.MapMethods(
                "/openai/threads/{*:rest}",
                new[] { "Get", "Delete", "Post" },
                async (HttpContext ctx, CancellationToken cancellationToken) =>
                    (await handler(ctx, null, null, AICallType.Threads, cancellationToken)).ResultHandler)
            .RequireHost(_hostNames);

        yield return application.MapMethods(
                "/openai/assistants/{assistantId}/{*:rest}",
                new[] { "Get", "Delete", "Post" },
                async (HttpContext ctx, CancellationToken cancellationToken, string assistantId) =>
                    (await handler(ctx, null, assistantId, AICallType.Assistants, cancellationToken)).ResultHandler)
            .RequireHost(_hostNames);

        yield return application.MapMethods(
                "/openai/files",
                new[] { "Post" },
                async (HttpContext ctx, CancellationToken cancellationToken) =>
                    (await handler(ctx, null, null, AICallType.Files, cancellationToken)).ResultHandler)
            .RequireHost(_hostNames);

        yield return application.MapMethods(
                "/openai/assistants",
                new[] { "Post" },
                async (HttpContext ctx, CancellationToken cancellationToken) =>
                    (await handler(ctx, null, null, AICallType.Assistants, cancellationToken)).ResultHandler)
            .RequireHost(_hostNames);

        yield return application.MapMethods(
                "/openai/{*:rest}",
                new[] { "Get", "Post", "Delete" },
                async (HttpContext ctx, CancellationToken cancellationToken) =>
                    (await handler(ctx, null, null, AICallType.Other, cancellationToken)).ResultHandler)
            .RequireHost(_hostNames);
    }

    public static HostNameMatchRouter WithHostHeader(string host)
    {
        return new HostNameMatchRouter(Guard.NotNullOrEmptyOrWhitespace(host, nameof(host)));
    }
}