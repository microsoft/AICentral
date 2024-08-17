using System.Security.Claims;
using Microsoft.Extensions.Primitives;

namespace AICentral.Core;

public class ExtendableWrappedContext : IRequestContext
{
    private readonly IRequestContext _context;

    public ExtendableWrappedContext(IRequestContext context)
    {
        _context = context;
    }

    public ILogger<T> GetLogger<T>() where T : notnull => _context.GetLogger<T>();

    public DateTimeOffset Now => _context.Now;
    public IHeaderDictionary ResponseHeaders => _context.ResponseHeaders;
    public virtual Stream RequestBody => _context.RequestBody;
    public virtual Dictionary<string, StringValues> QueryString => _context.QueryString;
    public string RequestMethod => _context.RequestMethod;
    public IServiceProvider RequestServices => _context.RequestServices;
    public string? UserName => _context.UserName;
    public IHeaderDictionary RequestHeaders => _context.RequestHeaders;
    public ClaimsPrincipal User => _context.User;
    public string RequestEncodedUrl => _context.RequestEncodedUrl;
    public IFormCollection Form => _context.Form;
    public IAICentralResponse Response => _context.Response;
    public string RemoteIpAddress => _context.RemoteIpAddress;
    public virtual PathString RequestPath => _context.RequestPath;
    public string RequestScheme => _context.RequestScheme;
    public HostString RequestHost => _context.RequestHost;
    public bool HasJsonContentType() => _context.HasJsonContentType();
    
    public string GetMultipartBoundary() => _context.GetMultipartBoundary();

    public string GetClientForLoggingPurposes() => _context.GetClientForLoggingPurposes();

    public virtual IResponseTransformer CreateJsonResponseTransformer() => _context.CreateJsonResponseTransformer();

    public bool ResponseSupportsTrailers() => _context.ResponseSupportsTrailers();

    public void ResponseDeclareTrailer(string header) => _context.ResponseDeclareTrailer(header);

    public void ResponseSetHeader(string headerName, string headerValue) => _context.ResponseSetHeader(headerName, headerValue);

    public void ResponseAppendTrailer(string trailerName, string trailerValue) => _context.ResponseAppendTrailer(trailerName, trailerValue);
}