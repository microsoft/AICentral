using System.Net;
using System.Text.Json;

namespace AICentral.ResultHandlers;

public class JsonResultHandler : IResult
{
    private readonly HttpStatusCode _statusCode;
    private readonly JsonDocument _json;

    public JsonResultHandler(HttpStatusCode statusCode, JsonDocument json)
    {
        _statusCode = statusCode;
        _json = json;
    }

    public async Task ExecuteAsync(HttpContext context)
    {
            context.Response.StatusCode = (int)_statusCode;
            context.Response.ContentType = "application/json";
            await using var utf8Writer = new Utf8JsonWriter(context.Response.BodyWriter, new JsonWriterOptions { Indented = false });
            _json.WriteTo(utf8Writer);
            await utf8Writer.FlushAsync();
    }
}