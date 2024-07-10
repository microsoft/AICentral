namespace AICentral.RequestFiltering;

public class RequestFilteringConfiguration
{
    public string[]? AllowedHostNames { get; init; }
    public bool? AllowDataUris { get; init; }
}