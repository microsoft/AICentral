using System.Security.Claims;
using AICentral.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace AICentralAzFunctions;

public class AzureFunctionsRequestContext : IRequestContext
{
    private readonly HttpRequestData _requestData;

    public AzureFunctionsRequestContext(HttpRequestData requestData)
    {
        _requestData = requestData;
    }

    public ILogger<T> GetLogger<T>() where T : notnull
    {
        throw new NotImplementedException();
    }

    public DateTimeOffset Now { get; }
    public IHeaderDictionary ResponseHeaders { get; }
    public Stream RequestBody { get; }
    public Dictionary<string, StringValues> QueryString { get; }
    public string RequestMethod { get; }
    public IServiceProvider RequestServices { get; }
    public string? UserName { get; }
    public IHeaderDictionary RequestHeaders { get; }
    public ClaimsPrincipal User { get; }
    public string RequestEncodedUrl { get; }
    public IFormCollection Form { get; }
    public HttpResponse Response { get; }
    public string RemoteIpAddress { get; }
    public PathString RequestPath { get; }
    public string RequestScheme { get; }
    public HostString RequestHost { get; }
    public bool HasJsonContentType()
    {
        throw new NotImplementedException();
    }

    public bool SupportsTrailers()
    {
        throw new NotImplementedException();
    }

    public void DeclareTrailer(string trailerHeader)
    {
        throw new NotImplementedException();
    }

    public void AppendTrailer(string trailerName, string trailerValue)
    {
        throw new NotImplementedException();
    }

    public string GetMultipartBoundary()
    {
        throw new NotImplementedException();
    }

    public string GetClientForLoggingPurposes()
    {
        throw new NotImplementedException();
    }

    public IResponseTransformer CreateJsonResponseTransformer()
    {
        throw new NotImplementedException();
    }
}