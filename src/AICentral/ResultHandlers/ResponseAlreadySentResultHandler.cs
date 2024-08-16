namespace AICentral.ResultHandlers;

public class ResponseAlreadySentResultHandler: IResult
{
    public Task ExecuteAsync(HttpContext context)
    {
        //assume result already sent down by the time this runs. No-op.
        return Task.CompletedTask;
    }
}