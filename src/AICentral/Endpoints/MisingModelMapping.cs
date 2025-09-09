namespace AICentral.Endpoints;

/// <summary>
/// New result to let missing model mappings not be logged as warnings. Fixes an issue
/// where a user had model mappings that were different across endpoints, but wanted
/// to load balance across them anyway. A missing model mapping was not seen as a warning
/// that should be logged with severity. 
/// </summary>
/// <param name="hostName"></param>
/// <param name="missingModel"></param>
/// <param name="logAsWarning"></param>
internal class MisingModelMapping(string hostName, string missingModel, bool logAsWarning) : IResult
{
    public string HostName { get; } = hostName;
    public string MissingModel { get; } = missingModel;
    public bool LogAsWarning { get; } = logAsWarning;

    /// <summary>
    /// Bit of a hack - to replicate old behaviour of returning 404 when no model mapping is found.
    /// </summary>
    /// <param name="httpContext"></param>
    /// <returns></returns>
    public Task ExecuteAsync(HttpContext httpContext)
    {
        var temp = Results.NotFound();
        return temp.ExecuteAsync(httpContext);
    }
}