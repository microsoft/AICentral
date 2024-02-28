using System.Net;
using AICentral.Core;
using Microsoft.Extensions.Primitives;

namespace LoadTest;

public class PreCannedEndpointAdapterFactory : IDownstreamEndpointAdapter, IDownstreamEndpointAdapterFactory
{
    private static string? _content;
    private static readonly Dictionary<string,StringValues> EmptyHeaders = new Dictionary<string, StringValues>();

    public PreCannedEndpointAdapterFactory(string endpointName)
    {
        Id = Guid.NewGuid().ToString();
        EndpointName = endpointName;

        using var contentReader = new StreamReader(
            typeof(PreCannedEndpointAdapterFactory)
                .Assembly
                .GetManifestResourceStream("LoadTest.Assets.FakeOpenAIChatCompletionsResponse.json")!
        );

        _content = contentReader.ReadToEnd();
    }

    public Task<Either<HttpRequestMessage, IResult>> BuildRequest(IncomingCallDetails incomingCall, HttpContext context)
    {
        return Task.FromResult(
            new Either<HttpRequestMessage, IResult>(new HttpRequestMessage(HttpMethod.Post,
                new Uri("https://localtest.me"))));
    }

    public Task<HttpResponseMessage> DispatchRequest(HttpContext context, HttpRequestMessage requestMessage,
        CancellationToken cancellationToken)
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(_content!)
        };
        return Task.FromResult(response);
    }

    public Task<ResponseMetadata> ExtractResponseMetadata(IncomingCallDetails callInformationIncomingCallDetails,
        HttpContext context,
        HttpResponseMessage openAiResponse)
    {
        return Task.FromResult(new ResponseMetadata(EmptyHeaders, null, null));
    }

    public string Id { get; }
    public Uri BaseUrl => new("https://localtest.me");
    public string EndpointName { get; }

    public void RegisterServices(HttpMessageHandler? httpMessageHandler, IServiceCollection services)
    {
    }

    public IDownstreamEndpointAdapter Build()
    {
        return this;
    }

    public static string ConfigName => "PreCannedEndpoint";

    public static IDownstreamEndpointAdapterFactory BuildFromConfig(ILogger logger, TypeAndNameConfig config)
    {
        return new PreCannedEndpointAdapterFactory(config.Name!);
    }

    public object WriteDebug()
    {
        return new
        {
            Type = "PreCanned"
        };
    }
}