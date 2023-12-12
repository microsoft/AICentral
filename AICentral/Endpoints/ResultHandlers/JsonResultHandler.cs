using System.Globalization;
using AICentral.Core;

namespace AICentral.Endpoints.ResultHandlers;

public class JsonResultHandler : IResult, IDisposable
{
    private readonly HttpResponseMessage _openAiResponseMessage;

    public JsonResultHandler(HttpResponseMessage openAiResponseMessage)
    {
        _openAiResponseMessage = openAiResponseMessage;
    }

    public async Task ExecuteAsync(HttpContext context)
    {
        context.Response.StatusCode = (int)_openAiResponseMessage.StatusCode;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(await _openAiResponseMessage.Content.ReadAsStringAsync());
    }

    public void Dispose()
    {
        _openAiResponseMessage.Dispose();
    }
}