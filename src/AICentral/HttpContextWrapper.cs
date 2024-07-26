using System.Security.Claims;
using System.Security.Principal;
using AICentral.Core;
using AICentral.ResultHandlers;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Http.Features;

namespace AICentral;

internal class HttpContextWrapper : IRequestContext
{
    private readonly HttpContext _ctx;

    public HttpContextWrapper(HttpContext ctx)
    {
        _ctx = ctx;
    }
    
    public T GetRequiredService<T>()
    {
        return _ctx.RequestServices.GetRequiredService<T>();
    }

    public IHeaderDictionary ResponseHeaders => _ctx.Response.Headers;
    public Stream RequestBody => _ctx.Request.Body;
    public QueryString QueryString => _ctx.Request.QueryString;
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
    public HttpResponse Response => _ctx.Response;
    public PathString RequestPath => _ctx.Request.Path;
    public string RequestScheme => _ctx.Request.Scheme;
    public HostString RequestHost => _ctx.Request.Host;
    public bool HasJsonContentType() => _ctx.Request.HasJsonContentType();

    public bool SupportsTrailers() => _ctx.Response.SupportsTrailers();

    public void DeclareTrailer(string trailerHeader)
    {
        _ctx.Response.DeclareTrailer(trailerHeader);
    }

    public void AppendTrailer(string trailerName, string trailerValue)
    {
        _ctx.Response.AppendTrailer(trailerName, trailerValue);
    }

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
}