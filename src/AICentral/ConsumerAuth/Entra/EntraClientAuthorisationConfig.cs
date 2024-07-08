namespace AICentral.ConsumerAuth.Entra;

public class EntraClientAuthorisationConfig
{
    public bool? DisableScopeAndRoleCheck { get; init; }
    public string[]? Roles { get; init; }
}