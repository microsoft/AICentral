using System.Security.Claims;
using AICentral.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace AICentralAzFunctions;

public class AzureFunctionsWrappedContext : IRequestContext
{
    private readonly HttpRequestData _request;
    private readonly FunctionContext _executionContext;
    private readonly HeaderDictionary _headerDictionary;
    private readonly ClaimsPrincipal? _claimsPrincipal;
    private readonly HeaderDictionary _responseHeaders;
    private readonly IAICentralResponse _response;
    private readonly Dictionary<string ,StringValues> _queryString;

    public AzureFunctionsWrappedContext(
        HttpRequestData request,
        FunctionContext executionContext, 
        HttpResponseData response)
    {
        _request = request;
        _executionContext = executionContext;
        _queryString = request.Query.AllKeys.ToDictionary(x => x!, x => new StringValues(request.Query[x]));
        _headerDictionary = new HeaderDictionary(request.Headers.ToDictionary(x => x.Key, x => new StringValues(x.Value.ToArray())));
        _claimsPrincipal = request.Identities.Any() ? new ClaimsPrincipal(request.Identities.First()) : null;
        _responseHeaders = new HeaderDictionary();
        _response = new AzureFunctionResponseWrapper(response);
    }

    public ILogger<T> GetLogger<T>() where T : notnull => _executionContext.InstanceServices.GetRequiredService<ILogger<T>>();

    public bool HasJsonContentType() => 
        _request.Headers.TryGetValues("content-type", out var vals) &&
        MediaTypeHeaderValue.TryParse(vals.First(), out var parsed) && 
        (parsed.MediaType.Value?.Contains("json", StringComparison.InvariantCultureIgnoreCase) ?? false);
    
    public string GetMultipartBoundary()
    {
        throw new NotImplementedException();
    }

    public string GetClientForLoggingPurposes()
    {
        var identity = _request.Identities.FirstOrDefault();
        if (identity == null) return string.Empty;
        
        if (identity.Name != null)
            return identity.Name;
        var claim1 = identity.FindFirst(x => x.Type == "appid");
        if (claim1 != null)
            return claim1.Value;
        var claim2 = identity.FindFirst (x => x.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");
        return claim2 != null ? claim2.Value : string.Empty;
    }

    public IResponseTransformer CreateJsonResponseTransformer()
    {
        //TODO - get a vectoriser route working as-well.
        return new EmptyResponseTransformer();
    }

    public bool ResponseSupportsTrailers()
    {
        return false;
    }

    public void ResponseDeclareTrailer(string header)
    {
    }

    public void ResponseSetHeader(string headerName, string headerValue)
    {
        _response.SetHeader(headerName, headerValue);
    }

    public void ResponseAppendTrailer(string trailerName, string trailerValue)
    {
        //don't think az functions supports these...
    }

    public DateTimeOffset Now => _executionContext.InstanceServices.GetRequiredService<IDateTimeProvider>().Now;
    public IHeaderDictionary ResponseHeaders => _responseHeaders;
    public Stream RequestBody => _request.Body;
    public Dictionary<string, StringValues> QueryString => _queryString;

    public string RequestMethod => _request.Method;
    public IServiceProvider RequestServices => _executionContext.InstanceServices;
    public string? UserName => GetClientForLoggingPurposes();

    public IHeaderDictionary RequestHeaders => _headerDictionary;
    public ClaimsPrincipal User => _claimsPrincipal!;
    public string RequestEncodedUrl => _request.Url.ToString();
    public IFormCollection Form => throw new NotSupportedException();
    public IAICentralResponse Response => _response;
    public string RemoteIpAddress => string.Empty;
    public PathString RequestPath => PathString.FromUriComponent(_request.Url);
    public string RequestScheme => _request.Url.Scheme;
    public HostString RequestHost => HostString.FromUriComponent(_request.Url);
}