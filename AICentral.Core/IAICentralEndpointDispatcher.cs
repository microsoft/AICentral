using Microsoft.Extensions.Primitives;

namespace AICentral.Core;

public interface IAICentralEndpointDispatcherFactory
{
    IAICentralEndpointDispatcher Build();
    object WriteDebug();
    void RegisterServices(HttpMessageHandler? optionalHandler, IServiceCollection services);
}

public interface IAICentralEndpointDispatcher
{
    Task<AICentralResponse> Handle(
        HttpContext context,
        AICallInformation callInformation,
        bool isLastChance,
        IAICentralResponseGenerator responseGenerator,
        CancellationToken cancellationToken);

    bool IsAffinityRequestToMe(string affinityHeaderValue);

}

public interface IEndpointRequestResponseHandler
{
    string Id { get; }
    string BaseUrl { get; }
    string EndpointName { get; }

    Task<Either<HttpRequestMessage, IResult>> BuildRequest(AICallInformation incomingCall, HttpContext context);

    Task HandleResponse(IncomingCallDetails callInformationIncomingCallDetails, HttpRequestMessage newRequest,
        HttpResponseMessage openAiResponse);

    Dictionary<string, StringValues> SanitiseHeaders(HttpContext context, HttpResponseMessage openAiResponse);
}

public class Either<T, T1>
{
    private readonly T1? _right;
    private readonly T? _left;

    public Either(T value)
    {
        _left = value;
    }

    public Either(T1 value)
    {
        _right = value;
    }

    public bool Left(out T? val)
    {
        val = _left;
        return _left != null;
    }

    public bool Right(out T1? val)
    {
        val = _right;
        return _right != null;
    }
}