using AICentral.Configuration.JSON;
using Microsoft.AspNetCore.Authentication;

namespace AICentral.PipelineComponents.Auth.ApiKey;

internal class ApiKeyOptions : AuthenticationSchemeOptions
{
    public string HeaderName { get; set; } = default!;
    public ConfigurationTypes.ApiKeyClientAuth[] Clients { get; set; } = default!;
}