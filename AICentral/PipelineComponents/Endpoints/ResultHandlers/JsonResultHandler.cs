using System.Globalization;
using Microsoft.Extensions.Primitives;

namespace AICentral.PipelineComponents.Endpoints.ResultHandlers;

public class JsonResultHandler: IResult, IDisposable
{
    private readonly HttpResponseMessage _openAiResponseMessage;
    private readonly AICentralUsageInformation _aiCentralUsageInformation;

    public JsonResultHandler(
        HttpResponseMessage openAiResponseMessage,
        AICentralUsageInformation aiCentralUsageInformation)
    {
        _openAiResponseMessage = openAiResponseMessage;
        _aiCentralUsageInformation = aiCentralUsageInformation;
    }

    public async Task ExecuteAsync(HttpContext context)
    {
        context.Response.StatusCode = (int)_openAiResponseMessage.StatusCode;
        context.Response.Headers.Add("x-aicentral-duration", _aiCentralUsageInformation.Duration.TotalMilliseconds.ToString(CultureInfo.InvariantCulture));
        context.Response.ContentType = "application/json";
        
        foreach (var responseHeader in _openAiResponseMessage.Headers)
        {
            context.Response.Headers.Add(responseHeader.Key, new StringValues(responseHeader.Value.ToArray()));
        }
        await context.Response.WriteAsync(await _openAiResponseMessage.Content.ReadAsStringAsync());
    }

    public void Dispose()
    {
        _openAiResponseMessage.Dispose();
    }
}