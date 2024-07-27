using System.Text.Json;
using System.Text.Json.Nodes;
using AICentral.Core;
using AICentral.ResultHandlers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace AICentral.AzureAISearchVectorizationProxy;

public class ProxyContext: HttpContextWrapper, IDisposable, IAsyncDisposable
{
    private readonly Uri _newUrl;
    private readonly JsonNode _incomingDocument;
    private readonly MemoryStream _requestStream;
    private readonly Dictionary<string, StringValues> _queryString;

    public ProxyContext(
        HttpContext ctx, 
        Uri relativeUrl, 
        string apiVersion,
        object requestContent,
        JsonNode incomingDocument): base(ctx)
    {
        _newUrl = relativeUrl;
        _incomingDocument = incomingDocument;
        var ms = new MemoryStream();
        JsonSerializer.Serialize(ms, requestContent);
        ms.Flush();
        ms.Position = 0;
        _requestStream = ms;
        _queryString = new Dictionary<string, StringValues>()
        {
            ["api-version"] = apiVersion
        };
    }

    public override PathString RequestPath => new(_newUrl.AbsolutePath);

    public override IResponseHandler CreateJsonResponseHandler()
    {   
        return new JsonResponseHandler(new AdaptJsonToAzureAISearchTransformer(_incomingDocument));
    }

    public override bool HasJsonContentType() => true;

    public override Stream RequestBody => _requestStream;

    public override Dictionary<string, StringValues> QueryString => _queryString;

    public void Dispose()
    {
        _requestStream.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await _requestStream.DisposeAsync();
    }
}