using System.Security.Claims;
using AICentral;
using AICentral.Core;
using AICentral.ResultHandlers;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Http.Features;

namespace AICentralWeb;

public class ProxyContext: HttpContextWrapper
{
    private readonly HttpContext _ctx;
    private readonly string _newUrl;

    public ProxyContext(HttpContext ctx, string newUrl): base(ctx)
    {
        _ctx = ctx;
        _newUrl = newUrl;
    }

    public override PathString RequestPath => new("/openai/deployments/foo/embeddings?api-version=2024-04-01");

    public override IResponseHandler CreateJsonResponseHandler()
    {   
        return new JsonResponseHandler(new AdaptJsonToAzureAISearchTransformer());
    }
}