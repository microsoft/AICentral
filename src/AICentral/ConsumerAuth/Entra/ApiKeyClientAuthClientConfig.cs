namespace AICentral.ConsumerAuth.Entra;

public class EntraClientAuthConfig
{
    public EntraClientAuthorisationConfig? Requirements { get; init; }
}

public class EntraClientAuthorisationConfig
{
    public string[]? Roles { get; init; }
}