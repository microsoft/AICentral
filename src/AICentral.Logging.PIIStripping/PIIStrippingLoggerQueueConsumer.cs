using AICentral.Core;
using Azure.AI.TextAnalytics;
using Azure.Storage.Queues;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AICentral.Logging.PIIStripping;

internal class PIIStrippingLoggerQueueConsumer(
    QueueClient queueClient,
    TextAnalyticsClient textAnalyticsClient,
    CosmosClient cosmosClient,
    PIIStrippingLoggerConfig config,
    ILogger<PIIStrippingLoggerQueueConsumer> logger,
    IDateTimeProvider dateTimeProvider)
    : IHostedService
{
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
        var database = cosmosClient.GetDatabase(config.CosmosDatabase);
        var container = await database.CreateContainerIfNotExistsAsync(config.CosmosContainer, "/LogId",
            cancellationToken: cancellationToken);
        var failCount = 0;

        try
        {
            while (!_stop)
            {
                try
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
                            Prompt = string.IsNullOrWhiteSpace(loggingMessage.Prompt) 
                                ? string.Empty
                                : redacted.Value[0].Entities.RedactedText,
                            Response =string.IsNullOrWhiteSpace(loggingMessage.Response) 
                                ? string.Empty
                                : redacted.Value[1].Entities.RedactedText
                        };

                        //save the message
                        await container.Container.CreateItemAsync(
                            redactedMessage,
                            new PartitionKey(redactedMessage.LogId),
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

                    failCount = 0;
                }
                catch (Exception e)
                    when (!
                              (e is TaskCanceledException
                               || e is StackOverflowException
                               || e is OutOfMemoryException
                               || e is AccessViolationException
                               || e is TaskCanceledException)
                         )
                {
                    //we want to keep this loop alive... Other than some select Exceptions, lets retry.
                    failCount++;
                    var timeout = TimeSpan.FromMinutes(5);

                    if (failCount >=
                        20) //arbritrary number... let's write an error out, and increase the timeout to 10 minutes.
                    {
                        timeout = TimeSpan.FromMinutes(20);
                        logger.LogError(e,
                            "Repeated failures. Increasing backoff to 10 minutes until failures resolved. Next retry: {Retry}",
                            dateTimeProvider.Now.Add(timeout));
                    }
                    else
                    {
                        logger.LogWarning(e, "Failure in processing queue. Backing off. Next retry: {Retry}",
                            dateTimeProvider.Now.Add(timeout));
                    }

                    await Task.Delay(timeout, cancellationToken);
                }
            }
        }
        catch (TaskCanceledException)
        {
            logger.LogInformation("Processing loop cancelled via cancellation token. Finishing process loop");
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