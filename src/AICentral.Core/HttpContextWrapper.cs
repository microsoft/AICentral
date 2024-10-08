using System.Security.Claims;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;

namespace AICentral.Core;

public class HttpContextWrapper : IRequestContext
{
    private readonly HttpContext _ctx;
    private readonly Dictionary<string,StringValues> _queryStringParts;
    private WrappedHttpResponse _wrappedHttpResponse;

    public HttpContextWrapper(HttpContext ctx)
    {
        _ctx = ctx;
        var queryStringParts = QueryHelpers.ParseQuery(ctx.Request.QueryString.Value ?? "");
        queryStringParts.Remove("x-aicentral-affinity-key");
        _queryStringParts = queryStringParts;
        _wrappedHttpResponse = new WrappedHttpResponse(_ctx.Response);
    }
    
    public ILogger<T> GetLogger<T>() where T : notnull => _ctx.RequestServices.GetRequiredService<ILogger<T>>();

    public DateTimeOffset Now => _ctx.RequestServices.GetRequiredService<IDateTimeProvider>().Now;

    public IHeaderDictionary ResponseHeaders => _ctx.Response.Headers;
    public virtual Stream RequestBody => _ctx.Request.Body;
    public virtual Dictionary<string, StringValues> QueryString => _queryStringParts;
    public string RequestMethod => _ctx.Request.Method;
    public IServiceProvider RequestServices => _ctx.RequestServices;
    public string? UserName => _ctx.User.Identity?.Name;

    public string RemoteIpAddress =>
        _ctx.Features.Get<IHttpConnectionFeature>()?.RemoteIpAddress?.ToString() ??
        _ctx.Connection.RemoteIpAddress?.ToString() ?? string.Empty;

    public IHeaderDictionary RequestHeaders => _ctx.Request.Headers;
    public ClaimsPrincipal User => _ctx.User;
    public string RequestEncodedUrl => _ctx.Request.GetEncodedUrl();
    public IFormCollection Form => _ctx.Request.Form;
    public IAICentralResponse Response => _wrappedHttpResponse;

    public virtual PathString RequestPath => _ctx.Request.Path;
    public string RequestScheme => _ctx.Request.Scheme;
    public HostString RequestHost => _ctx.Request.Host;
    public virtual bool HasJsonContentType() => _ctx.Request.HasJsonContentType();
    
    public string GetMultipartBoundary() => _ctx.Request.GetMultipartBoundary();

    public string GetClientForLoggingPurposes()
    {
        if (_ctx.User.Identity?.Name != null)
        {
            return _ctx.User.Identity.Name;
        }
        var appIdClaim = _ctx.User.Claims.FirstOrDefault(x => x.Type == "appid");
        if (appIdClaim != null)
        {
            return appIdClaim.Value;
        }
        var subjectClaim = _ctx.User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier);
        if (subjectClaim != null)
        {
            return subjectClaim.Value;
        }

        return string.Empty;
    }

    public virtual IResponseTransformer CreateJsonResponseTransformer()
    {
        return new EmptyResponseTransformer();
    }

    public bool ResponseSupportsTrailers()
    {
        return _ctx.Response.SupportsTrailers();
    }

    public void ResponseDeclareTrailer(string header)
    {
        _ctx.Response.DeclareTrailer(header);
    }

    public void ResponseSetHeader(string headerName, string headerValue)
    {
        _ctx.Response.Headers[headerName] = headerValue;
    }

    public void ResponseAppendTrailer(string trailerName, string trailerValue)
    {
        _ctx.Response.AppendTrailer(trailerName, trailerValue);
    }
}