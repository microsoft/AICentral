using Azure.AI.TextAnalytics;
using Azure.Storage.Queues;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AICentral.DistributedTokenLimits;

internal class PIIStrippingLoggerQueueConsumer(
    QueueClient queueClient,
    TextAnalyticsClient textAnalyticsClient,
    CosmosClient cosmosClient,
    PIIStrippingLoggerConfig config,
    ILogger<PIIStrippingLoggerQueueConsumer> logger)
    : IHostedService
{
    private readonly PIIStrippingLoggerConfig _config = config;
    private bool _stop;
    private Task? _consumer;
    private CancellationTokenSource? _cancellationToken;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Queue Consumer is starting");
        await Task.Yield();
        _cancellationToken = new CancellationTokenSource();
        _consumer = RunConsumer(_cancellationToken.Token);
    }

    private async Task RunConsumer(CancellationToken cancellationToken)
    {
        await queueClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
        var database = cosmosClient.GetDatabase(_config.CosmosDatabase);
        var container = await database.CreateContainerIfNotExistsAsync(_config.CosmosContainer, "/id", cancellationToken: cancellationToken);

        while (!_stop)
        {
            var message = await queueClient.ReceiveMessageAsync(TimeSpan.FromMinutes(2), cancellationToken);
            if (message.Value != null)
            {
                logger.LogDebug("Processing message from the queue");

                //get the prompt and redact it
                var loggingMessage = message.Value.Body.ToObjectFromJson<LogEntry>();
                var redacted = await textAnalyticsClient.RecognizePiiEntitiesBatchAsync(
                    [loggingMessage.Prompt, loggingMessage.Response], cancellationToken: cancellationToken);

                //log the response
                var redactedMessage = loggingMessage with
                {
                    Prompt = redacted.Value[0].Entities.RedactedText,
                    Response = redacted.Value[1].Entities.RedactedText
                };

                //save the message
                await container.Container.CreateItemAsync(
                    redactedMessage,
                    new PartitionKey(redactedMessage.id),
                    cancellationToken: cancellationToken);

                //delete the message
                await queueClient.DeleteMessageAsync(message.Value.MessageId, message.Value.PopReceipt,
                    cancellationToken);
            }
            else
            {
                await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
                logger.LogDebug("No messages in the queue");
            }
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _stop = true;
        try
        {
            if (_cancellationToken != null)
            {
                await _cancellationToken!.CancelAsync();
            }
        }
        catch (Exception)
        {
            // ignored
        }

        if (_consumer != null)
        {
            await _consumer!;
        }
    }
}