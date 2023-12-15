namespace AICentral.Core;

/// <summary>
/// Central configuration class. Allows a typed configuration system
/// </summary>
public class AICentralTypeAndNameConfig
{
    public string? Type { get; init; }
    public string? Name { get; init; }
    public IConfigurationSection? ConfigurationSection { get; set; }
    public T TypedProperties<T>() where T : class => ConfigurationSection?.GetSection("Properties").Get<T>() ?? throw new ArgumentNullException($"Missing Properties on section {ConfigurationSection?.Path ?? "Unknown"}");
}