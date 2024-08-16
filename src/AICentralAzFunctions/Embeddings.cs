using AICentral;
using AICentral.Core;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace AICentralAzFunctions;

public class Embeddings
{
    private readonly Pipeline _pipeline;
    private readonly ILogger _logger;

    public Embeddings(ILoggerFactory loggerFactory, ConfiguredPipelines pipeline)
    {
        _pipeline = pipeline.Pipelines.Single();
        _logger = loggerFactory.CreateLogger<Embeddings>();
    }

    
    [Function("Embeddings")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = HostNameMatchRouter.EmbeddingsRoute)] HttpRequestData req,
        string deploymentName,
        FunctionContext executionContext)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request.");

        var response = req.CreateResponse();
        await _pipeline.Execute(
            new AzureFunctionsWrappedContext(req, executionContext, response), 
            deploymentName, null,
            AICallType.Embeddings, executionContext.CancellationToken);

        return response;
    }
}