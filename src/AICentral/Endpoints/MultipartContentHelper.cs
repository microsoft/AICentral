using System.Text;
using AICentral.Core;
using Microsoft.AspNetCore.Http.Extensions;

namespace AICentral.Endpoints;

public static class MultipartContentHelper
{
    /// <summary>
    /// Bit tricky but to proxy Whisper requests we need to copy the multipart content from the incoming request to the new request.
    /// I've tried a few ways and this one seems to work without buffering the request, or messing with the Content's stream.Position.
    /// So hopefully it doesn't buffer the incoming request into RAM...
    /// </summary>
    public static MultipartFormDataContent CopyMultipartContent(
        IRequestContext incomingRequest,
        string? mappedModelName)
    {
        var newContent = new MultipartFormDataContent(incomingRequest.GetMultipartBoundary());

        foreach (var item in incomingRequest.Form.Files)
        {
            var fileContent = new StreamContent(item.OpenReadStream());
            fileContent.Headers.TryAddWithoutValidation("Content-Disposition", item.ContentDisposition);
            fileContent.Headers.TryAddWithoutValidation("Content-Type", item.ContentType);
            newContent.Add(fileContent);
        }

        foreach (var item in incomingRequest.Form)
        {
            if (item.Key == "model")
            {
                if (mappedModelName != null || item.Value.Count > 0)
                {
                    newContent.Add(
                        new StringContent(mappedModelName ?? item.Value.First()!, Encoding.GetEncoding("utf-8"),
                            "text/plain"), item.Key);
                }
            }
            else
            {
                if (item.Value.Count > 0)
                {
                    newContent.Add(new StringContent(item.Value.First()!, Encoding.GetEncoding("utf-8"), "text/plain"),
                        item.Key);
                }
            }
        }

        return newContent;
    }
}