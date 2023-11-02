using Microsoft.AspNetCore.Authentication;

namespace AICentral.PipelineComponents.Auth.ApiKey;

internal class ApiKeyOptions : AuthenticationSchemeOptions
{
    public string HeaderName { get; set; }
    public string Key1 { get; set; }
    public string Key2 { get; set; }
}