namespace AICentral.Endpoints;

internal class MisingModelMapping(string hostName, string missingModel, bool logAsWarning) : IResult
{
    public string HostName { get; } = hostName;
    public string MissingModel { get; } = missingModel;
    public bool LogAsWarning { get; } = logAsWarning;

    public Task ExecuteAsync(HttpContext httpContext)
    {
        throw new NotImplementedException();
    }
}