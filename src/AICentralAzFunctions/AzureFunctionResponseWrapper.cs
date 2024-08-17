using System.Net;
using AICentral.Core;
using Microsoft.Azure.Functions.Worker.Http;

namespace AICentralAzFunctions;

public class AzureFunctionResponseWrapper : IAICentralResponse
{
    private readonly HttpResponseData _response;

    public AzureFunctionResponseWrapper(HttpResponseData response)
    {
        _response = response;
    }

    public int StatusCode
    {
        set => _response.StatusCode = (HttpStatusCode)value;
    }

    public Stream Body => _response.Body;
    
    public void SetHeader(string headerName, string? headerValue)
    {
        _response.Headers.Add(headerName, headerValue);
    }
}