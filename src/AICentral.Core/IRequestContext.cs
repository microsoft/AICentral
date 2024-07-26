using System.Security.Claims;
using System.Security.Principal;

namespace AICentral.Core;

public interface IRequestContext
{
    T GetRequiredService<T>() where T : notnull;
    DateTimeOffset Now { get; }
    IHeaderDictionary ResponseHeaders { get;  }
    Stream RequestBody { get;  }
    QueryString QueryString { get;  }
    string RequestMethod { get;  }
    IServiceProvider RequestServices { get;  }
    string? UserName { get;  }
    IHeaderDictionary RequestHeaders { get;  }
    ClaimsPrincipal User { get;  }
    string RequestEncodedUrl { get;  }
    IFormCollection Form { get;  }
    HttpResponse Response { get;  }
    string RemoteIpAddress { get;  }
    PathString RequestPath { get;  }
    string RequestScheme { get;  }
    HostString RequestHost { get;  }
    bool HasJsonContentType();
    bool SupportsTrailers();
    void DeclareTrailer(string trailerHeader);
    void AppendTrailer(string trailerName, string trailerValue);
    string GetMultipartBoundary();
    string GetClientForLoggingPurposes();
    
    IResponseHandler CreateStreamResponseHandler();
    IResponseHandler CreateJsonResponseHandler();
    IResponseHandler CreateServerSideEventResponseHandler();
}