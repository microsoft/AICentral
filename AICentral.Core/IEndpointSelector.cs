namespace AICentral.Core;

public interface IEndpointSelector
{
    object WriteDebug();

    Task<AICentralResponse> Handle(
        HttpContext context, 
        AICallInformation aiCallInformation, 
        CancellationToken cancellationToken);
}