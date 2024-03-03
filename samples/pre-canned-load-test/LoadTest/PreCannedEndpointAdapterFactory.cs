using System.Net;
using System.Text;
using AICentral.Core;
using Microsoft.Extensions.Primitives;

namespace LoadTest;

public class PreCannedEndpointAdapterFactory : IDownstreamEndpointAdapter, IDownstreamEndpointAdapterFactory
{
    private static readonly Dictionary<string,StringValues> EmptyHeaders = new();
    private readonly Task<Either<HttpRequestMessage,IResult>> _preCannedRequest;
    private readonly byte[] _content;

    public PreCannedEndpointAdapterFactory(string endpointName)
    {
        Id = Guid.NewGuid().ToString();
        EndpointName = endpointName;

        using var contentReader = new StreamReader(
            typeof(PreCannedEndpointAdapterFactory)
                .Assembly
                .GetManifestResourceStream("LoadTest.Assets.FakeOpenAIChatCompletionsResponse.json")!
        );

        _preCannedRequest = Task.FromResult(
            new Either<HttpRequestMessage, IResult>(new HttpRequestMessage(HttpMethod.Post,
                new Uri("https://localtest.me"))
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            }));

        _content = Encoding.UTF8.GetBytes(contentReader.ReadToEnd());
    }

    public Task<Either<HttpRequestMessage, IResult>> BuildRequest(IncomingCallDetails incomingCall, HttpContext context)
    {
        return _preCannedRequest;
    }

    public Task<HttpResponseMessage> DispatchRequest(HttpContext context, HttpRequestMessage requestMessage,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(_content)
            {
                Headers =  { { "Content-Type", "application/json" } }
            }
        });
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