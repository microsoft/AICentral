using System.Text.Json;
using System.Text.Json.Nodes;
using AICentral.Core;
using Microsoft.Extensions.Primitives;

namespace AICentral.RequestFiltering;

public class RequestFilteringProvider : IPipelineStep
{
    private readonly string[] _whiteList;
    private readonly bool _allowDataUris;

    public RequestFilteringProvider(RequestFilteringConfiguration properties)
    {
        _whiteList = (properties.AllowedHostNames ?? []).Select(x => x.ToLowerInvariant()).ToArray();
        _allowDataUris = properties.AllowDataUris ?? true;
    }

    public async Task<AICentralResponse> Handle(
        IRequestContext context, 
        IncomingCallDetails aiCallInformation,
        NextPipelineStep next,
        CancellationToken cancellationToken)
    {
        if (aiCallInformation is { AICallType: AICallType.Chat, RequestContent: not null })
        {
            var requestContent = aiCallInformation.RequestContent!;
            var messages = requestContent["messages"]!.AsArray();
            var logger = context.GetLogger<RequestFilteringProvider>();
            foreach (var message in messages.AsArray())
            {
                RemoveImageUrls(logger, message!);
            }
        }

        return await next(context, aiCallInformation, cancellationToken);
    }

    private void RemoveImageUrls(ILogger<RequestFilteringProvider> logger, JsonNode message)
    {
        var remove = new List<JsonNode>();
        if (message["content"]!.GetValueKind() == JsonValueKind.Array)
        {
            var messageContentArray = message["content"]!.AsArray();
            foreach (var item in messageContentArray)
            {
                if (item != null && item["type"] != null)
                {
                    if (item["type"]!.GetValue<string>().ToLowerInvariant() == "image_url")
                    {
                        if (!ImageIsOk(logger, item["image_url"]!))
                        {
                            remove.Add(item);
                        }
                    }
                }
            }

            foreach (var toRemove in remove)
            {
                messageContentArray.Remove(toRemove);
            }
        }
    }

    private bool ImageIsOk(ILogger<RequestFilteringProvider> logger, JsonNode jsonNode)
    {
        try
        {
            var imageUrl = jsonNode["url"]!.GetValue<string>();
            var asUrl = new Uri(imageUrl);
            if (asUrl.Scheme == "http" || asUrl.Scheme == "https")
            {
                if (!_whiteList.Contains(asUrl.Host.ToLowerInvariant()))
                {
                    logger.LogWarning("Detected chat request with Hostname: {HostName} which is not whitelisted",
                        asUrl.Host);
                    return false;
                }
            }

            if (asUrl.Scheme.StartsWith("data") && !_allowDataUris)
            {
                logger.LogWarning("Detected chat request with data-uri: {DatUri} which is not allowed",
                    asUrl.Scheme);

                return false;
            }

            return true;
        }
        catch (KeyNotFoundException)
        {
            return true;
        }
    }

    public Task BuildResponseHeaders(IRequestContext context, HttpResponseMessage rawResponse,
        Dictionary<string, StringValues> rawHeaders)
    {
        return Task.CompletedTask;
    }
}