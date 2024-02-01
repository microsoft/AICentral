namespace AICentral.Core;

/// <summary>
/// Central configuration class. Allows a typed configuration system
/// </summary>
public class TypeAndNameConfig
{
    /// <summary>
    /// Type of configuration
    /// </summary>
    /// <remarks>
    /// Is used to find the correct factory to build the step from IAICentralPipelineStepFactory.ConfigName
    /// </remarks>
    public string? Type { get; init; }
    
    /// <summary>
    /// Friendly name to reference the step in a pipeline / elsewhere in config
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// The raw configuration section
    /// </summary>
    public IConfigurationSection? ConfigurationSection { get; set; }
    
    /// <summary>
    /// Binds the custom Properties section of the config to a given Config Type
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public T TypedProperties<T>() where T : class => ConfigurationSection?.GetSection("Properties").Get<T>() ?? throw new ArgumentNullException($"Missing Properties on section {ConfigurationSection?.Path ?? "Unknown"}");
}
