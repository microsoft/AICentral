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

    public IEnumerable<RouteHandlerBuilder> BuildRoutes(WebApplication application, Delegate handler)
    {
        yield return application.MapMethods(
                "/openai/images/generations:submit",
                new[] { "Post" }, handler)
            .RequireHost(_hostName);

        yield return application.MapMethods(
                "/openai/operations/images/{operationId}",
                new[] { "Get", "Delete" }, handler)
            .RequireHost(_hostName);

        yield return application.MapMethods(
                "/openai/deployments/{deploymentName}/images/generations",
                new[] { "Post" }, handler)
            .RequireHost(_hostName);

        yield return application.MapMethods(
                "/openai/deployments/{deploymentName}/audio/transcriptions",
                new[] { "Post" }, handler)
            .RequireHost(_hostName);

        yield return application.MapMethods(
                "/openai/deployments/{deploymentName}/audio/translations",
                new[] { "Post" }, handler)
            .RequireHost(_hostName);

        yield return application.MapMethods(
                "/openai/deployments/{deploymentName}/chat/completions",
                new[] { "Post" }, handler)
            .RequireHost(_hostName);

        yield return application.MapMethods(
                "/openai/deployments/{deploymentName}/embeddings",
                new[] { "Post" }, handler)
            .RequireHost(_hostName);

        yield return application.MapMethods(
                "/openai/deployments/{deploymentName}/completions",
                new[] { "Post" }, handler)
            .RequireHost(_hostName);

    }

    public static HeaderMatchRouter WithHostHeader(string host)
    {
        return new HeaderMatchRouter(Guard.NotNullOrEmptyOrWhitespace(host, nameof(host)));
    }
}