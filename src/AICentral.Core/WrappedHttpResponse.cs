namespace AICentral.Core;

public class WrappedHttpResponse : IAICentralResponse
{
    private readonly HttpResponse _response;

    public WrappedHttpResponse(HttpResponse response)
    {
        _response = response;
    }

    public int StatusCode
    {
        set => _response.StatusCode = value;
    }

    public Stream Body => _response.Body;
    
    public void SetHeader(string headerName, string? headerValue)
    {
        _response.Headers[headerName] = headerValue;
    }
}