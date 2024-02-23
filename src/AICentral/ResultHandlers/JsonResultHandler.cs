using System.Text.Json;

namespace AICentral.ResultHandlers;

public class JsonResultHandler : IResult, IDisposable
{
    private readonly HttpResponseMessage _openAiResponseMessage;
    private readonly JsonDocument _json;

    public JsonResultHandler(HttpResponseMessage openAiResponseMessage, JsonDocument json)
    {
        _openAiResponseMessage = openAiResponseMessage;
        _json = json;
    }

    public async Task ExecuteAsync(HttpContext context)
    {
        context.Response.StatusCode = (int)_openAiResponseMessage.StatusCode;
        context.Response.ContentType = "application/json";
        await using var utf8Writer = new Utf8JsonWriter(context.Response.BodyWriter, new JsonWriterOptions { Indented = false });
        _json.WriteTo(utf8Writer);
        await utf8Writer.FlushAsync();
    }

    public void Dispose()
    {
        _openAiResponseMessage.Dispose();
    }
}