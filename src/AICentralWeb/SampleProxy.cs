using AICentral.Core;

namespace AICentralWeb;

public class SampleProxy : IRouteProxy
{
    public RouteHandlerBuilder MapRoute(WebApplication application, AIHandler handler)
    {
        return application.MapMethods(
            "/mappedembeddings",
            new[] { "Post" },
            async (HttpContext ctx, CancellationToken cancellationToken, string deploymentName) =>
                (await handler(WrapContext(ctx), deploymentName, null, AICallType.Embeddings, cancellationToken))
                .ResultHandler);

    }

    private IRequestContext WrapContext(HttpContext ctx)
    {
        throw new NotImplementedException();
    }
}