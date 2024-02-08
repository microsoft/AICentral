namespace AICentral;

internal interface IHandleOpenAICalls
{
    Task<IResult> HandleDeploymentBasedCalls(HttpContext context, CancellationToken cancellationToken);
    Task<IResult> HandleDalle2Calls(HttpContext context, CancellationToken cancellationToken);
    Task<IResult> HandleAssistantCalls(HttpContext context, CancellationToken cancellationToken);
}