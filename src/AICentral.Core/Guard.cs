namespace AICentral.Core;

/// <summary>
/// Helper methods to check for Nulls / empty strings, etc and throw Argument Exceptions
/// </summary>
public static class Guard
{
    public static T NotNull<T>(T? input, IConfigurationSection configurationSection, string parameterName)
    {
        return input ?? throw new ArgumentException($"You must pass a value for {parameterName} at {configurationSection.Path}");
    }

    public static T NotNull<T>(T? input, string parameterName)
    {
        return input ?? throw new ArgumentException($"You must pass a value for {parameterName}");
    }

    public static string NotNullOrEmptyOrWhitespace(string? input, string parameterName)
    {
        return string.IsNullOrWhiteSpace(input) ? throw new ArgumentException($"You must pass a value for {parameterName}") : input;
    }

}