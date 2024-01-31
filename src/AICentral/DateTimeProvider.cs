using AICentral.Core;

namespace AICentral;

/// <summary>
/// Implementation of IDateTimeProvider using DateTimeOffset.Now
/// </summary>
/// <remarks>
/// Override for testing date / time behaviour. AICentral always uses IDateTimeProvider to find the current date 
/// </remarks>
public class DateTimeProvider: IDateTimeProvider
{
    public DateTimeOffset Now => DateTimeOffset.Now;
}