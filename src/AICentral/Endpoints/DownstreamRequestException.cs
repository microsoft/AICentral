namespace AICentral.Endpoints;

internal class DownstreamRequestException(IResult result)
    : HttpRequestException("Cannot dispatch to downstream endpoint.")
{
    public IResult Result { get; } = result;
}
