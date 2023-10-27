namespace AICentral.Pipelines.Endpoints.AzureOpenAI;

public class AzureOpenAIActionStreamingResultHandler: IResult
{
    public Task ExecuteAsync(HttpContext context)
    {
        //assume result already sent down by the time this runs. No-op.
        return Task.CompletedTask;
    }
}