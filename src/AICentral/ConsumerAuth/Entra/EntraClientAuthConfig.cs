namespace AICentral.ConsumerAuth.Entra;

public class EntraClientAuthConfig
{
    public EntraClientAuthorisationConfig? Requirements { get; init; }
    public object? Entra { get; init; }
}