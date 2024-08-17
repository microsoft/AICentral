using System.Security.Claims;
using Microsoft.Extensions.Primitives;

namespace AICentral.Core;

public interface IRequestContext
{
    ILogger<T> GetLogger<T>() where T : notnull;
    DateTimeOffset Now { get; }
    IHeaderDictionary ResponseHeaders { get;  }
    Stream RequestBody { get;  }
    Dictionary<string, StringValues> QueryString { get;  }
    string RequestMethod { get;  }
    IServiceProvider RequestServices { get;  }
    string? UserName { get;  }
    IHeaderDictionary RequestHeaders { get;  }
    ClaimsPrincipal User { get;  }
    string RequestEncodedUrl { get;  }
    IFormCollection Form { get;  }
    IAICentralResponse Response { get;  }
    string RemoteIpAddress { get;  }
    PathString RequestPath { get;  }
    string RequestScheme { get;  }
    HostString RequestHost { get;  }
    bool HasJsonContentType();
    string GetMultipartBoundary();
    string GetClientForLoggingPurposes();
    IResponseTransformer CreateJsonResponseTransformer();
    bool ResponseSupportsTrailers();
    void ResponseDeclareTrailer(string header);
    void ResponseSetHeader(string headerName, string headerValue);
    void ResponseAppendTrailer(string trailerName, string trailerValue);
}

public interface IAICentralResponse
{
    int StatusCode { set; }
    Stream Body { get; }
    void SetHeader(string headerName, string? headerValue);
}