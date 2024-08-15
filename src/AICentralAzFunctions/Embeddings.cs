using System.Collections.Generic;
using System.Net;
using AICentral;
using AICentral.Core;
using Microsoft.Extensions.Logging;

namespace AICentralAzFunctions;

public class Embeddings
{
    private readonly Pipeline _aiCentralPipeline;
    private readonly ILogger _logger;

    public Embeddings(ILoggerFactory loggerFactory, Pipeline aiCentralPipeline)
    {
        _aiCentralPipeline = aiCentralPipeline;
        _logger = loggerFactory.CreateLogger<Embeddings>();
    }

    [Function("Embeddings")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req,
        string deploymentName,
        FunctionContext executionContext)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request.");
        var responseDataresponseData = new HttpResponseData();
        var response = await _aiCentralPipeline.Execute(
            new AzureFunctionsRequestContext(req),
            deploymentName,
            null,
            AICallType.Embeddings,
            executionContext.CancellationToken);

    }
}