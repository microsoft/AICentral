using AICentral.Core;
using Azure.Storage.Queues;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace AICentral.Logging.PIIStripping;

public class PIIStrippingLogger(
    string stepName,
    QueueClient queueClient,
    PIIStrippingLoggerConfig config,
    ILogger<PIIStrippingLogger> logger)
    : IPipelineStep
{
    private readonly string _stepName = stepName;
    private readonly PIIStrippingLoggerConfig _config = config;
    private bool _doesQueueExist;
    private bool _loggedError;

    public async Task<AICentralResponse> Handle(IRequestContext context, IncomingCallDetails aiCallInformation,
        NextPipelineStep next,
        CancellationToken cancellationToken)
    {
        var response = await next(context, aiCallInformation, cancellationToken);
        if (response.DownstreamUsageInformation.Success.GetValueOrDefault())
        {
            if (!_doesQueueExist)
            {
                var queueCreateResponse =
                    await queueClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
                if (queueCreateResponse == null)
                {
                    _doesQueueExist = true;
                }
                else
                {
                    if (!_loggedError)
                    {
                        logger.LogWarning("Failed to create Advanced Logging Queue. Cannot log. Reason {Reason}",
                            queueCreateResponse.ReasonPhrase);
                        _loggedError = true;
                    }
                }
            }

            if (_doesQueueExist)
            {
                var sendReceipt = await queueClient.SendMessageAsync(new BinaryData(
                    new LogEntry(
                        Guid.NewGuid().ToString(),
                        $"{response.DownstreamUsageInformation.DeploymentName}_{response.DownstreamUsageInformation.Client}_{response.DownstreamUsageInformation.StartDate:yyyy-MM-dd}",
                        response.DownstreamUsageInformation.InternalEndpointName,
                        response.DownstreamUsageInformation.OpenAIHost,
                        response.DownstreamUsageInformation.ModelName,
                        response.DownstreamUsageInformation.DeploymentName,
                        response.DownstreamUsageInformation.Client,
                        response.DownstreamUsageInformation.CallType.ToString(),
                        response.DownstreamUsageInformation.StreamingResponse,
                        response.DownstreamUsageInformation.Prompt,
                        response.DownstreamUsageInformation.Response,
                        response.DownstreamUsageInformation.EstimatedTokens?.Value.EstimatedPromptTokens,
                        response.DownstreamUsageInformation.EstimatedTokens?.Value.EstimatedCompletionTokens,
                        response.DownstreamUsageInformation.KnownTokens?.PromptTokens,
                        response.DownstreamUsageInformation.KnownTokens?.CompletionTokens,
                        response.DownstreamUsageInformation.KnownTokens?.TotalTokens,
                        response.DownstreamUsageInformation.RemoteIpAddress,
                        response.DownstreamUsageInformation.StartDate,
                        response.DownstreamUsageInformation.Duration,
                        response.DownstreamUsageInformation.Success
                    )), cancellationToken: cancellationToken);
            }
        }

        return response;
    }

    /// <summary>
    /// We can't return remaining token headers yet as streamed responses haven't been calculated (at this point).
    /// Instead we will return a trailing header directly in the response (see above method).
    /// </summary>
    /// <param name="context"></param>
    /// <param name="rawResponse"></param>
    /// <param name="rawHeaders"></param>
    /// <returns></returns>
    public Task BuildResponseHeaders(IRequestContext context, HttpResponseMessage rawResponse,
        Dictionary<string, StringValues> rawHeaders)
    {
        return Task.CompletedTask;
    }
}