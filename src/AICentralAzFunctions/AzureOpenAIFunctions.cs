using System.Net;
using AICentral;
using AICentral.Core;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace AICentralAzFunctions;

public class AzureOpenAIFunctions
{
    private readonly Pipeline _pipeline;
    private readonly ILogger _logger;

    public AzureOpenAIFunctions(ILoggerFactory loggerFactory, ConfiguredPipelines pipeline)
    {
        _pipeline = pipeline.Pipelines.Single();
        _logger = loggerFactory.CreateLogger<AzureOpenAIFunctions>();
    }

    
    [Function("Embeddings")]
    public async Task<HttpResponseData> Embeddings(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = HostNameMatchRouter.EmbeddingsRoute)] HttpRequestData req,
        string deploymentName,
        FunctionContext executionContext)
    {
        var authdIdentities = req.Identities.Where(x => x.IsAuthenticated);
        _logger.LogInformation("C# HTTP trigger function processed a request. Found {Count} authd identities", authdIdentities.Count());

        var response = req.CreateResponse();
        await _pipeline.Execute(
            new AzureFunctionsWrappedContext(req, executionContext, response), 
            deploymentName, null,
            AICallType.Embeddings, executionContext.CancellationToken);

        return response;
    }
    
    [Function("ChatCompletions")]
    public async Task<HttpResponseData> ChatCompletions(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = HostNameMatchRouter.ChatCompletionsRoute)] HttpRequestData req,
        string deploymentName,
        FunctionContext executionContext)
    {
        var authdIdentities = req.Identities.Where(x => x.IsAuthenticated);
        _logger.LogInformation("C# HTTP trigger function processed a request. Found {Count} authd identities", authdIdentities.Count());

        var response = req.CreateResponse();
        await _pipeline.Execute(
            new AzureFunctionsWrappedContext(req, executionContext, response), 
            deploymentName, null,
            AICallType.Chat, executionContext.CancellationToken);

        return response;
    }

    [Function("Completions")]
    public async Task<HttpResponseData> Completions(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = HostNameMatchRouter.CompletionsRoute)] HttpRequestData req,
        string deploymentName,
        FunctionContext executionContext)
    {
        var authdIdentities = req.Identities.Where(x => x.IsAuthenticated);
        _logger.LogInformation("C# HTTP trigger function processed a request. Found {Count} authd identities", authdIdentities.Count());

        var response = req.CreateResponse();
        await _pipeline.Execute(
            new AzureFunctionsWrappedContext(req, executionContext, response), 
            deploymentName, null,
            AICallType.Chat, executionContext.CancellationToken);

        return response;
    }

}