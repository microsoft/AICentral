namespace AICentral.Core;

public interface IEndpointSelector
{
    Task<AICentralResponse> Handle(
        HttpContext context, 
        AICallInformation aiCallInformation, 
        bool isLastChance,
        CancellationToken cancellationToken);
}